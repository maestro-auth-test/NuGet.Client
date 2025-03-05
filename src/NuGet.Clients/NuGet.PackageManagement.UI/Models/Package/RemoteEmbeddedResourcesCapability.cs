// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
#nullable enable

using System;
using System.IO;
using System.Net.Cache;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NuGet.PackageManagement.UI.Models.Package
{
    internal class RemoteEmbeddedResourcesCapability : IEmbeddedResources
    {
        internal const int DecodePixelWidth = 32;
        private static readonly RequestCachePolicy RequestCacheIfAvailable = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
        private HttpSource _httpSource;

        public RemoteEmbeddedResourcesCapability(HttpSource httpSource, Uri? iconUri, Uri? licenseUri, Uri? readmeUri)
        {
            _httpSource = httpSource ?? throw new ArgumentNullException(nameof(httpSource));
            IconUri = iconUri;
            LicenseUri = licenseUri;
            ReadmeUri = readmeUri;
        }

        public Uri? IconUri { get; }

        public Uri? LicenseUri { get; }

        public Uri? ReadmeUri { get; }

        public async Task<BitmapSource?> GetIconAsync(CancellationToken cancellationToken)
        {
            if (IconUri != null)
            {
                return await GetEmbeddedFileAsync(IconUri, ProcessBitmapSourceResult, cancellationToken);
            }

            return null;
        }

        public async Task<string?> GetLicenseAsync(CancellationToken cancellationToken)
        {
            if (LicenseUri != null)
            {
                return await GetEmbeddedFileAsync(LicenseUri, ProcessStringResultAsync, cancellationToken);
            }

            return null;
        }

        public async Task<string?> GetReadmeAsync(CancellationToken cancellationToken)
        {
            if (ReadmeUri != null)
            {
                return await GetEmbeddedFileAsync(ReadmeUri, ProcessStringResultAsync, cancellationToken);
            }

            return null;
        }

        private static async Task<string?> ProcessStringResultAsync(HttpSourceResult result)
        {
            if (result.Stream != null)
            {
                using StreamReader streamReader = new StreamReader(result.Stream);
                return await streamReader.ReadToEndAsync();
            }
            return null;
        }

        private static Task<BitmapImage?> ProcessBitmapSourceResult(HttpSourceResult result)
        {
            if (result.Stream != null)
            {
                using StreamReader streamReader = new StreamReader(result.Stream);
                BitmapImage iconBitmapImage = new BitmapImage();
                iconBitmapImage.BeginInit();

                // BitmapImage can download on its own from URIs, but in order
                // to support downloading on a worker thread, we need to download the image
                // data and put into a memorystream. Then have the BitmapImage decode the
                // image from the memorystream.
                using var memoryStream = new MemoryStream();
                // Cannot call CopyToAsync as we'll get an InvalidOperationException due to CheckAccess() in next line.
                result.Stream.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                iconBitmapImage.StreamSource = memoryStream;

                // Default cache policy: Per MSDN, satisfies a request for a resource either by using the cached copy of the resource or by sending a request
                // for the resource to the server. The action taken is determined by the current cache policy and the age of the content in the cache.
                // This is the cache level that should be used by most applications.
                iconBitmapImage.UriCachePolicy = RequestCacheIfAvailable;

                // Instead of scaling larger images and keeping larger image in memory, this makes it so we scale it down, and throw away the bigger image.
                // Only need to set this on one dimension, to preserve aspect ratio
                iconBitmapImage.DecodePixelWidth = DecodePixelWidth;

                // Workaround for https://github.com/dotnet/wpf/issues/3503
                iconBitmapImage.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

                iconBitmapImage.CacheOption = BitmapCacheOption.OnLoad;

                iconBitmapImage.EndInit();
                iconBitmapImage.Freeze();
                return Task.FromResult<BitmapImage?>(iconBitmapImage);
            }
            return Task.FromResult<BitmapImage?>(null);
        }

        private async Task<T?> GetEmbeddedFileAsync<T>(
            Uri uri,
            Func<HttpSourceResult, Task<T>> processAsync,
            CancellationToken cancellationToken)
        {
            using SourceCacheContext cacheContext = new SourceCacheContext();
            var httpSourceCacheContext = HttpSourceCacheContext.Create(cacheContext, 0);

            return await _httpSource.GetAsync(
                new HttpSourceCachedRequest(
                    uri.AbsoluteUri,
                    uri.AbsoluteUri,
                    httpSourceCacheContext)
                {
                    IgnoreNotFounds = true,
                },
                processAsync,
                NullLogger.Instance,
                cancellationToken);
        }
    }
}
