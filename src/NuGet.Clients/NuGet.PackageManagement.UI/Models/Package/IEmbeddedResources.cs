// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NuGet.PackageManagement.UI
{
    internal interface IEmbeddedResources
    {
        Task<Stream?> GetIconAsync(CancellationToken cancellationToken);

        Task<Stream?> GetLicenseAsync(CancellationToken cancellationToken);

        Task<Stream?> GetReadmeAsync(CancellationToken cancellationToken);
    }
}
