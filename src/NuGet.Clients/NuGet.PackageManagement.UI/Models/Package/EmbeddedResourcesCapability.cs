// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.VisualStudio.Internal.Contracts;

namespace NuGet.PackageManagement.UI.Models.Package
{
    internal class EmbeddedResourcesCapability : IEmbeddedResources
    {
        private INuGetPackageFileService _nugetPackageFileService;
        private PackageModel _package;

        public EmbeddedResourcesCapability(INuGetPackageFileService nugetPackageFileService, PackageModel package, Uri? iconUri, Uri? licenseUri, Uri? readmeUri)
        {
            _nugetPackageFileService = nugetPackageFileService ?? throw new ArgumentNullException(nameof(nugetPackageFileService));
            _package = package ?? throw new ArgumentNullException(nameof(package));
            IconUri = iconUri;
            LicenseUri = licenseUri;
            ReadmeUri = readmeUri;
        }

        public Uri? IconUri { get; }

        public Uri? LicenseUri { get; }

        public Uri? ReadmeUri { get; }

        public async Task<Stream?> GetIconAsync(CancellationToken cancellationToken)
        {
            return await _nugetPackageFileService.GetPackageIconAsync(_package.Identity, cancellationToken);
        }

        public async Task<Stream?> GetLicenseAsync(CancellationToken cancellationToken)
        {
            return await _nugetPackageFileService.GetEmbeddedLicenseAsync(_package.Identity, cancellationToken);
        }

        public async Task<Stream?> GetReadmeAsync(CancellationToken cancellationToken)
        {
            if (ReadmeUri != null)
            {
                return await _nugetPackageFileService.GetReadmeAsync(ReadmeUri, cancellationToken);
            }
            return null;
        }
    }
}
