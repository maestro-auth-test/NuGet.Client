using System;
using System.Collections.Generic;
using NuGet.Packaging;
using NuGet.Packaging.Core;

#nullable enable

namespace NuGet.PackageManagement.UI.Models.Package
{
    class RemotePackageModel : PackageModel
    {
        public RemotePackageModel(PackageIdentity identity,
            string? title = null,
            string? description = null,
            string? authors = null,
            Uri? projectUrl = null,
            string[]? tags = null,
            string? copyright = null,
            bool isListed = false,
            DateTimeOffset? dateTimeOffset = null,
            Uri? packageDetailsUrl = null,
            long? downloadCount = null,
            Uri? iconUrl = null,
            IEnumerable<PackageDependencyGroup>? dependencySets = null)
            : base(identity, title, description, authors, projectUrl, tags, copyright)
        {
            IsListed = isListed;
            Published = dateTimeOffset;
            PackageDetailsUrl = packageDetailsUrl;
            DownloadCount = downloadCount;
            IconUrl = iconUrl;
            DependencySets = dependencySets;
        }

        public bool IsListed { get; set; }
        public DateTimeOffset? Published { get; }
        public Uri? PackageDetailsUrl { get; }
        public long? DownloadCount { get; }
        public Uri? IconUrl { get; }
        public IEnumerable<PackageDependencyGroup>? DependencySets { get; }

        // Capabilities
    }
}
