// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NuGet.PackageManagement.UI.Models.Package;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGet.VisualStudio.Internal.Contracts;
using Xunit;

namespace NuGet.PackageManagement.UI.Test.Models
{
    public class EmbeddedResourcesCapabilityTests
    {
        [Fact]
        public void LocalEmbeddedResourcesCapabilityCtor_SetsValues()
        {
            // Arrange
            var icon = new Uri(@"C:\Path\To\Icon.png");
            var license = new Uri(@"C:\Path\To\Icon.png");
            var readme = new Uri(@"C:\Path\To\Icon.png");
            var mockPackageFileService = new Mock<INuGetPackageFileService>();
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestPackageModel(identity);

            // Act
            var capability = new EmbeddedResourcesCapability(mockPackageFileService.Object, package, icon, license, readme);

            // Assert
            Assert.Equal(icon, capability.IconUri);
            Assert.Equal(license, capability.LicenseUri);
            Assert.Equal(readme, capability.ReadmeUri);
        }

        [Fact]
        public void LocalEmbeddedResourcesCapabilityCtor_WithNullValues_SetsValues()
        {
            // Arrange
            var mockPackageFileService = new Mock<INuGetPackageFileService>();
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestPackageModel(identity);

            // Act
            var capability = new EmbeddedResourcesCapability(mockPackageFileService.Object, package, null, null, null);

            // Assert
            Assert.Null(capability.IconUri);
            Assert.Null(capability.LicenseUri);
            Assert.Null(capability.ReadmeUri);
        }

        [Fact]
        public void LocalEmbeddedResourcesCapabilityCtor_WithNullService_Throws()
        {
            // Arrange
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestPackageModel(identity);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EmbeddedResourcesCapability(null, package, null, null, null));
        }


        [Fact]
        public void LocalEmbeddedResourcesCapabilityCtor_WithNullPackagee_Throws()
        {
            // Arrange
            var mockPackageFileService = new Mock<INuGetPackageFileService>();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new EmbeddedResourcesCapability(mockPackageFileService.Object, null, null, null, null));
        }

        [Fact]
        public async Task GetIconAsync_WithFile_ReturnsStream()
        {
            // Arrange
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("stream"));
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestPackageModel(identity);
            var mockPackageFileService = new Mock<INuGetPackageFileService>();
            mockPackageFileService.Setup(x => x.GetPackageIconAsync(It.IsAny<PackageIdentity>(), default)).ReturnsAsync(stream);
            EmbeddedResourcesCapability test = new EmbeddedResourcesCapability(mockPackageFileService.Object, package, new Uri(@"C:\path\to\image.png"), null, null);

            // Act
            var result = await test.GetIconAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            mockPackageFileService.Verify(x => x.GetPackageIconAsync(It.IsAny<PackageIdentity>(), default), Times.Once);
        }

        [Fact]
        public async Task GetLicenseAsync_WithFile_ReturnsStream()
        {
            // Arrange
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("stream"));
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestPackageModel(identity);
            var mockPackageFileService = new Mock<INuGetPackageFileService>();
            mockPackageFileService.Setup(x => x.GetEmbeddedLicenseAsync(It.IsAny<PackageIdentity>(), default)).ReturnsAsync(stream);
            EmbeddedResourcesCapability test = new EmbeddedResourcesCapability(mockPackageFileService.Object, package, null, new Uri(@"C:\path\to\image.png"), null);

            // Act
            var result = await test.GetLicenseAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            mockPackageFileService.Verify(x => x.GetEmbeddedLicenseAsync(It.IsAny<PackageIdentity>(), default), Times.Once);
        }


        [Fact]
        public async Task GetReadmeAsync_WithUrl_ReturnsStream()
        {
            // Arrange
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("stream"));
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestPackageModel(identity);
            var mockPackageFileService = new Mock<INuGetPackageFileService>();
            mockPackageFileService.Setup(x => x.GetReadmeAsync(It.IsAny<Uri>(), default)).ReturnsAsync(stream);
            EmbeddedResourcesCapability test = new EmbeddedResourcesCapability(mockPackageFileService.Object, package, null, null, new Uri(@"C:\path\to\image.png"));

            // Act
            var result = await test.GetReadmeAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            mockPackageFileService.Verify(x => x.GetReadmeAsync(It.IsAny<Uri>(), default), Times.Once);
        }

        [Fact]
        public async Task GetReadmeAsync_NoUrl_ReturnsNull()
        {
            // Arrange
            using Stream stream = new MemoryStream(Encoding.UTF8.GetBytes("stream"));
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestPackageModel(identity);
            var mockPackageFileService = new Mock<INuGetPackageFileService>();
            mockPackageFileService.Setup(x => x.GetReadmeAsync(It.IsAny<Uri>(), default)).ReturnsAsync(stream);
            EmbeddedResourcesCapability test = new EmbeddedResourcesCapability(mockPackageFileService.Object, package, null, null, null);

            // Act
            var result = await test.GetReadmeAsync(CancellationToken.None);

            // Assert
            Assert.Null(result);
            mockPackageFileService.Verify(x => x.GetReadmeAsync(It.IsAny<Uri>(), default), Times.Never);
        }
    }
}
