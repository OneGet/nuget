﻿using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using Xunit;
using Xunit.Extensions;

namespace NuGet.Test.Integration.NuGetCommandLine
{
    public class NuGetHelpCommandTest
    {
        [Theory]
        [InlineData("config")]
        [InlineData("delete")]
        [InlineData("install")]
        [InlineData("list")]
        [InlineData("pack")]
        [InlineData("push")]
        [InlineData("restore")]
        [InlineData("setApiKey")]
        [InlineData("sources")]
        [InlineData("spec")]
        [InlineData("update")]
        public void HelpCommand_HelpMessage(string command)
        {
            // Arrange
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");

            // Act 
            var r = CommandRunner.Run(
                nugetexe,
                Directory.GetCurrentDirectory(),
                "help " + command,
                waitForExit: true);            

            // Assert
            Assert.Equal(0, r.Item1);            
        }

        // Tests that -ConfigFile is not included in the help message
        [Fact]
        public void HelpCommand_SpecCommand()
        {
            // Arrange
            var targetDir = ConfigurationManager.AppSettings["TargetDir"] ?? Environment.CurrentDirectory;
            var nugetexe = Path.Combine(targetDir, "nuget.exe");

            // Act 
            var r = CommandRunner.Run(
                nugetexe,
                Directory.GetCurrentDirectory(),
                "help spec",
                waitForExit: true);

            // Assert
            Assert.Equal(0, r.Item1);
            Assert.DoesNotContain("-ConfigFile", r.Item2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
