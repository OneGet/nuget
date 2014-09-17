// 
//  Copyright (c) Microsoft Corporation. All rights reserved. 
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//  

namespace NuGet.OneGet {
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.Remoting;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Channels.Ipc;
    using System.Security.Principal;
    using System.Xml.Linq;
    using System.Xml.XPath;
    using Microsoft.Win32;

    public abstract class ChocolateyRequest : BaseRequest {

        internal static ImplictLazy<string> ChocolateyModuleFolder = new ImplictLazy<string>( () => Path.GetFullPath(Assembly.GetExecutingAssembly().Location));
        internal static ImplictLazy<string> ChocolateyModuleFile = new ImplictLazy<string>(() => Path.Combine(ChocolateyModuleFolder, "Chocolatey.psd1"));
        internal static ImplictLazy<string> EtcPath = new ImplictLazy<string>(() => Path.Combine(ChocolateyModuleFolder, "etc"));
        internal static ImplictLazy<string> ChocolateyConfigPath = new ImplictLazy<string>(() => Path.Combine(RootInstallationPath, "chocolateyinstall", "Chocolatey.config"));
        internal static ImplictLazy<string> SystemDrive = new ImplictLazy<string>(() => {
                var drive = Environment.GetEnvironmentVariable("SystemDrive");

                if (string.IsNullOrEmpty(drive)) {
                    return "c:\\";
                }
                return drive + "\\";
            });

        internal static ImplictLazy<string> RootInstallationPath = new ImplictLazy<string>(() => {
            var rip = Environment.GetEnvironmentVariable("ChocolateyPath");
            if (string.IsNullOrEmpty(rip)) {
                // current default
                rip = Path.Combine(SystemDrive, @"\", "Chocolatey");

                // store it.
                Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.Process);
            }

            if (!rip.DirectoryHasDriveLetter()) {
                rip = rip.TrimStart('\\');
                rip = Path.Combine(SystemDrive, rip);
                Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.User);
                Environment.SetEnvironmentVariable("ChocolateyPath", rip, EnvironmentVariableTarget.Process);
            }
            if (!Directory.Exists(rip)) {
                Directory.CreateDirectory(rip);
            }
            return rip;
        });

        internal static ImplictLazy<string> HelperModuleText = new ImplictLazy<string>(() => {
                var asm = Assembly.GetExecutingAssembly();

                var resource = asm.GetManifestResourceNames().FirstOrDefault(each => each.EndsWith("ChocolateyHelpers.psm1", StringComparison.OrdinalIgnoreCase));
                if (resource != null) {
                    return asm.GetManifestResourceStream(resource).ReadToEnd();
                }
            return string.Empty;
        });
      
