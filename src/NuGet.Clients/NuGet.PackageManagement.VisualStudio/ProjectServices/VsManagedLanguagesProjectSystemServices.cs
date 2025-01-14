// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Versioning;
using NuGet.VisualStudio;
using VSLangProj150;
using static NuGet.PackageManagement.VisualStudio.ProjectServices.LegacyPackageReferenceServiceUtilities;
using Task = System.Threading.Tasks.Task;

namespace NuGet.PackageManagement.VisualStudio
{
    /// <summary>
    /// Contains the information specific to a Visual Basic or C# project.
    /// </summary>
    internal class VsManagedLanguagesProjectSystemServices :
        INuGetProjectServices
        , IProjectSystemCapabilities
        , IProjectSystemReferencesReader
        , IProjectSystemReferencesService
    {
        private static readonly string[] ReferenceMetadata;

        private readonly IVsProjectAdapter _vsProjectAdapter;
        private readonly IVsProjectThreadingService _threadingService;
        private readonly VSProject4 _vsProject4;

        public bool SupportsPackageReferences => true;

        public bool NominatesOnSolutionLoad { get; private set; } = false;

        #region INuGetProjectServices

        [Obsolete]
        public IProjectBuildProperties BuildProperties => throw new NotImplementedException();

        public IProjectSystemCapabilities Capabilities => this;

        public IProjectSystemReferencesReader ReferencesReader => this;

        public IProjectSystemReferencesService References => this;

        public IProjectSystemService ProjectSystem => throw new NotSupportedException();

        public IProjectScriptHostService ScriptService { get; }

        #endregion INuGetProjectServices

        static VsManagedLanguagesProjectSystemServices()
        {
            ReferenceMetadata = new string[]
            {
                ProjectItemProperties.IncludeAssets,
                ProjectItemProperties.ExcludeAssets,
                ProjectItemProperties.PrivateAssets,
                ProjectItemProperties.NoWarn,
                ProjectItemProperties.GeneratePathProperty,
                ProjectItemProperties.Aliases,
                ProjectItemProperties.VersionOverride,
                ProjectItemProperties.IsImplicitlyDefined,
            };
        }

        public VsManagedLanguagesProjectSystemServices(
            IVsProjectAdapter vsProjectAdapter,
            IVsProjectThreadingService threadingService,
            VSProject4 vsProject4,
            bool nominatesOnSolutionLoad,
            Lazy<IScriptExecutor> scriptExecutor)
        {
            Assumes.Present(vsProjectAdapter);
            Assumes.Present(threadingService);
            Assumes.Present(vsProject4);

            _vsProjectAdapter = vsProjectAdapter;
            _threadingService = threadingService;
            _vsProject4 = vsProject4;

            ScriptService = new VsProjectScriptHostService(vsProjectAdapter, scriptExecutor);

            NominatesOnSolutionLoad = nominatesOnSolutionLoad;
        }

        public async Task<IEnumerable<LibraryDependency>> GetPackageReferencesAsync(
            NuGetFramework targetFramework, CancellationToken _)
        {
            Assumes.Present(targetFramework);

            await _threadingService.JoinableTaskFactory.SwitchToMainThreadAsync();

            var installedPackages = _vsProject4.PackageReferences?.InstalledPackages;

            if (installedPackages == null)
            {
                return Array.Empty<LibraryDependency>();
            }

            bool isCpvmEnabled = IsCentralPackageManagementVersionsEnabled();

            var references = installedPackages
                .Cast<string>()
                .Where(r => !string.IsNullOrEmpty(r))
                .Select(installedPackage =>
                {
                    if (_vsProject4.PackageReferences.TryGetReference(
                        installedPackage,
                        ReferenceMetadata,
                        out var version,
                        out var metadataElements,
                        out var metadataValues))
                    {
                        return new PackageReference(
                            name: installedPackage,
                            version: version,
                            metadataElements: metadataElements,
                            metadataValues: metadataValues,
                            targetNuGetFramework: targetFramework);
                    }

                    return null;
                })
                .Where(p => p != null)
                .Select(p => ToPackageLibraryDependency(p, isCpvmEnabled));

            return references.ToList();
        }

        public async Task<IEnumerable<ProjectRestoreReference>> GetProjectReferencesAsync(
            ILogger _, CancellationToken __)
        {
            await _threadingService.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_vsProject4.References == null)
            {
                return Array.Empty<ProjectRestoreReference>();
            }

            var references = new List<ProjectRestoreReference>();
            foreach (Reference6 r in _vsProject4.References.Cast<Reference6>())
            {
                if (r.SourceProject != null && await EnvDTEProjectUtility.IsSupportedAsync(r.SourceProject))
                {
                    Array metadataElements;
                    Array metadataValues;
                    r.GetMetadata(ReferenceMetadata, out metadataElements, out metadataValues);

                    references.Add(ToProjectRestoreReference(new ProjectReference(
                        uniqueName: r.SourceProject.FullName,
                        metadataElements: metadataElements,
                        metadataValues: metadataValues)));
                }
            }

            return references;
        }

