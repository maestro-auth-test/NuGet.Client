// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.VisualStudio.Internal.Contracts;

namespace NuGet.PackageManagement.UI.Models.Package
{
    internal class EmbeddedResourcesCapability : IEmbeddedResources
    {
        private INuGetPackageFileService _nugetPackageFileService;
        private PackageIdentity _packageIdentity;
        private Uri? _readmeUri;

        public EmbeddedResourcesCapability(INuGetPackageFileService nugetPackageFileService, PackageIdentity packageIdentity, Uri? readmeUri)
        {
            _nugetPackageFileService = nugetPackageFileService ?? throw new ArgumentNullException(nameof(nugetPackageFileService));
            _packageIdentity = packageIdentity ?? throw new ArgumentNullException(nameof(packageIdentity));
            _readmeUri = readmeUri;
        }

        public async Task<Stream?> GetIconAsync(CancellationToken cancellationToken)
        {
            return await _nugetPackageFileService.GetPackageIconAsync(_packageIdentity, cancellationToken);
        }

        public async Task<Stream?> GetLicenseAsync(CancellationToken cancellationToken)
        {
            return await _nugetPackageFileService.GetEmbeddedLicenseAsync(_packageIdentity, cancellationToken);
        }

        public async Task<Stream?> GetReadmeAsync(CancellationToken cancellationToken)
        {
            if (_readmeUri != null)
            {
                return await _nugetPackageFileService.GetReadmeAsync(_readmeUri, cancellationToken);
            }
            return null;
        }
    }
}
