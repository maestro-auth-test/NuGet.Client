// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGet.VisualStudio.Internal.Contracts;
using Xunit;

namespace NuGet.PackageManagement.UI.Test
{
    public class RemotePackageModelTests
    {
        [Fact]
        public void RemotePackageModelCtr_IdAndVersion_ReturnsValueFromIdentity()
        {
            // Arrange
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);

            // Act
            var package = new TestRemotePackageModel(identity, vulnerableCapability);

            // Assert
            Assert.Equal("TestPackage", package.Id);
            Assert.Equal(new NuGetVersion("1.0.0"), package.Version);
        }

        [Fact]
        public void RemotePackageModelCtr_OptionalParameters_ReturnsExpected()
        {
            // Arrange
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);
            var isListed = true;
            var dateTimeOffset = DateTimeOffset.Now;
            var packageDetailsUrl = new Uri("http://example.com");
            var downloadCount = 1000;
            var framework = new NuGetFramework("net8.0");
            var dependencySets = new List<PackageDependencyGroup> { new PackageDependencyGroup(framework, [new PackageDependency("non_existing", VersionRange.Parse("1.1"))]) };

            // Act
            var package = new TestRemotePackageModel(identity, vulnerableCapability, null, null, null, null, null, null, isListed, dateTimeOffset, packageDetailsUrl, downloadCount, dependencySets);

            // Assert
            Assert.Equal("TestPackage", package.Id);
            Assert.Equal(new NuGetVersion("1.0.0"), package.Version);
            Assert.Equal(isListed, package.IsListed);
            Assert.Equal(dateTimeOffset, package.Published);
            Assert.Equal(packageDetailsUrl, package.PackageDetailsUrl);
            Assert.Equal(downloadCount, package.DownloadCount);
            Assert.Equal(dependencySets, package.DependencySets);
        }

        [Fact]
        public void RemotePackageModel_IsVulnerableProperty_ReturnsExpected()
        {
            // Arrange
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);

            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestRemotePackageModel(identity, vulnerableCapability);

            // Act
            var isVulnerable = package.IsPackageVulnerable();

            // Assert
            Assert.Equal(isVulnerable, true);
        }

        [Fact]
        public void RemotePackageModel_VulnerableCapability_HasVulnerabilities_ReturnsTrue()
        {
            // Arrange
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);

            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestRemotePackageModel(identity, vulnerableCapability);

            // Act
            var hasVulnerabilities = package.GetPackageVulnerabilities();

            // Assert
            Assert.True(hasVulnerabilities.Any());
        }

        [Fact]
        public void RemotePackageModel_VulnerableCapability_GetPackageVulnerabilityMaxSeverity_ReturnsExpected()
        {
            // Arrange
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);

            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var package = new TestRemotePackageModel(identity, vulnerableCapability);

            // Act
            var maxSeverity = package.GetPackageVulnerabilityMaxSeverity();

            // Assert
            Assert.Equal(maxSeverity, Protocol.PackageVulnerabilitySeverity.Moderate);
        }
    }
}
