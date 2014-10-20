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
    using System.IO;
    using System.Linq;
    using IRequestObject = System.Object;
    using global::OneGet.ProviderSDK;

    public abstract class CommonProvider<T> where T : BaseRequest {
        protected static readonly string[] _empty = new string[0];
        protected static Dictionary<string, string[]> _features;

        internal static IEnumerable<string> SupportedSchemes {
            get {
                return _features[Constants.Features.SupportedSchemes];
            }
        }

        internal abstract string ProviderName {get;}


        /// <summary>
        /// Returns the name of the Provider. 
        /// </summary>
        /// <returns>The name of this proivder (uses the constant declared at the top of the class)</returns>
        public string GetPackageProviderName() {
            return ProviderName;
        }

        /// <summary>
        /// NOT IMPLEMENTED YET
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        /// <returns></returns>
        public void GetPackageDetails(string fastPackageReference, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::GetPackageDetails' '{1}'", ProviderName, fastPackageReference);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }

                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::GetPackageDetails' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Returns a collection of strings to the client advertizing features this provider supports.
        /// </summary>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void GetFeatures(IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::GetFeatures' ", ProviderName);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(false)) {
                        return;
                    }

                    foreach (var feature in _features) {
                        request.Yield(feature);
                    }
                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::GetFeatures' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }

        }

        /// <summary>
        /// Returns dynamic option definitions to the HOST
        /// </summary>
        /// <param name="category">The category of dynamic options that the HOST is interested in</param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public abstract void GetDynamicOptions(string category, IRequestObject requestObject);


        /// <summary>
        /// This is called when the user is adding (or updating) a package source
        /// 
        /// If this PROVIDER doesn't support user-defined package sources, remove this method.
        /// </summary>
        /// <param name="name">The name of the package source. If this parameter is null or empty the PROVIDER should use the location as the name (if the PROVIDER actually stores names of package sources)</param>
        /// <param name="location">The location (ie, directory, URL, etc) of the package source. If this is null or empty, the PROVIDER should use the name as the location (if valid)</param>
        /// <param name="trusted">A boolean indicating that the user trusts this package source. Packages returned from this source should be marked as 'trusted'</param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void AddPackageSource(string name, string location, bool trusted, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::AddPackageSource' '{1}','{2}','{3}'", ProviderName, name, location, trusted);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(false)) {
                        return;
                    }

                    // if they didn't pass in a name, use the location as a name. (if you support that kind of thing)
                    name = string.IsNullOrEmpty(name) ? location : name;

                    // let's make sure that they've given us everything we need.
                    if (string.IsNullOrEmpty(name)) {
                        request.Error(ErrorCategory.InvalidArgument, Constants.Parameters.Name, Constants.Messages.MissingRequiredParameter, Constants.Parameters.Name);
                        // we're done here.
                        return;
                    }

                    if (string.IsNullOrEmpty(location)) {
                        request.Error(ErrorCategory.InvalidArgument, Constants.Parameters.Location, Constants.Messages.MissingRequiredParameter, Constants.Parameters.Location);
                        // we're done here.
                        return;
                    }

                    // if this is supposed to be an update, there will be a dynamic parameter set for IsUpdatePackageSource
                    var isUpdate = request.GetOptionValue(Constants.Parameters.IsUpdate).IsTrue();

                    // if your source supports credentials you get get them too:
                    // string username =request.Username; 
                    // SecureString password = request.Password;
                    // feel free to send back an error here if your provider requires credentials for package sources.


                    // check first that we're not clobbering an existing source, unless this is an update

                    var src = request.FindRegisteredSource(name);

                    if (src != null && !isUpdate) {
                        // tell the user that there's one here already
                        request.Error(ErrorCategory.InvalidArgument, name ?? location, Constants.Messages.PackageProviderExists, name ?? location);
                        // we're done here.
                        return;
                    }

                    // conversely, if it didn't find one, and it is an update, that's bad too:
                    if (src == null && isUpdate) {
                        // you can't find that package source? Tell that to the user
                        request.Error(ErrorCategory.ObjectNotFound, name ?? location, Constants.Messages.UnableToResolveSource, name ?? location);
                        // we're done here.
                        return;
                    }

                    // ok, we know that we're ok to save this source
                    // next we check if the location is valid (if we support that kind of thing)

                    var validated = false;

                    if (!request.SkipValidate) {
                        // the user has not opted to skip validating the package source location, so check that it's valid (talk to the url, or check if it's a valid directory, etc)
                        // todo: insert code to check if the source is valid

                        validated = request.ValidateSourceLocation(location);

                        if (!validated) {
                            request.Error(ErrorCategory.InvalidData, name ?? location, Constants.Messages.SourceLocationNotValid, location);
                            // we're done here.
                            return;
                        }

                        // we passed validation!
                    }

                    // it's good to check just before you actaully write something to see if the user has cancelled the operation
                    if (request.IsCanceled) {
                        return;
                    }

                    // looking good -- store the package source
                    // todo: create the package source (and store it whereever you store it)

                    request.Verbose("Storing package source {0}", name);
                    
                    // actually yielded by the implementation.
                    //request.AddPackageSource(name, location, trusted, validated);

                    // and, before you go, Yield the package source back to the caller.

                    if (!request.YieldPackageSource(name, location, trusted, true /*since we just registered it*/, validated)) {
                        // always check the return value of a yield, since if it returns false, you don't keep returning data
                        // this can happen if they have cancelled the operation.
                        return;
                    }
                    // all done!

                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in {0} PackageProvider -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Removes/Unregisters a package source
        /// </summary>
        /// <param name="name">The name or location of a package source to remove.</param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void RemovePackageSource(string name, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::RemovePackageSource' '{1}'", ProviderName, name);

                    var src = request.FindRegisteredSource(name);
                    if (src == null) {
                        request.Warning(Constants.Messages.UnableToResolveSource, name);
                        return;
                    }

                    request.RemovePackageSource(src.Name);
                    request.YieldPackageSource(src.Name, src.Location, src.Trusted, false, src.IsValidated);
                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::RemovePackageSource' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void ResolvePackageSources(IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::ResolvePackageSources'", ProviderName);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(false)) {
                        return;
                    }

                    foreach (var source in request.SelectedSources) {
                        request.YieldPackageSource(source.Name, source.Location, source.Trusted, source.IsRegistered, source.IsValidated);
                    }
                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::ResolvePackageSources' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
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
        /// <param name="requestObject"></param>
        /// <returns></returns>
        public void FindPackage(string name, string requiredVersion, string minimumVersion, string maximumVersion, int id, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::FindPackage' '{1}','{2}','{3}','{4}'", ProviderName, requiredVersion, minimumVersion, maximumVersion, id);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }

                    // get the package by ID first.
                    // if there are any packages, yield and return
                    if (request.YieldPackages(request.GetPackageById(name, requiredVersion, minimumVersion, maximumVersion), name)) {
                        return;
                    }

                    // have we been cancelled?
                    if (request.IsCanceled) {
                        return;
                    }

                    // Try searching for matches and returning those.
                    request.YieldPackages(request.SearchForPackages(name, requiredVersion, minimumVersion, maximumVersion), name);

                    request.Debug("Finished '{0}::FindPackage' '{1}','{2}','{3}','{4}'", ProviderName, requiredVersion, minimumVersion, maximumVersion, id);
                }
            } catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::FindPackage' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void InstallPackage(string fastPackageReference, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::InstallPackage' '{1}'", ProviderName, fastPackageReference);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }

                    var pkgRef = request.GetPackageByFastpath(fastPackageReference);
                    if (pkgRef == null) {
                        request.Error(ErrorCategory.InvalidArgument, fastPackageReference, Constants.Messages.UnableToResolvePackage);
                        return;
                    }

                    var dependencies = request.GetUninstalledPackageDependencies(pkgRef).Reverse().ToArray();
                    int progressId = 0;

                    if (dependencies.Length > 0) {
                        progressId = request.StartProgress(0, "Installing package '{0}'", pkgRef.GetCanonicalId(request));
                    }
                    var n = 0;
                    foreach (var d in dependencies) {
                        request.Progress(progressId, (n*100/(dependencies.Length+1)) + 1, "Installing dependent package '{0}'", d.GetCanonicalId(request));
                        if (!request.InstallSinglePackage(d)) {
                            request.Error(ErrorCategory.InvalidResult, pkgRef.GetCanonicalId(request), Constants.Messages.DependentPackageFailedInstall, d.GetCanonicalId(request));
                            return;
                        }
                        n++;
                        request.Progress(progressId, (n * 100 / (dependencies.Length + 1)) , "Installed dependent package '{0}'", d.GetCanonicalId(request));
                    }

                    // got this far, let's install the package we came here for.
                    if (!request.InstallSinglePackage(pkgRef)) {
                        // package itself didn't install.
                        // roll that back out everything we did install.
                        // and get out of here.
                        request.Error(ErrorCategory.InvalidResult, pkgRef.GetCanonicalId(request), Constants.Messages.PackageFailedInstall, pkgRef.GetCanonicalId(request));
                        request.CompleteProgress(progressId, false);
                    }
                    request.CompleteProgress(progressId, true);

                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::InstallPackage' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }


        /// <summary>
        /// Uninstalls a package 
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void UninstallPackage(string fastPackageReference, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::UninstallPackage' '{1}'", ProviderName, fastPackageReference);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }
                    var pkg = request.GetPackageByFastpath(fastPackageReference);
                    request.UninstallPackage(pkg);
                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::UninstallPackage' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }



        /// <summary>
        /// Downloads a remote package file to a local location.
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="location"></param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        /// <returns></returns>
        public void DownloadPackage(string fastPackageReference, string location, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::DownloadPackage' '{1}','{2}'", ProviderName, fastPackageReference, location);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }

                    var pkgRef = request.GetPackageByFastpath(fastPackageReference);
                    if (pkgRef == null) {
                        request.Error(ErrorCategory.InvalidArgument, fastPackageReference, Constants.Messages.UnableToResolvePackage);
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
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::DownloadPackage' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }



        /// <summary>
        /// Returns package references for all the dependent packages
        /// </summary>
        /// <param name="fastPackageReference"></param>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        /// <returns></returns>
        public void GetPackageDependencies(string fastPackageReference, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::GetPackageDependencies' '{1}'", ProviderName, fastPackageReference);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }

                    var pkgRef = request.GetPackageByFastpath(fastPackageReference);
                    if (pkgRef == null) {
                        request.Error(ErrorCategory.InvalidArgument, fastPackageReference, Constants.Messages.UnableToResolvePackage);
                        return;
                    }

                    foreach (var depSet in pkgRef.Package.DependencySets) {
                        foreach (var dep in depSet.Dependencies) {
                            var depRefs = dep.VersionSpec == null ? request.GetPackageById(dep.Id).ToArray() : request.GetPackageByIdAndVersionSpec(dep.Id, dep.VersionSpec, true).ToArray();
                            if (depRefs.Length == 0) {
                                request.Error(ErrorCategory.InvalidResult, pkgRef.GetCanonicalId(request), Constants.Messages.DependencyResolutionError, request.GetCanonicalPackageId(ProviderName, dep.Id, ((object)dep.VersionSpec ?? "").ToString()));
                            }
                            foreach (var dependencyReference in depRefs) {
                                request.YieldPackage(dependencyReference, pkgRef.Id);
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::GetPackageDependencies' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Finds a package given a local filename
        /// </summary>
        /// <param name="file"></param>
        /// <param name="id"></param>
        /// <param name="requestObject"></param>
        public void FindPackageByFile(string file, int id, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::FindPackageByFile' '{1}','{2}'", ProviderName, file, id);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }

                    var pkgItem = request.GetPackageByFilePath(Path.GetFullPath(file));
                    if (pkgItem != null) {
                        request.YieldPackage(pkgItem, file);
                    }
                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::FindPackageByFile' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }

        /// <summary>
        /// Gets the installed packages 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="requestObject"></param>
        public void GetInstalledPackages(string name, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<T>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::GetInstalledPackages' '{1}'", ProviderName, name);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(true)) {
                        return;
                    }

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
            } catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::GetInstalledPackages' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }

    }
}