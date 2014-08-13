﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetInstallCommandTest
    {
        [Fact]
        public void InstallCommand_PackageSaveModeNuspec()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                // Act
                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nuspec" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nuspecFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nuspec");

                Assert.True(File.Exists(nuspecFile));
                var nupkgFiles = Directory.GetFiles(outputDirectory, "*.nupkg", SearchOption.AllDirectories);
                Assert.Equal(0, nupkgFiles.Length);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        [Fact]
        public void InstallCommand_PackageSaveModeNupkg()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                // Act
                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nupkg" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nupkgFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nupkg");

                Assert.True(File.Exists(nupkgFile));
                var nuspecFiles = Directory.GetFiles(outputDirectory, "*.nuspec", SearchOption.AllDirectories);
                Assert.Equal(0, nuspecFiles.Length);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        [Fact]
        public void InstallCommand_PackageSaveModeNuspecNupkg()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                // Act
                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nupkg;nuspec" };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nupkgFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nupkg");
                var nuspecFile = Path.ChangeExtension(nupkgFile, "nuspec");

                Assert.True(File.Exists(nupkgFile));
                Assert.True(File.Exists(nuspecFile));
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        // Test that after a package is installed with -PackageSaveMode nuspec, nuget.exe
        // can detect that the package is already installed when trying to install the same
        // package.
        [Fact]
        public void InstallCommand_PackageSaveModeNuspecReinstall()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);

                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-PackageSaveMode", "nuspec" };
                int r = Program.Main(args);
                Assert.Equal(0, r);

                // Act
                MemoryStream memoryStream = new MemoryStream();
                TextWriter writer = new StreamWriter(memoryStream);
                Console.SetOut(writer);
                r = Program.Main(args);
                writer.Close();
                var output = Encoding.Default.GetString(memoryStream.ToArray());

                // Assert
                var expectedOutput = "'testPackage1 1.1.0' already installed." +
                    Environment.NewLine;
                Assert.Equal(expectedOutput, output);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }

        // Test that PackageSaveMode specified in nuget.config file is used.
        [Fact]
        public void InstallCommand_PackageSaveModeInConfigFile()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = Util.CreateTestPackage(
                    "testPackage1", "1.1.0", source);

                var configFile = Path.Combine(source, "nuget.config");                
                Util.CreateFile(Path.GetDirectoryName(configFile), Path.GetFileName(configFile), "<configuration/>");
                string[] args = new string[] { 
                    "config", "-Set", "PackageSaveMode=nuspec",
                    "-ConfigFile", configFile };
                int r = Program.Main(args);
                Assert.Equal(0, r);

                // Act
                args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source, 
                    "-ConfigFile", configFile };
                r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);

                var nuspecFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\testPackage1.1.1.0.nuspec");

                Assert.True(File.Exists(nuspecFile));
                var nupkgFiles = Directory.GetFiles(outputDirectory, "*.nupkg", SearchOption.AllDirectories);
                Assert.Equal(0, nupkgFiles.Length);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
			}
		}

        // Tests that when package restore is enabled and -RequireConsent is specified,
        // the opt out message is displayed.
        [Theory]
        [InlineData("packages.config")]
        [InlineData("packages.proj1.config")]
        public void InstallCommand_OptOutMessage(string configFileName)
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var workingPath = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var repositoryPath = Path.Combine(workingPath, Guid.NewGuid().ToString());
            var proj1Directory = Path.Combine(workingPath, "proj1");
            var currentDirectory = Directory.GetCurrentDirectory();
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");

            try
            {
                Util.CreateDirectory(workingPath);
                Util.CreateDirectory(repositoryPath);
                Util.CreateDirectory(proj1Directory);

                Util.CreateTestPackage("packageA", "1.1.0", repositoryPath);
                Util.CreateTestPackage("packageB", "2.2.0", repositoryPath);

                Util.CreateFile(workingPath, "my.config",
                    @"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageRestore>
    <add key=""enabled"" value=""True"" />
  </packageRestore>
</configuration>");

                Util.CreateFile(proj1Directory, "proj1.csproj",
                    @"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <None Include='packages.config' />
  </ItemGroup>
</Project>");
                Util.CreateFile(proj1Directory, configFileName,
@"<packages>
  <package id=""packageA"" version=""1.1.0"" targetFramework=""net45"" />
</packages>");
                // Act 
                var r = CommandRunner.Run(
                    nugetexe,
                    proj1Directory,
                    "install " + configFileName + " -Source " + repositoryPath + @" -ConfigFile ..\my.config -RequireConsent -Verbosity detailed",
                    waitForExit: true);

                // Assert
                Assert.Equal(0, r.Item1);
                string optOutMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.RestoreCommandPackageRestoreOptOutMessage,
                    NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                Assert.Contains(optOutMessage, r.Item2);                
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
                Util.DeleteDirectory(workingPath);
            }
        }

        // Tests that when package restore is enabled, but -RequireConsent is not specified,
        // the opt out message is not displayed.
        [Theory]
        [InlineData("packages.config")]
        [InlineData("packages.proj1.config")]
        public void InstallCommand_NoOptOutMessage(string configFileName)
        {
            // Arrange
            var tempPath = Path.GetTempPath();
            var workingPath = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var repositoryPath = Path.Combine(workingPath, Guid.NewGuid().ToString());
            var proj1Directory = Path.Combine(workingPath, "proj1");
            var currentDirectory = Directory.GetCurrentDirectory();
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");

            try
            {
                Util.CreateDirectory(workingPath);
                Util.CreateDirectory(repositoryPath);
                Util.CreateDirectory(proj1Directory);

                Util.CreateTestPackage("packageA", "1.1.0", repositoryPath);
                Util.CreateTestPackage("packageB", "2.2.0", repositoryPath);

                Util.CreateFile(workingPath, "my.config",
                    @"
<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <packageRestore>
    <add key=""enabled"" value=""True"" />
  </packageRestore>
</configuration>");

                Util.CreateFile(proj1Directory, "proj1.csproj",
                    @"<Project ToolsVersion='4.0' DefaultTargets='Build' 
    xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <OutputPath>out</OutputPath>
    <TargetFrameworkVersion>v4.0</TargetFrameworkVersion>
  </PropertyGroup>
  <ItemGroup>
    <None Include='packages.config' />
  </ItemGroup>
</Project>");
                Util.CreateFile(proj1Directory, configFileName,
@"<packages>
  <package id=""packageA"" version=""1.1.0"" targetFramework=""net45"" />
</packages>");
                // Act 
                var r = CommandRunner.Run(
                    nugetexe,
                    proj1Directory,
                    "install " + configFileName + " -Source " + repositoryPath + @" -ConfigFile ..\my.config",
                    waitForExit: true);

                // Assert
                Assert.Equal(0, r.Item1);
                string optOutMessage = String.Format(
                    CultureInfo.CurrentCulture,
                    NuGetResources.RestoreCommandPackageRestoreOptOutMessage,
                    NuGet.Resources.NuGetResources.PackageRestoreConsentCheckBoxText.Replace("&", ""));
                Assert.DoesNotContain(optOutMessage, r.Item2);
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
                Util.DeleteDirectory(workingPath);
            }
        }

        // Tests that when no version is specified, nuget will query the server to get
        // the latest version number first.
        [Fact]
        public void InstallCommand_GetLastestReleaseVersion()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var workingDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                Util.CreateDirectory(workingDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                var package = new ZipPackage(packageFileName);
                MachineCache.Default.RemovePackage(package);

                var server = new MockServer(mockServerEndPoint);
                string findPackagesByIdRequest = string.Empty;
                bool packageDownloadIsCalled = false;

                server.Get.Add("/nuget/FindPackagesById()", r => 
                    new Action<HttpListenerResponse>(response =>
                    {
                        findPackagesByIdRequest = r.Url.ToString();
                        response.ContentType = "application/atom+xml;type=feed;charset=utf-8";
                        string feed = server.ToODataFeed(new[] { package }, "FindPackagesById");
                        MockServer.SetResponseContent(response,  feed);
                    }));

                server.Get.Add("/nuget/Packages(Id='testPackage1',Version='1.1.0')", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        response.ContentType = "application/atom+xml;type=entry;charset=utf-8";
                        var p1 = server.ToOData(package);
                        MockServer.SetResponseContent(response, p1);
                    }));

                server.Get.Add("/package/testPackage1", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        packageDownloadIsCalled = true;
                        response.ContentType = "application/zip";
                        using (var stream = package.GetStream())
                        {
                            var content = stream.ReadAllBytes();
                            MockServer.SetResponseContent(response, content);
                        }
                    }));

                server.Get.Add("/nuget", r => "OK");

                server.Start();

                // Act
                var args = "install testPackage1 -Source " + mockServerEndPoint + "nuget";
                var r1 = CommandRunner.Run(
                    nugetexe,
                    workingDirectory,
                    args,
                    waitForExit: true);
                server.Stop();

                // Assert
                Assert.Equal(0, r1.Item1);
                Assert.Contains("$filter=IsLatestVersion", findPackagesByIdRequest);
                Assert.True(packageDownloadIsCalled);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(workingDirectory);
            }
        }

        // Tests that when no version is specified, and -Prerelease is specified,
        // nuget will query the server to get the latest prerelease version number first.
        [Fact]
        public void InstallCommand_GetLastestPrereleaseVersion()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var workingDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                Util.CreateDirectory(workingDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                var package = new ZipPackage(packageFileName);
                MachineCache.Default.RemovePackage(package);

                var server = new MockServer(mockServerEndPoint);
                string findPackagesByIdRequest = string.Empty;
                bool packageDownloadIsCalled = false;

                server.Get.Add("/nuget/FindPackagesById()", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        findPackagesByIdRequest = r.Url.ToString();
                        response.ContentType = "application/atom+xml;type=feed;charset=utf-8";
                        string feed = server.ToODataFeed(new[] { package }, "FindPackagesById");
                        MockServer.SetResponseContent(response, feed);
                    }));

                server.Get.Add("/nuget/Packages(Id='testPackage1',Version='1.1.0')", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        response.ContentType = "application/atom+xml;type=entry;charset=utf-8";
                        var p1 = server.ToOData(package);
                        MockServer.SetResponseContent(response, p1);
                    }));

                server.Get.Add("/package/testPackage1", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        packageDownloadIsCalled = true;
                        response.ContentType = "application/zip";
                        using (var stream = package.GetStream())
                        {
                            var content = stream.ReadAllBytes();
                            MockServer.SetResponseContent(response, content);
                        }
                    }));

                server.Get.Add("/nuget", r => "OK");

                server.Start();

                // Act
                var args = "install testPackage1 -Prerelease -Source " + mockServerEndPoint + "nuget";
                var r1 = CommandRunner.Run(
                    nugetexe,
                    workingDirectory,
                    args,
                    waitForExit: true);
                server.Stop();

                // Assert
                Assert.Equal(0, r1.Item1);
                Assert.Contains("$filter=IsAbsoluteLatestVersion", findPackagesByIdRequest);
                Assert.True(packageDownloadIsCalled);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(workingDirectory);
            }
        }

        // Tests that when -Version is specified, nuget will use request 
        // Packages(Id='id',Version='version') to get the specified version
        [Fact]
        public void InstallCommand_WithVersionSpecified()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var tempPath = Path.GetTempPath();
            var workingDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                Util.CreateDirectory(workingDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                var package = new ZipPackage(packageFileName);
                MachineCache.Default.RemovePackage(package);

                var server = new MockServer(mockServerEndPoint);
                bool getPackageByVersionIsCalled = false;
                bool packageDownloadIsCalled = false;

                server.Get.Add("/nuget/Packages(Id='testPackage1',Version='1.1.0')", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        getPackageByVersionIsCalled = true;
                        response.ContentType = "application/atom+xml;type=entry;charset=utf-8";
                        var p1 = server.ToOData(package);
                        MockServer.SetResponseContent(response, p1);
                    }));

                server.Get.Add("/package/testPackage1", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        packageDownloadIsCalled = true;
                        response.ContentType = "application/zip";
                        using (var stream = package.GetStream())
                        {
                            var content = stream.ReadAllBytes();
                            MockServer.SetResponseContent(response, content);
                        }
                    }));

                server.Get.Add("/nuget", r => "OK");

                server.Start();

                // Act
                var args = "install testPackage1 -Version 1.1.0 -Source " + mockServerEndPoint + "nuget";
                var r1 = CommandRunner.Run(
                    nugetexe,
                    workingDirectory,
                    args,
                    waitForExit: true);
                server.Stop();

                // Assert
                Assert.Equal(0, r1.Item1);
                Assert.True(getPackageByVersionIsCalled);
                Assert.True(packageDownloadIsCalled);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(workingDirectory);
            }
        }

        // Tests that when -Version is specified, if the specified version cannot be found,
        // nuget will retry with new version numbers by appending 0's to the specified version.
        [Fact]
        public void InstallCommand_WillTryNewVersionsByAppendingZeros()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var tempPath = Path.GetTempPath();
            var workingDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(workingDirectory);

                // deleting testPackage1 from machine cache
                var packages = MachineCache.Default.FindPackagesById("testPackage1");
                foreach (var p in packages)
                {
                    MachineCache.Default.RemovePackage(p);
                }
                
                var server = new MockServer(mockServerEndPoint);
                List<string> requests = new List<string>();
                server.Get.Add("/nuget/Packages", r =>
                    {
                        requests.Add(r.Url.ToString());
                        return HttpStatusCode.NotFound;
                    });
                server.Get.Add("/nuget", r => "OK");

                server.Start();

                // Act
                var args = "install testPackage1 -Version 1.1 -Source " + mockServerEndPoint + "nuget";
                var r1 = CommandRunner.Run(
                    nugetexe,
                    workingDirectory,
                    args,
                    waitForExit: true);
                server.Stop();

                // Assert
                Assert.Equal(1, r1.Item1);

                Assert.Equal(3, requests.Count);
                Assert.True(requests[0].EndsWith("Packages(Id='testPackage1',Version='1.1')"));
                Assert.True(requests[1].EndsWith("Packages(Id='testPackage1',Version='1.1.0')"));
                Assert.True(requests[2].EndsWith("Packages(Id='testPackage1',Version='1.1.0.0')"));
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(workingDirectory);
            }
        }

        // Tests that nuget will NOT download package from http source if the package on the server
        // has the same hash value as the cached version.
        [Fact]
        public void InstallCommand_WillUseCachedFile()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var workingDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                Util.CreateDirectory(workingDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                var package = new ZipPackage(packageFileName);
                MachineCache.Default.RemovePackage(package);

                // add the package to machine cache
                MachineCache.Default.AddPackage(package);

                var server = new MockServer(mockServerEndPoint);
                string findPackagesByIdRequest = string.Empty;
                bool packageDownloadIsCalled = false;

                server.Get.Add("/nuget/FindPackagesById()", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        findPackagesByIdRequest = r.Url.ToString();
                        response.ContentType = "application/atom+xml;type=feed;charset=utf-8";
                        string feed = server.ToODataFeed(new[] { package }, "FindPackagesById");
                        MockServer.SetResponseContent(response, feed);
                    }));

                server.Get.Add("/nuget/Packages(Id='testPackage1',Version='1.1.0')", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        response.ContentType = "application/atom+xml;type=entry;charset=utf-8";
                        var p1 = server.ToOData(package);
                        MockServer.SetResponseContent(response, p1);
                    }));

                server.Get.Add("/package/testPackage1", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        packageDownloadIsCalled = true;
                        response.ContentType = "application/zip";
                        using (var stream = package.GetStream())
                        {
                            var content = stream.ReadAllBytes();
                            MockServer.SetResponseContent(response, content);
                        }
                    }));

                server.Get.Add("/nuget", r => "OK");

                server.Start();

                // Act
                var args = "install testPackage1 -Source " + mockServerEndPoint + "nuget";
                var r1 = CommandRunner.Run(
                    nugetexe,
                    workingDirectory,
                    args,
                    waitForExit: true);
                server.Stop();

                // Assert
                Assert.Equal(0, r1.Item1);
                Assert.Contains("$filter=IsLatestVersion", findPackagesByIdRequest);

                // verifies that package is NOT downloaded from server since nuget uses
                // the file in machine cache.
                Assert.False(packageDownloadIsCalled);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(workingDirectory);
            }
        }

        // Tests that nuget will download package from http source if the package on the server
        // has a different hash value from the cached version.
        [Fact]
        public void InstallCommand_DownloadPackageWhenHashChanges()
        {
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var tempPath = Path.GetTempPath();
            var packageDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var workingDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var mockServerEndPoint = "http://localhost:1234/";

            try
            {
                // Arrange
                Util.CreateDirectory(packageDirectory);
                Util.CreateDirectory(workingDirectory);
                var packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                var package = new ZipPackage(packageFileName);
                MachineCache.Default.RemovePackage(package);

                // add the package to machine cache
                MachineCache.Default.AddPackage(package);

                // create a new package. Now this package has different hash value from the package in
                // the machine cache.
                packageFileName = Util.CreateTestPackage("testPackage1", "1.1.0", packageDirectory);
                package = new ZipPackage(packageFileName);

                var server = new MockServer(mockServerEndPoint);
                string findPackagesByIdRequest = string.Empty;
                bool packageDownloadIsCalled = false;

                server.Get.Add("/nuget/FindPackagesById()", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        findPackagesByIdRequest = r.Url.ToString();
                        response.ContentType = "application/atom+xml;type=feed;charset=utf-8";
                        string feed = server.ToODataFeed(new[] { package }, "FindPackagesById");
                        MockServer.SetResponseContent(response, feed);
                    }));

                server.Get.Add("/nuget/Packages(Id='testPackage1',Version='1.1.0')", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        response.ContentType = "application/atom+xml;type=entry;charset=utf-8";
                        var p1 = server.ToOData(package);
                        MockServer.SetResponseContent(response, p1);
                    }));

                server.Get.Add("/package/testPackage1", r =>
                    new Action<HttpListenerResponse>(response =>
                    {
                        packageDownloadIsCalled = true;
                        response.ContentType = "application/zip";
                        using (var stream = package.GetStream())
                        {
                            var content = stream.ReadAllBytes();
                            MockServer.SetResponseContent(response, content);
                        }
                    }));

                server.Get.Add("/nuget", r => "OK");

                server.Start();

                // Act
                var args = "install testPackage1 -Source " + mockServerEndPoint + "nuget";
                var r1 = CommandRunner.Run(
                    nugetexe,
                    workingDirectory,
                    args,
                    waitForExit: true);
                server.Stop();

                // Assert
                Assert.Equal(0, r1.Item1);
                Assert.Contains("$filter=IsLatestVersion", findPackagesByIdRequest);

                // verifies that package is downloaded from server since the cached version has
                // a different hash from the package on the server.
                Assert.True(packageDownloadIsCalled);
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(packageDirectory);
                Util.DeleteDirectory(workingDirectory);
            }
        }


        // Tests that when both the normal package and the symbol package exist in a local repository,
        // nuget install should pick the normal package.
        [Fact]
        public void InstallCommand_PreferNonSymbolPackage()
        {
            var tempPath = Path.GetTempPath();
            var source = Path.Combine(tempPath, Guid.NewGuid().ToString());
            var outputDirectory = Path.Combine(tempPath, Guid.NewGuid().ToString());

            try
            {
                // Arrange            
                Util.CreateDirectory(source);
                Util.CreateDirectory(outputDirectory);

                var packageFileName = PackageCreater.CreatePackage(
                    "testPackage1", "1.1.0", source);
                var symbolPackageFileName = PackageCreater.CreateSymbolPackage(
                    "testPackage1", "1.1.0", source);

                // Act
                string[] args = new string[] { 
                    "install", "testPackage1", 
                    "-OutputDirectory", outputDirectory,
                    "-Source", source };
                int r = Program.Main(args);

                // Assert
                Assert.Equal(0, r);
                var testTxtFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\content\test1.txt");
                Assert.True(File.Exists(testTxtFile));

                var symbolTxtFile = Path.Combine(
                    outputDirectory,
                    @"testPackage1.1.1.0\symbol.txt");
                Assert.False(File.Exists(symbolTxtFile));
            }
            finally
            {
                // Cleanup
                Util.DeleteDirectory(outputDirectory);
                Util.DeleteDirectory(source);
            }
        }
    }
}
