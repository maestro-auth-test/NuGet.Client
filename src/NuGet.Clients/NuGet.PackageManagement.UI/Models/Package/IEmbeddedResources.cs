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
        ValueTask<Stream?> GetIconAsync(CancellationToken cancellationToken);

        ValueTask<Stream?> GetLicenseAsync(CancellationToken cancellationToken);

        ValueTask<Stream?> GetReadmeAsync(CancellationToken cancellationToken);
    }
}
