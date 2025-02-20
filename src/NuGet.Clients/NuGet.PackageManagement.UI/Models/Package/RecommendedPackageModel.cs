using System;
using NuGet.Packaging.Core;

#nullable enable

namespace NuGet.PackageManagement.UI
{
    class RecommendedPackageModel : PackageModel
    {
        public RecommendedPackageModel(PackageIdentity identity,
            string? title = null,
            string? description = null,
            string? authors = null,
            Uri? projectUrl = null,
            string[]? tags = null,
            string? copyright = null)
            : base(identity, title, description, authors, projectUrl, tags, copyright)
        {
        }
    }
}