        internal bool ForceX86 {
            [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
            get {
                return GetOptionValue(OptionCategory.Install, "Destination").IsTrue();
            }
        }

        public static ImplictLazy<bool> IsElevated = new ImplictLazy<bool>(() => {
            var id = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(id);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        });


        internal override string Destination {
            get {
                return RootInstallationPath;
            }
        }

        internal override IDictionary<string, PackageSource> RegisteredPackageSources {
            get {
                try {
                    return Config.XPathSelectElements("/chocolatey/sources/source")
                        .Where(each => each.Attribute("id") != null && each.Attribute("value") != null)
                        .ToDictionaryNicely(each => each.Attribute("id").Value, each => new PackageSource {
                            Name = each.Attribute("id").Value,
                            Location = each.Attribute("value").Value,
                            Trusted = each.Attributes("trusted").Any() && each.Attribute("trusted").Value.IsTrue(),
                            IsRegistered = true,
                            IsValidated = each.Attributes("validated").Any() && each.Attribute("validated").Value.IsTrue(),
                        }, StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception e) {
                    e.Dump(this);
                }
                return new Dictionary<string, PackageSource>(StringComparer.OrdinalIgnoreCase) {
                    {
                        "chocolatey", new PackageSource {
                            Name = "chocolatey",
                            Location = "http://chocolatey.org/api/v2/",
                            Trusted = false,
                            IsRegistered = false,
                            IsValidated = true,
                        }
                    }
                };
            }
        }

        internal override void RemovePackageSource(string id) {
            var config = Config;
            var source = config.XPathSelectElements(string.Format("/chocolatey/sources/source[@id='{0}']",id)).FirstOrDefault();
            if (source != null) {
                source.Remove();
                Config = config;
            }
        }

        internal override void AddPackageSource(string name, string location, bool isTrusted, bool isValidated) {
            if (SkipValidate || ValidateSourceLocation(location)) {
                var config = Config;
                var sources = config.XPathSelectElements("/chocolatey/sources").FirstOrDefault();
                if (sources == null) {
                    config.Root.Add(sources = new XElement("sources"));
                }
                var source = new XElement("source");
                source.SetAttributeValue("id", name);
                source.SetAttributeValue("value", location);
                if (isValidated) {
                    source.SetAttributeValue("validated", true);
                }
                if (isTrusted) {
                    source.SetAttributeValue("trusted", true);
                }
                sources.Add(source);
                Config = config;

                YieldPackageSource(name, location, isTrusted, true, isValidated);
            }
        }


        public void GenerateBins(string pkgPath) {
            var exes = Directory.EnumerateFiles(pkgPath, "*.exe", SearchOption.AllDirectories);
            foreach (var exe in exes) {
                if (File.Exists((exe + ".ignore"))) {
                    continue;
                }
                if (File.Exists(exe + ".gui")) {
                    GenerateGuiBin(exe);
                    continue;
                }
                GenerateConsoleBin(exe);
            }
        }

        internal override void PostInstall(PackageItem packageItem) {
            // run the install script
            var pkgPath = packageItem.FullPath;
            var scripts = Directory.EnumerateFiles(pkgPath, "chocolateyInstall.ps1", SearchOption.AllDirectories);
            var script = scripts.FirstOrDefault();
            if (script != null) {
                // Error("Missing chocolateyinstall.ps1", pkgPath, null);
                // break;
                // Now for the script.
                // InvokeChocolateyScript(pkgPath, packageImte., script);
                InvokeChocolateyScript(script, pkgPath);
            }

            // Now handle 'bins'
            GenerateBins(pkgPath);


        }

        internal override void PostUninstall(PackageItem packageItem) {
            // run the uninstall script
        }

        internal override void PreInstall(PackageItem packageItem) {
            // run the install script
        }

        internal override void PreUninstall(PackageItem packageItem) {
            // run the uninstall script
        }

        internal XDocument Config {
            get {
                try {
                    var doc = XDocument.Load(ChocolateyConfigPath);
                    if (doc.Root != null && doc.Root.Name == "chocolatey") {
                        return doc;
                    }

                    // doc root isn't right. make a new one!
                }
                catch {
                    // bad doc 
                }
                return XDocument.Load(new MemoryStream(@"<?xml version=""1.0""?>
<chocolatey>
    <useNuGetForSources>false</useNuGetForSources>
    <sources>
        <source id=""chocolatey"" value=""http://chocolatey.org/api/v2/"" />
    </sources>
</chocolatey>
".ToByteArray()));
            }
            set {
                Verbose("Saving Chocolatey Config", ChocolateyConfigPath);

                if (value == null) {
                    return;
                }

                ProviderServices.CreateFolder(Path.GetDirectoryName(ChocolateyConfigPath), this);
                value.Save(ChocolateyConfigPath);
            }
        }
       

        
        internal void InvokeChocolateyScript(string path,string workingDirectory) {
            var pwd = Directory.GetCurrentDirectory();

            try {
                workingDirectory = string.IsNullOrEmpty(workingDirectory) ? pwd : workingDirectory;

                if (Directory.Exists(workingDirectory)) {
                    Directory.SetCurrentDirectory(workingDirectory);
                }
                if (File.Exists(path)) {
                    path = Path.GetFullPath(path);
                    ChocolateyScript.Invoke(this, path);    
                }
            } catch (Exception e) {
                e.Dump(this);
            }

            Directory.SetCurrentDirectory(pwd); 
        }

        internal static int InvokeChocolateyScriptForRPC(string remoteUri, string script) {
            // connect to the server process first
            var clientChannel = new IpcClientChannel();
            ChannelServices.RegisterChannel(clientChannel, true);
            var req = (ChocolateyRequest)Activator.GetObject(typeof(ChocolateyRequest), remoteUri);
            ChocolateyScript.Invoke(req,script.FromBase64());
            return 0;
        }

        /// <summary>
        /// This creates an RPC server that has endpoints for all the HostAPIs, then creates an elevated process that can call back into this process to report progress.
        /// 
        /// 
        /// </summary>
        /// <param name="script"></param>
        /// <returns></returns>
        internal int InvokeElevatedViaRPC(string script) {

            var guid = Guid.NewGuid();

            // set up the server IPC channel
            var serverChannel = new IpcServerChannel(guid.ToString());
            ChannelServices.RegisterChannel(serverChannel, true);
            // RemotingConfiguration.RegisterWellKnownServiceType( typeof(BaseRequest), "Request", WellKnownObjectMode.Singleton);
            var objRef = RemotingServices.Marshal(this);

            // Create the client elevated
            var process = AsyncProcess.Start(new ProcessStartInfo {
                FileName = NuGetExePath,
                Arguments = string.Format("-rpc {0} -script {1}", objRef.URI, script.ToBase64()),
                // WorkingDirectory = workingDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
            });

            process.WaitForExit();

            RemotingServices.Disconnect(this);
            return 0;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool GetChocolateyWebFile(string packageName, string fileFullPath, string url, string url64bit) {
            if (!string.IsNullOrEmpty(url64bit) && Environment.Is64BitOperatingSystem && !ForceX86) {
                url = url64bit;
            }

            Verbose("GetChocolateyWebFile", "{0} => {1}", packageName, url);

            var uri = new Uri(url);

            ProviderServices.DownloadFile(uri, fileFullPath, this);
            if (string.IsNullOrEmpty(fileFullPath)) {
                throw new Exception("Failed to download file {0}".format(url));
            }

            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyInstallPackage(string packageName, string fileType, string silentArgs, string file, int[] validExitCodes, string workingDirectory) {
            Verbose("InstallChocolateyInstallPackage", "{0}", packageName);

            switch (fileType.ToLowerInvariant()) {
                case "msi":
                    return StartChocolateyProcessAsAdmin("/i {0} {1}".format(file, silentArgs), "msiexec.exe", true, true, validExitCodes, workingDirectory);

                case "exe":
                    return StartChocolateyProcessAsAdmin("{0}".format(silentArgs), file, true, true, validExitCodes, workingDirectory);

                case "msu":
                    return StartChocolateyProcessAsAdmin("{0} {1}".format(file, silentArgs), "wusa.exe", true, true, validExitCodes, workingDirectory);
            }
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyPackage(string packageName, string fileType, string silentArgs, string url, string url64bit, int[] validExitCodes, string workingDirectory) {
            try {
                
                var tempFolder = Path.GetTempPath();;
                var chocTempDir = Path.Combine(tempFolder, "chocolatey");
                var pkgTempDir = Path.Combine(chocTempDir, packageName);
                ProviderServices.Delete(pkgTempDir, this);
                ProviderServices.CreateFolder(pkgTempDir, this);

                if (!string.IsNullOrEmpty(url64bit) && Environment.Is64BitOperatingSystem && !ForceX86) {
                    url = url64bit;
                }

                string localFile = null;


                // check to see if the url is a local file 
                if (Uri.IsWellFormedUriString(url, UriKind.RelativeOrAbsolute)) {
                    var uri = new Uri(url);
                    if (uri.IsFile) {
                        // file uri.
                        localFile = Path.GetFullPath(uri.AbsolutePath);
                        if (!File.Exists(localFile)) {
                            localFile = null;
                        }
                    }
                }

                if (string.IsNullOrEmpty(localFile)) {
                    localFile = Path.Combine(pkgTempDir, "{0}install.{1}".format(packageName, fileType));
                    if (!GetChocolateyWebFile(packageName, localFile, url, url64bit)) {
                        throw new Exception(string.Format( "Download failed {0} {1} {2}",url, url64bit, localFile));
                    }
                }

                if (InstallChocolateyInstallPackage(packageName, fileType, silentArgs, localFile, validExitCodes, workingDirectory)) {
                    Verbose("Package Successfully Installed", packageName);
                    return true;
                }
                throw new Exception("Failed Install.");
            }
            catch (Exception e) {
                e.Dump(this);
                Error(ErrorCategory.InvalidResult, packageName, Constants.DependentPackageFailedInstall, packageName);
                
                throw new Exception("Failed Installation");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public Snapshot SnapshotFolder(string locationToMonitor) {
            return new Snapshot(this, locationToMonitor);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyPath(string pathToInstall, string context) {
            if (context.Equals("machine", StringComparison.InvariantCultureIgnoreCase)) {
                if (IsElevated) {
                    EnvironmentUtility.SystemPath = EnvironmentUtility.SystemPath.Append(pathToInstall).RemoveMissingFolders();
                    EnvironmentUtility.Path = EnvironmentUtility.Path.Append(pathToInstall).RemoveMissingFolders();
                    return true;
                }
                Verbose("Elevation Required--May not modify system path without elevation");
                return false;
            }
            EnvironmentUtility.UserPath = EnvironmentUtility.UserPath.Append(pathToInstall).RemoveMissingFolders();
            EnvironmentUtility.Path = EnvironmentUtility.Path.Append(pathToInstall).RemoveMissingFolders();
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public void UpdateSessionEnvironment() {
            Verbose("Reloading Environment", "");
            EnvironmentUtility.Rehash();
        }

        internal string PackageInstallationPath {
            get {
                var path = Path.Combine(RootInstallationPath, "lib");
                if (!Directory.Exists(path)) {
                    ProviderServices.CreateFolder(path, this);
                }
                return path;
            }
        }


        internal string PackageExePath {
            get {
                var path = Path.Combine(RootInstallationPath, "bin");
                if (!Directory.Exists(path)) {
                    ProviderServices.CreateFolder(path, this);
                }
                return Path.Combine(RootInstallationPath, "bin");
            }
        }


        public string GetBatFileLocation(string exe, string name) {
            if (string.IsNullOrEmpty(name)) {
                return Path.Combine(PackageExePath, Path.GetFileNameWithoutExtension(exe) + ".bat");
            }
            else {
                return Path.Combine(PackageExePath, Path.GetFileNameWithoutExtension(name) + ".bat");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Can be called from powershell")]
        public void GeneratePS1ScriptBin(string ps1, string name = null) {
            File.WriteAllText(GetBatFileLocation(ps1, name), @"@echo off
powershell -NoProfile -ExecutionPolicy unrestricted -Command ""& '{0}'  %*""".format(ps1));
        }

        public void GenerateConsoleBin(string exe, string name = null) {
            File.WriteAllText(GetBatFileLocation(exe, name), @"@echo off
SET DIR=%~dp0%
cmd /c ""%DIR%{0} %*""
exit /b %ERRORLEVEL%".format(PackageExePath.RelativePathTo(exe)));
        }

        public void GenerateGuiBin(string exe, string name = null) {
            File.WriteAllText(GetBatFileLocation(exe, name), @"@echo off
SET DIR=%~dp0%
start """" ""%DIR%{0}"" %*".format(PackageExePath.RelativePathTo(exe)));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public void RemoveBins(string pkgPath) {
            var exes = Directory.EnumerateFiles(pkgPath, "*.exe", SearchOption.AllDirectories);
            foreach (var exe in exes) {
                if (File.Exists(exe + ".ignore")) {
                    continue;
                }
                if (File.Exists(exe + ".gui")) {
                    RemoveGuiBin(exe);
                    continue;
                }
                RemoveConsoleBin(exe);
            }
        }

        protected override string ConfigurationFileLocation {
            get {
                return ChocolateyConfigPath;
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public void RemoveConsoleBin(string exe, string name = null) {
            ProviderServices.Delete(GetBatFileLocation(exe, name),RemoteThis);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public void RemoveGuiBin(string exe, string name = null) {
            ProviderServices.Delete(GetBatFileLocation(exe, name),RemoteThis);
        }


        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyPowershellCommand(string packageName, string psFileFullPath, string url, string url64bit, string workingDirectory) {
            if (GetChocolateyWebFile(packageName, psFileFullPath, url, url64bit)) {
                if (File.Exists(psFileFullPath)) {
                    GeneratePS1ScriptBin(psFileFullPath);
                    return true;
                }
            }

            Verbose("Unable to download script {0}", url);
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyVsixPackage(string packageName, string vsixUrl, string vsVersion) {
            Verbose("InstallChocolateyVsixPackage", packageName);
            var vs = Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Microsoft\VisualStudio");
            var versions = vs.GetSubKeyNames().Select(each => {
                float f;
                if (!float.TryParse(each, out f)) {
                    return null;
                }
                if (f < 10.0) {
                    return null;
                }

                var vsv = vs.OpenSubKey(each);
                if (vsv.GetValueNames().Contains("InstallDir", StringComparer.OrdinalIgnoreCase)) {
                    return new {
                        Version = f,
                        InstallDir = vsv.GetValue("InstallDir").ToString()
                    };
                }
                return null;
            }).Where( each => each != null ).OrderByDescending(each => each.Version);

            var reqVsVersion = versions.FirstOrDefault();

            if (!string.IsNullOrEmpty(vsVersion)) {
                float f;
                if (!float.TryParse(vsVersion, out f)) {
                    throw new Exception("Unable to parse version number");
                }

                reqVsVersion = versions.FirstOrDefault(each => each.Version == f);
            }

            if (reqVsVersion == null) {
                throw new Exception("Required Visual Studio Version is not installed");
            }

            var vsixInstller = Path.Combine(reqVsVersion.InstallDir, "VsixInstaller.exe");
            if (!File.Exists(vsixInstller)) {
                throw new Exception("Can't find Visual Studio VSixInstaller.exe {0}".format(vsixInstller));
            }
            var file = Path.Combine(Path.GetTempPath(), packageName.MakeSafeFileName());

            ProviderServices.DownloadFile(new Uri(vsixUrl), file, this);

            if (string.IsNullOrEmpty(file) || !File.Exists(file)) {
                throw new Exception("Unable to download file {0}".format(vsixUrl));
            }
            var process = AsyncProcess.Start(new ProcessStartInfo {
                FileName = vsixInstller,
                Arguments = @"/q ""{0}""".format(file),
            });
            process.WaitForExit();
            if (process.ExitCode > 0 && process.ExitCode != 1001) {
                Verbose("VsixInstall Failure {0}", file);
                throw new Exception("Install failure");
            }
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyExplorerMenuItem(string menuKey, string menuLabel, string command, string type) {
            Verbose("InstallChocolateyExplorerMenuItem", "{0}/{1}/{2}/{3}", menuKey, menuLabel, command, type);

            var key = type == "file" ? "*" : (type == "directory" ? "directory" : null);
            if (key == null) {
                return false;
            }
            if (!IsElevated) {
                return StartChocolateyProcessAsAdmin("Install-ChocolateyExplorerMenuItem '{0}' '{1}' '{2}' '{3}'".format(menuKey, menuLabel, command, type), "powershell", false, false, new[] {
                    0
                }, Environment.CurrentDirectory);
            }

            var k = Registry.ClassesRoot.CreateSubKey(@"{0}\shell\{1}".format(key, menuKey));
            k.SetValue(null, menuLabel);
            var c = k.CreateSubKey("command");
            c.SetValue(null, @"{0} ""%1""");
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool UninstallChocolateyPackage(string packageName, string fileType, string silentArgs, string file, int[] validExitCodes, string workingDirectory) {
            Verbose("UninstallChocolateyPackage", packageName);

            switch (fileType.ToLowerInvariant()) {
                case "msi":
                    return StartChocolateyProcessAsAdmin("/x {0} {1}".format(file, silentArgs), "msiexec.exe", true, true, validExitCodes, workingDirectory);

                case "exe":
                    return StartChocolateyProcessAsAdmin("{0}".format(silentArgs), file, true, true, validExitCodes, workingDirectory);

                default:
                    Verbose("Unsupported Uninstall Type {0}", fileType);
                    break;
            }
            return false;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public string GetChocolateyUnzip(string fileFullPath, string destination, string specificFolder, string packageName) {
            Verbose("GetChocolateyUnzip", fileFullPath);
            try {
                var zipfileFullPath = fileFullPath;

                if (!string.IsNullOrEmpty(specificFolder)) {
                    fileFullPath = Path.Combine(fileFullPath, specificFolder);
                }

                if (!string.IsNullOrEmpty(packageName)) {
                    var packageLibPath = Environment.GetEnvironmentVariable("ChocolateyPackageFolder");
                    ProviderServices.CreateFolder(packageLibPath, this);
                    var zipFileName = Path.GetFileName(zipfileFullPath);
                    var zipExtractLogFullPath = Path.Combine(packageLibPath, "{0}.txt".format(zipFileName));
                    var snapshot = new Snapshot(this, destination);

                    UnZip(fileFullPath, destination);

                    snapshot.WriteFileDiffLog(zipExtractLogFullPath);
                }
                else {
                    UnZip(fileFullPath, destination);
                }
                return destination;
            }
            catch (Exception e) {
                e.Dump(this);
                Verbose("PackageInstallation Failed {0}", packageName);
                throw new Exception("Failed Installation");
            }
        }

        private void UnZip(string zipFile, string destination) {
            var zip = ZipArchive.OpenOnFile(zipFile, FileMode.Open, FileAccess.Read, FileShare.Read, true);
            foreach (var file in zip.Files) {

                var fName = Path.Combine(destination, file.Name);
                if (File.Exists(fName) || Directory.Exists(fName)) {
                    ProviderServices.DeleteFile(fName, RemoteThis);
                }

                if (file.FolderFlag) {
                    ProviderServices.CreateFolder(fName, RemoteThis);
                    continue;
                }

                if (file.VolumeLabelFlag) {
                    continue;
                }

                using (var output = File.Create(fName)) {
                    file.GetStream(FileMode.Open, FileAccess.Read).CopyTo(output);
                }
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyZipPackage(string packageName, string url, string unzipLocation, string url64bit, string specificFolder, string workingDirectory) {
            Verbose("InstallChocolateyZipPackage", packageName);
            try {
                var tempFolder = Path.GetTempPath();;
                var chocTempDir = Path.Combine(tempFolder, "chocolatey");
                var pkgTempDir = Path.Combine(chocTempDir, packageName);
                ProviderServices.Delete(pkgTempDir, this);
                ProviderServices.CreateFolder(pkgTempDir, this);

                var file = Path.Combine(pkgTempDir, "{0}install.{1}".format(packageName, "zip"));
                if (GetChocolateyWebFile(packageName, file, url, url64bit)) {
                    if (GetChocolateyUnzip(file, unzipLocation, specificFolder, packageName).Is()) {
                        Verbose("Package Successfully Installed", packageName);
                        return true;
                    }
                    throw new Exception("Failed Install.");
                }
                throw new Exception("Failed Download.");
            }
            catch (Exception e) {
                e.Dump(this);
                Verbose("PackageInstallation Failed {0}", packageName);
                throw new Exception("Failed Installation");
            }
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool UnInstallChocolateyZipPackage(string packageName, string zipFileName) {
            Verbose("UnInstallChocolateyZipPackage", "Package '{0}', ZipFile '{1}'", packageName, zipFileName);
            try {
                var packageLibPath = Environment.GetEnvironmentVariable("ChocolateyPackageFolder");
                var zipContentFile = Path.Combine(packageLibPath, "{0}.txt".format(Path.GetFileName(zipFileName)));
                if (File.Exists(zipContentFile)) {
                    foreach (var file in File.ReadAllLines(zipContentFile).Where(each => !string.IsNullOrEmpty(each) && File.Exists(each))) {
                        ProviderServices.DeleteFile(file,RemoteThis);
                    }
                }
            }
            catch (Exception e) {
                e.Dump(this);
                Verbose("uninstall failure {0}", packageName);
            }
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyFileAssociation(string extension, string executable) {
            Verbose("InstallChocolateyFileAssociation", "{0} with executable {1}", extension, executable);
            if (string.IsNullOrEmpty(executable)) {
                throw new ArgumentNullException("executable");
            }

            if (string.IsNullOrEmpty(extension)) {
                throw new ArgumentNullException("extension");
            }
            executable = Path.GetFullPath(executable);
            if (!File.Exists(executable)) {
                throw new FileNotFoundException("Executable not found", executable);
            }

            extension = "." + extension.Trim().Trim('.');
            var fileType = Path.GetFileName(executable).Replace(' ', '_');

            return StartChocolateyProcessAsAdmin(@"/c assoc {0}={1} & ftype {1}={2} ""%1"" %*".format(extension, fileType, executable), "cmd.exe", false, false, new[] {
                0
            }, Environment.CurrentDirectory);
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool InstallChocolateyPinnedTaskBarItem(string targetFilePath) {
            Verbose("InstallChocolateyPinnedTaskBarItem", targetFilePath);
            if (string.IsNullOrEmpty(targetFilePath)) {
                Verbose("Failed InstallChocolateyPinnedTaskBarItem -- Empty path");
                throw new Exception("Failed.");
            }

            ProviderServices.AddPinnedItemToTaskbar(Path.GetFullPath(targetFilePath),RemoteThis);
            return true;
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        public bool StartChocolateyProcessAsAdmin(string statements, string exeToRun, bool minimized, bool noSleep, int[] validExitCodes, string workingDirectory) {
            Verbose("StartChocolateyProcessAsAdmin", "Exe '{0}', Args '{1}'", exeToRun, statements);

            if (exeToRun.EqualsIgnoreCase("powershell")) {
                // run as a powershell script
                if (IsElevated) {
                    Verbose("Already Elevated", "Running PowerShell script in process");
                    // in proc, we're already good.
                    ChocolateyScript.Invoke(this, statements);
                    return true;
                }

                Verbose("Not Elevated", "Running PowerShell script in new process");
                // otherwise setup a new proc
                InvokeElevatedViaRPC(statements);
            }

            // just a straight exec from here.
            try {
                Verbose("Launching Process", "EXE :'{0}'", exeToRun);
                var process = AsyncProcess.Start(new ProcessStartInfo {
                    FileName = exeToRun,
                    Arguments = statements,
                    WorkingDirectory = workingDirectory,
                    WindowStyle = minimized ? ProcessWindowStyle.Hidden : ProcessWindowStyle.Normal,
                    Verb = IsElevated ? "" : "runas",
                });

                while (!process.WaitForExit(1)) {
                    if (IsCancelled()) {
                        process.Kill();
                        Verbose("Process Killed", "Host requested cancellation");
                        throw new Exception("Killed Process {0}".format(exeToRun));
                    }
                }

                if (validExitCodes.Contains(process.ExitCode)) {
                    Verbose("Process Exited Successfully.", "{0}", exeToRun);
                    return true;
                }
                Verbose("Process Failed {0}", exeToRun);
                throw new Exception("Process Exited with non-successful exit code {0} : {1} ".format(exeToRun, process.ExitCode));
            }
            catch (Exception e) {
                e.Dump(this);

                Error("Process Execution Failed", "'{0}' -- {1}", exeToRun, e.Message);
                throw e;
            }
        }
    }

}