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

namespace NuGet.OneGet{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using global::NuGet;
    using RequestImpl = System.Object;

    public class NuGetProvider {
        private static readonly string[] _empty = new string[0];

        private static readonly Dictionary<string,string[]> _features = new Dictionary<string, string[]> {
            { "supports-powershell-modules", _empty },
            { "schemes", new [] {"http", "https", "file"} },
            { "extensions", new [] {"nupkg"} },
            { "magic-signatures", _empty },
        };

        internal static IEnumerable<string> SupportedSchemes {
            get {
                return _features["schemes"];
            }
        }
        /// <summary>
        ///     Returns the name of the Provider. 
        /// </summary>
        /// <required />
        /// <returns>the name of the package provider</returns>
        public string GetPackageProviderName() {
            return Constants.ProviderName;
        }

        public void InitializeProvider(object dynamicInterface, RequestImpl requestImpl) {
            RequestExtensions.RemoteDynamicInterface = dynamicInterface;
            _features.AddOrSet("exe", new[] {
                Assembly.GetAssembly(typeof(global::NuGet.PackageSource)).Location
            });
        }

        public void GetFeatures(RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::GetFeatures'");
                foreach (var feature in _features) {
                    request.Yield(feature);
                }
            }
        }

        /// <summary>
        /// DEPRECATED -- for supporting the AUG 2014 OneGetPreview
        /// </summary>
        /// <param name="category"></param>
        /// <param name="requestImpl"></param>
        public void GetDynamicOptions(int category, RequestImpl requestImpl) {
            try {
                GetDynamicOptions(((OptionCategory)category).ToString(), requestImpl);
            } catch {
                // meh. If it doesn't fit, move on.
            }
        }

