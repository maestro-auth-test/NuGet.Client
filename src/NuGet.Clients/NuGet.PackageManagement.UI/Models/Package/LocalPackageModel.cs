using System;
using NuGet.Packaging.Core;

#nullable enable

namespace NuGet.PackageManagement.UI
{
    class LocalPackageModel : PackageModel
    {
        public LocalPackageModel(PackageIdentity identity,
            string packagePath,
            string? title = null,
            string? description = null,
            string? authors = null,
            Uri? projectUrl = null,
            string[]? tags = null,
            string? copyright = null)
            : base(identity, title, description, authors, projectUrl, tags, copyright)
        {
            PackagePath = packagePath;
        }

        public string PackagePath { get; set; }
    }
}
