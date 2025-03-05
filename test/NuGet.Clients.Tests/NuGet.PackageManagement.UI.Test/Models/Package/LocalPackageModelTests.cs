// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using NuGet.VisualStudio.Internal.Contracts;
using Xunit;

namespace NuGet.PackageManagement.UI.Test
{
    public class LocalPackageModelTests
    {
        [Fact]
        public void LocalPackageModelCtr_IdAndVersion_ReturnsValueFromIdentity()
        {
            // Arrange
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);
            var packagePath = "C:\\TestPackage";

            // Act
            var package = new TestLocalPackageModel(identity, packagePath, vulnerableCapability);

            // Assert
            Assert.Equal("TestPackage", package.Id);
            Assert.Equal(new NuGetVersion("1.0.0"), package.Version);
        }

        [Fact]
        public void LocalPackageModelCtr_PackagePath_ReturnsExpected()
        {
            // Arrange
            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);
            var packagePath = "C:\\TestPackage";

            // Act
            var package = new TestLocalPackageModel(identity, packagePath, vulnerableCapability);

            // Assert
            Assert.Equal(packagePath, package.PackagePath);
        }

        [Fact]
        public void LocalPackageModel_IsVulnerableProperty_ReturnsExpected()
        {
            // Arrange
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);

            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var packagePath = "C:\\TestPackage";

            var package = new TestLocalPackageModel(identity, packagePath, vulnerableCapability);

            // Act
            var isVulnerable = package.IsPackageVulnerable();

            // Assert
            Assert.Equal(isVulnerable, true);
        }

        [Fact]
        public void LocalPackageModel_VulnerableCapability_HasVulnerabilities_ReturnsTrue()
        {
            // Arrange
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);

            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var packagePath = "C:\\TestPackage";

            var package = new TestLocalPackageModel(identity, packagePath, vulnerableCapability);

            // Act
            var hasVulnerabilities = package.GetPackageVulnerabilities();

            // Assert
            Assert.True(hasVulnerabilities.Any());
        }

        [Fact]
        public void LocalPackageModel_VulnerableCapability_GetPackageVulnerabilityMaxSeverity_ReturnsExpected()
        {
            // Arrange
            List<PackageVulnerabilityMetadataContextInfo> vulnerabilities = [new(new Uri("http://example.com"), 1)];
            var vulnerableCapability = new VulnerableCapability(vulnerabilities);

            var identity = new PackageIdentity("TestPackage", new NuGetVersion("1.0.0"));
            var packagePath = "C:\\TestPackage";

            var package = new TestLocalPackageModel(identity, packagePath, vulnerableCapability);

            // Act
            var maxSeverity = package.GetPackageVulnerabilityMaxSeverity();

            // Assert
            Assert.Equal(maxSeverity, Protocol.PackageVulnerabilitySeverity.Moderate);
        }
    }
}