        public void GetDynamicOptions(string category, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                try {
                    request.Debug("Calling '{0}::GetDynamicOptions' '{1}'", "NuGet", category);

                    OptionCategory cat;
                    if (!Enum.TryParse(category ?? "", true, out cat)) {
                        // unknown category
                        return;
                    }

                    switch (cat) {
                        case OptionCategory.Package:
                            request.YieldDynamicOption(cat, "Tag", OptionType.StringArray, false);
                            request.YieldDynamicOption(cat, "Contains", OptionType.String, false);
                            request.YieldDynamicOption(cat, "AllowPrereleaseVersions", OptionType.Switch, false);
                            break;

                        case OptionCategory.Source:
                            request.YieldDynamicOption(cat, "ConfigFile", OptionType.String, false);
                            request.YieldDynamicOption(cat, "SkipValidate", OptionType.Switch, false);
                            break;

                        case OptionCategory.Install:
                            request.YieldDynamicOption(cat, "Destination", OptionType.Path, true);
                            request.YieldDynamicOption(cat, "SkipDependencies", OptionType.Switch, false);
                            request.YieldDynamicOption(cat, "ContinueOnFailure", OptionType.Switch, false);
                            request.YieldDynamicOption(cat, "ExcludeVersion", OptionType.Switch, false);
                            request.YieldDynamicOption(cat, "PackageSaveMode", OptionType.String, false,new [] {"nuspec", "nupkg", "nuspec;nupkg"} );
                            break;
                    }
                } catch {
                    // this makes it ignore new OptionCategories that it doesn't know about.
                }
            }
        }


        // --- Manages package sources ---------------------------------------------------------------------------------------------------
        public void AddPackageSource(string name, string location, bool trusted, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::AddPackageSource'");
                var src = request.FindRegisteredSource(name);
                if (src != null) {
                    request.RemovePackageSource(src.Name);
                }

                if (!request.SkipValidate) {

                    if (request.ValidateSourceLocation(location)) {
                        request.AddPackageSource(name, location, trusted, true);
                        return;
                    }
                    // not valid
                    request.Error(ErrorCategory.InvalidData, location, Constants.SourceLocationNotValid, location);
                }

                request.AddPackageSource(name, location, trusted,false);
            }
        }

        public void ResolvePackageSources(RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::ResolvePackageSources'");
                foreach (var source in request.SelectedSources) {
                    request.YieldPackageSource(source.Name, source.Location, source.Trusted, source.IsRegistered, source.IsValidated);
                }
            }
        }

        public void RemovePackageSource(string name, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::RemovePackageSource'");
                var src = request.FindRegisteredSource(name);
                if (src == null) {
                    request.Warning(Constants.UnableToResolveSource, name);
                    return;
                }

                request.RemovePackageSource(src.Name);
                request.YieldPackageSource(src.Name, src.Location, src.Trusted, false, src.IsValidated);
            }
        }

        // --- Finds packages ---------------------------------------------------------------------------------------------------
        /// <summary>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="requiredVersion"></param>
        /// <param name="minimumVersion"></param>
        /// <param name="maximumVersion"></param>
        /// <param name="id"></param>
        /// <param name="requestImpl"></param>
        /// <returns></returns>
        public void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, int id, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::FindPackage'");

                // get the package by ID first.
                // if there are any packages, yield and return
                if (request.YieldPackages(request.GetPackageById(name, requiredVersion, minimumVersion, maximumVersion), name)) {
                    return;
                }

                // have we been cancelled?
                if (request.IsCancelled()) {
                    return;
                }

                // Try searching for matches and returning those.
                request.YieldPackages(request.SearchForPackages(name, requiredVersion, minimumVersion, maximumVersion), name);
            }
        }

        public void FindPackageByFile(string filePath, int id, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::FindPackageByFile'");
                var pkgItem = request.GetPackageByFilePath(Path.GetFullPath(filePath));
                if (pkgItem != null) {
                    request.YieldPackage(pkgItem, filePath);
                }
            }
        }

        /* NOT SUPPORTED BY NUGET -- AT THIS TIME 
        public void FindPackageByUri(Uri uri, int id, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::FindPackageByUri'");

                // check if this URI is a valid source
                // if it is, get the list of packages from this source

                // otherwise, download the Uri and see if it's a package 
                // that we support.
            }
        }
         */

        public void GetInstalledPackages(string name, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::GetInstalledPackages'");

                // look in the destination directory for directories that contain nupkg files.
                var subdirs = Directory.EnumerateDirectories(request.Destination);
                foreach (var subdir in subdirs) {
                    var nupkgs = Directory.EnumerateFileSystemEntries(subdir, "*.nupkg", SearchOption.TopDirectoryOnly);

                    foreach (var pkgFile in nupkgs) {
                        var pkgItem = request.GetPackageByFilePath(pkgFile);

                        if (pkgItem != null && pkgItem.IsInstalled) {

                            if (pkgItem.Id.Equals(name, StringComparison.CurrentCultureIgnoreCase)) {
                                request.YieldPackage(pkgItem, name);
                                break;
                            }
                            if (string.IsNullOrEmpty(name) || pkgItem.Id.IndexOf(name, StringComparison.CurrentCultureIgnoreCase) > -1) {
                                if (!request.YieldPackage(pkgItem, name)) {
                                    return;
                                }
                            }
                        }
                    }
                }
                
            }
        }

        // --- operations on a package ---------------------------------------------------------------------------------------------------
        public void DownloadPackage(string fastPath, string location, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::DownloadPackage'");

                var pkgRef = request.GetPackageByFastpath(fastPath);
                if (pkgRef == null) {
                    request.Error(ErrorCategory.InvalidArgument, fastPath, Constants.UnableToResolvePackageReference);
                    return;
                }

                // cheap and easy copy to location.
                using (var input = pkgRef.Package.GetStream()) {
                    using (var output = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                        input.CopyTo(output);
                    }
                }
            }
        }

        public void GetPackageDependencies(string fastPath, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::GetPackageDependencies'");

                var pkgRef = request.GetPackageByFastpath(fastPath);
                if (pkgRef == null) {
                    request.Error(ErrorCategory.InvalidArgument, fastPath, Constants.UnableToResolvePackageReference);
                    return;
                }

                foreach (var depSet in pkgRef.Package.DependencySets) {
                    foreach (var dep in depSet.Dependencies) {
                        var depRefs = dep.VersionSpec == null ? request.GetPackageById(dep.Id).ToArray() : request.GetPackageByIdAndVersionSpec(dep.Id, dep.VersionSpec, true).ToArray();
                        if (depRefs.Length == 0) {
                            request.ThrowError(ErrorCategory.InvalidResult, pkgRef.GetCanonicalId(request),  Constants.DependencyResolutionError , request.GetCanonicalPackageId(Constants.ProviderName, dep.Id, ((object)dep.VersionSpec ?? "").ToString()));
                        }
                        foreach (var dependencyReference in depRefs) {
                            request.YieldPackage(dependencyReference, pkgRef.Id);
                        }
                    }
                }
            }
        }

        public void GetPackageDetails(string fastPath, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::GetPackageDetails'");
            }
        }

        public void InstallPackage(string fastPath, RequestImpl requestImpl) {
            // ensure that mandatory parameters are present.
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::InstallPackage'");

                var pkgRef = request.GetPackageByFastpath(fastPath);
                if (pkgRef == null) {
                    request.Error(ErrorCategory.InvalidArgument, fastPath, Constants.UnableToResolvePackageReference);
                    return;
                }

                var dependencies = request.GetUninstalledPackageDependencies(pkgRef).Reverse().ToArray();

                foreach (var d in dependencies) {
                    if (!request.InstallSinglePackage(d)) {
                        request.Error(ErrorCategory.InvalidResult, pkgRef.GetCanonicalId(request) , Constants.DependentPackageFailedInstall, d.GetCanonicalId(request));
                        return;
                    }
                }

                // got this far, let's install the package we came here for.
                if (!request.InstallSinglePackage(pkgRef)) {
                    // package itself didn't install.
                    // roll that back out everything we did install.
                    // and get out of here.
                    request.Error(ErrorCategory.InvalidResult, pkgRef.GetCanonicalId(request), Constants.PackageFailedInstall, pkgRef.GetCanonicalId(request), Constants.ReasonUnknown);
                    
                }
            }
        }

        // call-back for each package installed when installing dependencies?

        public void UninstallPackage(string fastPath, RequestImpl requestImpl) {
            using (var request =requestImpl.As<Request>()) {
                request.Debug("Calling 'NuGet::UninstallPackage'");
                var pkg = request.GetPackageByFastpath(fastPath);
                var dir = pkg.InstalledDirectory;

                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) ) {
                    request.DeleteFolder(pkg.InstalledDirectory,request.RemoteThis);
                    request.YieldPackage(pkg, pkg.Id);
                }
            }
        }
    }
}