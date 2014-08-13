﻿using Moq;
using NuGet.Commands;
using NuGet.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class ProjectFactoryTest
    {
        [Fact]
        public void ProjectFactoryCanCompareContentsOfReadOnlyFile()
        {
            var us = Assembly.GetExecutingAssembly();
            var sourcePath = us.Location;
            var targetFile = new PhysicalPackageFile { SourcePath = sourcePath };
            var fullPath = sourcePath + "readOnly";
            File.Copy(sourcePath, fullPath);
            File.SetAttributes(fullPath, FileAttributes.ReadOnly);
            try
            {
                var actual = ProjectFactory.ContentEquals(targetFile, fullPath);

                Assert.Equal(true, actual);
            }
            finally
            {
                File.SetAttributes(fullPath, FileAttributes.Normal);
                File.Delete(fullPath);
            }
        }

        /// <summary>
        /// This test ensures that when building a nuget package from a project file (e.g. .csproj)
        /// that if the case doesn't match between a file in the .nuspec file and the file on disk
        /// that NuGet won't attempt to add it again.
        /// </summary>
        /// <example>
        /// Given: The .nuspec file contains &quot;Assembly.xml&quot; and the file on disk is &quot;Assembly.XML.&quot;
        /// Command: nuget pack Assembly.csproj
        /// Output: Exception: An item with the key already exists
        /// </example>
        [Fact]
        public void EnsureProjectFactoryDoesNotAddFileThatIsAlreadyInPackage()
        {
            // Setup
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");
            var workingDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            
            try
            {
                // Arrange
                Util.CreateDirectory(workingDirectory);
                Util.CreateFile(workingDirectory, "Assembly.nuspec", GetNuspecContent());
                Util.CreateFile(workingDirectory, "Assembly.csproj", GetProjectContent());
                Util.CreateFile(workingDirectory, "Source.cs", GetSourceFileContent());
                var projPath = Path.Combine(workingDirectory, "Assembly.csproj");

                // Act 
                var actual = CommandRunner.Run(
                    nugetexe,
                    workingDirectory,
                    "pack Assembly.csproj -build",
                    waitForExit: true);
                var package = new OptimizedZipPackage(Path.Combine(workingDirectory, "Assembly.1.0.0.nupkg"));
                var files = package.GetFiles().Select(f => f.Path).ToArray();

                // Assert
                Assert.Equal(0, actual.Item1);
                    Array.Sort(files);
                    Assert.Equal(files, new[] { 
                    @"lib\net45\Assembly.dll",
                    @"lib\net45\Assembly.xml" 
                });
            }
            finally
            {
                // Teardown
                Directory.Delete(workingDirectory, true);
            }
        }

        private static string GetNuspecContent()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<package xmlns=""http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd"">
    <metadata>
        <id>Assembly</id>
        <version>1.0.0</version>
        <title />
        <authors>Author</authors>
        <owners />
        <requireLicenseAcceptance>false</requireLicenseAcceptance>
        <description>Description for Assembly.</description>
    </metadata>
    <files>
        <file src=""bin\Debug\Assembly.dll"" target=""lib\net45\Assembly.dll"" />
        <file src=""bin\Debug\Assembly.xml"" target=""lib\net45\Assembly.xml"" />
    </files>
</package>";
        }

        private static string GetProjectContent()
        {
            return @"<?xml version=""1.0"" encoding=""utf-8""?>
<Project ToolsVersion=""4.0"" DefaultTargets=""Build"" xmlns=""http://schemas.microsoft.com/developer/msbuild/2003"">
  <Import Project=""$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props"" Condition=""Exists('$(MSBuildExtensionsPath)\$(MSBuildToolsVersion)\Microsoft.Common.props')"" />
  <PropertyGroup>
    <Configuration Condition="" '$(Configuration)' == '' "">Debug</Configuration>
    <Platform Condition="" '$(Platform)' == '' "">AnyCPU</Platform>
    <ProjectGuid>{CD08AD03-0CBF-47B1-8A95-D9E9C2330F50}</ProjectGuid>
    <OutputType>Library</OutputType>
    <AppDesignerFolder>Properties</AppDesignerFolder>
    <RootNamespace>Assembly</RootNamespace>
    <AssemblyName>Assembly</AssemblyName>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
    <FileAlignment>512</FileAlignment>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Debug|AnyCPU' "">
    <DebugSymbols>true</DebugSymbols>
    <DebugType>full</DebugType>
    <Optimize>false</Optimize>
    <OutputPath>bin\Debug\</OutputPath>
    <DocumentationFile>bin\Debug\Assembly.XML</DocumentationFile>
    <DefineConstants>DEBUG;TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
    <TargetFrameworkVersion>v4.5</TargetFrameworkVersion>
  </PropertyGroup>
  <PropertyGroup Condition="" '$(Configuration)|$(Platform)' == 'Release|AnyCPU' "">
    <DebugType>pdbonly</DebugType>
    <Optimize>true</Optimize>
    <OutputPath>bin\Release\</OutputPath>
    <DefineConstants>TRACE</DefineConstants>
    <ErrorReport>prompt</ErrorReport>
    <WarningLevel>4</WarningLevel>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include=""System"" />
    <Reference Include=""System.Core"" />
    <Reference Include=""System.Xml.Linq"" />
    <Reference Include=""System.Data.DataSetExtensions"" />
    <Reference Include=""Microsoft.CSharp"" />
    <Reference Include=""System.Data"" />
    <Reference Include=""System.Xml"" />
  </ItemGroup>
  <ItemGroup>
    <Compile Include=""Source.cs"" />
  </ItemGroup>
  <Import Project=""$(MSBuildToolsPath)\Microsoft.CSharp.targets"" />
</Project>";
        }

        private static string GetSourceFileContent()
        {
            return @"using System;

namespace Assembly
{
    /// <summary>Source</summary>
    public class Source
    {
        // Does nothing
    }
}";
        }
    }
}