        public async Task AddOrUpdatePackageReferenceAsync(LibraryDependency packageReference, CancellationToken _)
        {
            Assumes.Present(packageReference);

            await _threadingService.JoinableTaskFactory.SwitchToMainThreadAsync();

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

            AddOrUpdatePackageReference(
                packageReference.Name,
                packageReference.LibraryRange.VersionRange,
                metadataElements.ToArray(),
                metadataValues.ToArray());
        }

        private void AddOrUpdatePackageReference(string packageName, VersionRange packageVersion, string[] metadataElements, string[] metadataValues)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Note that API behavior is:
            // - specify a metadata element name with a value => add/replace that metadata item on the package reference
            // - specify a metadata element name with no value => remove that metadata item from the project reference
            // - don't specify a particular metadata name => if it exists on the package reference, don't change it (e.g. for user defined metadata)
            _vsProject4.PackageReferences.AddOrUpdate(
                packageName,
                packageVersion.OriginalString ?? packageVersion.ToShortString(),
                metadataElements,
                metadataValues);
        }

        public async Task RemovePackageReferenceAsync(string packageName)
        {
            Assumes.NotNullOrEmpty(packageName);

            await _threadingService.JoinableTaskFactory.SwitchToMainThreadAsync();

            _vsProject4.PackageReferences.Remove(packageName);
        }

        private bool IsCentralPackageManagementVersionsEnabled()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
#pragma warning disable CS0618 // Type or member is obsolete
            // Need to validate no project systems get this property via DTE, and if so, switch to GetPropertyValue
            return MSBuildStringUtility.IsTrue(_vsProjectAdapter.BuildProperties.GetPropertyValueWithDteFallback(ProjectBuildProperties.ManagePackageVersionsCentrally));
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public async Task<IReadOnlyList<(string id, string[] metadata)>> GetItemsAsync(string itemTypeName, params string[] metadataNames)
        {
            await _threadingService.JoinableTaskFactory.SwitchToMainThreadAsync();
            return GetItems(_vsProjectAdapter, itemTypeName, metadataNames);
        }

        internal static IReadOnlyList<(string id, string[] metadata)> GetItems(IVsProjectAdapter projectAdapter, string itemTypeName, params string[] metadataNames)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            IEnumerable<(string ItemId, string[] ItemMetadata)> items = projectAdapter.GetBuildItemInformation(itemTypeName, metadataNames);
            var enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return Array.Empty<(string, string[])>();
            }

            List<(string, string[])> result = items is ICollection<(string, string[])> itemCollection ? new(itemCollection.Count) : new();

            do
            {
                result.Add(enumerator.Current);
            } while (enumerator.MoveNext());

            return result;
        }
    }
}
