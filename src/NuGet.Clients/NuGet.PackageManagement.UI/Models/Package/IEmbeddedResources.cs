// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace NuGet.PackageManagement.UI
{
    internal interface IEmbeddedResources
    {
        Uri? IconUri { get; }

        Uri? LicenseUri { get; }

        Uri? ReadmeUri { get; }

        Task<BitmapSource?> GetIconAsync(CancellationToken cancellationToken);

        Task<string?> GetLicenseAsync(CancellationToken cancellationToken);

        Task<string?> GetReadmeAsync(CancellationToken cancellationToken);
    }
}
