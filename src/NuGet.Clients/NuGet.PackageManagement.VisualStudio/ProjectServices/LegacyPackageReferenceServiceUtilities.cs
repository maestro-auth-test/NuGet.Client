// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Collections.Immutable;
using Microsoft;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;
using NuGet.Versioning;

namespace NuGet.PackageManagement.VisualStudio.ProjectServices
{
    internal static class LegacyPackageReferenceServiceUtilities
    {
        internal static ProjectRestoreReference ToProjectRestoreReference(ProjectReference item)
        {
            var reference = new ProjectRestoreReference
            {
                ProjectUniqueName = item.UniqueName,
                ProjectPath = item.UniqueName
            };

            MSBuildRestoreUtility.ApplyIncludeFlags(
                reference,
                GetReferenceMetadataValue(item, ProjectItemProperties.IncludeAssets),
                GetReferenceMetadataValue(item, ProjectItemProperties.ExcludeAssets),
                GetReferenceMetadataValue(item, ProjectItemProperties.PrivateAssets));

            return reference;
        }

        internal static LibraryDependency ToPackageLibraryDependency(PackageReference reference, bool isCpvmEnabled)
        {
            // Get warning suppressions
            ImmutableArray<NuGetLogCode> noWarn = MSBuildStringUtility.GetNuGetLogCodes(GetReferenceMetadataValue(reference, ProjectItemProperties.NoWarn));

            (var includeType, var suppressParent) = MSBuildRestoreUtility.GetLibraryDependencyIncludeFlags(
                GetReferenceMetadataValue(reference, ProjectItemProperties.IncludeAssets),
                GetReferenceMetadataValue(reference, ProjectItemProperties.ExcludeAssets),
                GetReferenceMetadataValue(reference, ProjectItemProperties.PrivateAssets));

            var dependency = new LibraryDependency()
            {
                AutoReferenced = MSBuildStringUtility.IsTrue(GetReferenceMetadataValue(reference, ProjectItemProperties.IsImplicitlyDefined)),
                GeneratePathProperty = MSBuildStringUtility.IsTrue(GetReferenceMetadataValue(reference, ProjectItemProperties.GeneratePathProperty)),
                Aliases = GetReferenceMetadataValue(reference, ProjectItemProperties.Aliases, defaultValue: null),
                VersionOverride = GetVersionOverride(reference),
                LibraryRange = new LibraryRange(
                    name: reference.Name,
                    versionRange: ToVersionRange(reference.Version, isCpvmEnabled),
                    typeConstraint: LibraryDependencyTarget.Package),
                NoWarn = noWarn,
                IncludeType = includeType,
                SuppressParent = suppressParent,
            };

            return dependency;
        }

        private static VersionRange ToVersionRange(string version, bool isCpvmEnabled)
        {
            if (string.IsNullOrEmpty(version))
            {
                if (isCpvmEnabled)
                {
                    // Projects that have their packages managed centrally will not have Version metadata on PackageReference items.
                    return null;
                }
                else
                {
                    return VersionRange.All;
                }
            }

            return VersionRange.Parse(version);
        }

        private static string GetReferenceMetadataValue(PackageReference reference, string metadataElement, string defaultValue = "")
        {
            Assumes.Present(reference);
            Assumes.NotNullOrEmpty(metadataElement);

            if (reference.MetadataElements == null || reference.MetadataValues == null)
            {
                return defaultValue; // no metadata for package
            }

            var index = Array.IndexOf(reference.MetadataElements, metadataElement);
            if (index >= 0)
            {
                return reference.MetadataValues.GetValue(index) as string;
            }

            return defaultValue;
        }

        private static string GetReferenceMetadataValue(ProjectReference reference, string metadataElement)
        {
            Assumes.Present(reference);
            Assumes.NotNullOrEmpty(metadataElement);

            if (reference.MetadataElements == null || reference.MetadataValues == null)
            {
                return string.Empty; // no metadata for package
            }

            var index = Array.IndexOf(reference.MetadataElements, metadataElement);
            if (index >= 0)
            {
                return reference.MetadataValues.GetValue(index) as string;
            }

            return string.Empty;
        }

        private static VersionRange GetVersionOverride(PackageReference reference)
        {
            Assumes.Present(reference);

            string versionOverride = GetReferenceMetadataValue(reference, ProjectItemProperties.VersionOverride, defaultValue: null);

            if (string.IsNullOrWhiteSpace(versionOverride))
            {
                return null;
            }

            return VersionRange.Parse(versionOverride);
        }

        internal class ProjectReference
        {
            public ProjectReference(string uniqueName, Array metadataElements, Array metadataValues)
            {
                UniqueName = uniqueName;
                MetadataElements = metadataElements;
                MetadataValues = metadataValues;
            }

            public string UniqueName { get; }
            public Array MetadataElements { get; }
            public Array MetadataValues { get; }
        }

        internal class PackageReference
        {
            public PackageReference(
                string name,
                string version,
                Array metadataElements,
                Array metadataValues,
                NuGetFramework targetNuGetFramework)
            {
                Name = name;
                Version = version;
                MetadataElements = metadataElements;
                MetadataValues = metadataValues;
                TargetNuGetFramework = targetNuGetFramework;
            }

            public string Name { get; }
            public string Version { get; }
            public string VersionOverride { get; }
            public Array MetadataElements { get; }
            public Array MetadataValues { get; }
            public NuGetFramework TargetNuGetFramework { get; }
        }
    }
}
