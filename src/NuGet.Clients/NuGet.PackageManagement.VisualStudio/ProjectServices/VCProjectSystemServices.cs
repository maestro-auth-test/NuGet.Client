// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectManagement;
using NuGet.Versioning;
using NuGet.VisualStudio;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.VCProjectEngine;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using MicrosoftBuildEvaluationProject = Microsoft.Build.Evaluation.Project;
using static NuGet.PackageManagement.VisualStudio.ProjectServices.LegacyPackageReferenceServiceUtilities;

namespace NuGet.PackageManagement.VisualStudio
{
    /// <summary>
    /// Contains the information specific to a Visual C++ project.
    /// </summary>
    internal class VCProjectSystemServices
        : NativeProjectSystemReferencesReader
        , INuGetProjectServices
        , IProjectSystemCapabilities
        , IProjectSystemReferencesService
    {
        private readonly IVsProjectAdapter _vsProjectAdapter;
        private readonly IVsProjectThreadingService _threadingService;
        private readonly VCProject _vcProject;

        public bool SupportsPackageReferences => true;

        #region INuGetProjectServices

        [Obsolete]
        public IProjectBuildProperties BuildProperties => throw new NotImplementedException();

        public IProjectSystemCapabilities Capabilities => this;

        public IProjectSystemReferencesReader ReferencesReader => this;

        public IProjectSystemReferencesService References => this;

        public IProjectSystemService ProjectSystem => throw new NotSupportedException();

        public IProjectScriptHostService ScriptService { get; }

        public bool NominatesOnSolutionLoad => false;

        #endregion INuGetProjectServices

        public VCProjectSystemServices(IVsProjectAdapter vsProjectAdapter, IVsProjectThreadingService threadingService, VCProject vcProject, Lazy<IScriptExecutor> scriptExecutor)
            : base(vsProjectAdapter, threadingService)
        {
            Assumes.Present(vsProjectAdapter);
            Assumes.Present(threadingService);
            Assumes.Present(vcProject);
            _vsProjectAdapter = vsProjectAdapter;
            _threadingService = threadingService;
            _vcProject = vcProject;
            ScriptService = new VsProjectScriptHostService(vsProjectAdapter, scriptExecutor);
        }

        public override async Task<IEnumerable<LibraryDependency>> GetPackageReferencesAsync(
            NuGetFramework targetFramework, CancellationToken cancellationToken)
        {
            Assumes.Present(targetFramework);

            await _threadingService.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            IEnumerable<LibraryDependency> references = null;

            await ProjectHelper.DoWorkInReadLockAsync(
                _vsProjectAdapter.Project,
                _vsProjectAdapter.VsHierarchy,
                buildProject => references = GetPackageReferencesAsync(buildProject, targetFramework));

            return references;
        }

        private static List<LibraryDependency> GetPackageReferencesAsync(
            MicrosoftBuildEvaluationProject msBuildEvaluationproject,
            NuGetFramework targetFramework)
        {
            var packageReferences = msBuildEvaluationproject.GetItems("PackageReference");

            if (packageReferences != null && packageReferences.Count != 0)
            {
                var references = packageReferences.Select(installedPackage =>
                {
                    List<string> metadataElements = new List<string>();
                    List<string> metadataValues = new List<string>();


                    foreach (var item in installedPackage.Metadata)
                    {
                        if (item.Name.Equals("Version", StringComparison.OrdinalIgnoreCase) == false)
                        {
                            metadataElements.Add(item.Name);
                            metadataValues.Add(item.EvaluatedValue);
                        }
                    }


                    return new PackageReference(
                            name: installedPackage.EvaluatedInclude,
                            version: installedPackage.GetMetadataValue("Version"),
                            metadataElements: metadataElements.ToArray(),
                            metadataValues: metadataValues.ToArray(),
                            targetNuGetFramework: targetFramework);

                })
                .Where(p => p != null)
                .Select(p => ToPackageLibraryDependency(p, isCpvmEnabled: false));

                return references.ToList();
            }

            return [];
        }

        public async Task AddOrUpdatePackageReferenceAsync(LibraryDependency packageReference, CancellationToken cancellationToken)
        {
            Assumes.Present(packageReference);

            await _threadingService.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var includeFlags = packageReference.IncludeType;
            var privateAssetsFlag = packageReference.SuppressParent;
            var metadataElements = new List<string>();
            var metadataValues = new List<string>();
            if (includeFlags != LibraryIncludeFlags.All)
            {
                metadataElements.Add(ProjectItemProperties.IncludeAssets);
                metadataValues.Add(LibraryIncludeFlagUtils.GetFlagString(includeFlags).Replace(',', ';'));
            }

            if (privateAssetsFlag != LibraryIncludeFlagUtils.DefaultSuppressParent)
            {
                metadataElements.Add(ProjectItemProperties.PrivateAssets);
                metadataValues.Add(LibraryIncludeFlagUtils.GetFlagString(privateAssetsFlag).Replace(',', ';'));
            }

            await ProjectHelper.DoWorkInWriterLockAsync(
                _vsProjectAdapter.Project,
                _vsProjectAdapter.VsHierarchy,
                buildProject => AddOrUpdatePackageReference(
                    buildProject,
                    packageReference.Name,
                    packageReference.LibraryRange.VersionRange,
                    metadataElements.ToArray(),
                    metadataValues.ToArray()));
        }

        private static void AddOrUpdatePackageReference(MicrosoftBuildEvaluationProject msBuildEvaluationproject, string packageName, VersionRange packageVersion, string[] metadataElements, string[] metadataValues)
        {
            // Note that API behavior is:
            // - specify a metadata element name with a value => add/replace that metadata item on the package reference
            // - specify a metadata element name with no value => remove that metadata item from the project reference
            // - don't specify a particular metadata name => if it exists on the package reference, don't change it (e.g. for user defined metadata)

            var packageReferences = msBuildEvaluationproject.GetItems("PackageReference");
            var metadataElementCount = metadataElements.Length < metadataValues.Length ? metadataElements.Length : metadataValues.Length;

            var szPackageVersion = packageVersion.OriginalString ?? packageVersion.ToShortString();

            foreach (ProjectItem packageReferenceProjectItem in packageReferences)
            {
                if (packageReferenceProjectItem.EvaluatedInclude.Equals(packageName, StringComparison.OrdinalIgnoreCase))
                {
                    //Update PackageReference
                    packageReferenceProjectItem.SetMetadataValue("Version", szPackageVersion);

                    for (int i = 0; i != metadataElementCount; ++i)
                    {
                        if (metadataValues[i] == null || metadataValues[i].Length == 0)
                            packageReferenceProjectItem.RemoveMetadata(metadataElements[i]);
                        else
                            packageReferenceProjectItem.SetMetadataValue(metadataElements[i], metadataValues[i]);
                    }

                    msBuildEvaluationproject.ReevaluateIfNecessary();
                    return;
                }
            }

            ProjectItemElement itemElement = null;

            //add new
            if (packageReferences.Count != 0)
            {
                itemElement = msBuildEvaluationproject.Xml.CreateItemElement("PackageReference", packageName);

                var where = packageReferences.Last().Xml;

                where.Parent.InsertAfterChild(itemElement, where);
            }
            else
            {
                var itemGroup = msBuildEvaluationproject.Xml.AddItemGroup();

                itemElement = itemGroup.AddItem("PackageReference", packageName);
            }


            //Set PackageReference
            itemElement.AddMetadata("Version", szPackageVersion);


            for (int i = 0; i != metadataElementCount; ++i)
            {
                if (metadataValues[i] != null && metadataValues[i].Length != 0)
                    itemElement.AddMetadata(metadataElements[i], metadataValues[i]);
            }

            msBuildEvaluationproject.ReevaluateIfNecessary();

            return;
        }

        public async Task RemovePackageReferenceAsync(string packageName)
        {
            Assumes.NotNullOrEmpty(packageName);


            await NuGetUIThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            await ProjectHelper.DoWorkInWriterLockAsync(
                _vsProjectAdapter.Project,
                _vsProjectAdapter.VsHierarchy,
                buildProject => RemovePackageReferenceAsync(buildProject, packageName));
        }

        private static void RemovePackageReferenceAsync(MicrosoftBuildEvaluationProject msBuildEvaluationproject, string packageName)
        {
            var packageReferences = msBuildEvaluationproject.GetItems("PackageReference");

            foreach (ProjectItem packageReferenceProjectItem in packageReferences)
            {
                if (packageReferenceProjectItem.EvaluatedInclude.Equals(packageName, StringComparison.OrdinalIgnoreCase))
                {
                    var packageReferenceParent = packageReferenceProjectItem.Xml.Parent;

                    packageReferenceParent.RemoveChild(packageReferenceProjectItem.Xml);


                    if (packageReferenceParent.Count == 0)
                    {
                        packageReferenceParent.Parent.RemoveChild(packageReferenceParent);
                    }


                    msBuildEvaluationproject.ReevaluateIfNecessary();

                    break;
                }
            }
        }

        //public Task<IReadOnlyList<(string id, string[] metadata)>> GetItemsAsync(string itemTypeName, params string[] metadataNames)
        //{
        //    IReadOnlyList<(string, string[])> results = new List<(string, string[])>();
        //    return Task.FromResult(results);
        //}
    }
}
