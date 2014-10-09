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
    using System.Collections.Generic;
    using System.Reflection;
    using global::OneGet.ProviderSDK;
    using IRequestObject = System.Object;

    public class NuGetProvider : CommonProvider<NuGetRequest> {
        static NuGetProvider() {
            _features = new Dictionary<string, string[]> {
                {"supports-powershell-modules", _empty},
                {"uri-schemes", new[] {"http", "https", "file"}},
                {"file-extensions", new[] {"nupkg"}},
                {"magic-signatures", new[] {"50b40304"}},
                // { global::OneGet.ProviderSDK.Constants.Features.AutomationOnly, _empty }
            };
        }

        internal override string ProviderName {
            get {
                return "NuGet";
            }
        }

        public void InitializeProvider(IRequestObject requestObject) {
            _features.AddOrSet("exe", new[] {
                Assembly.GetAssembly(typeof (NuGet.PackageSource)).Location
            });

            // create a strongly-typed request object.
            using (var request = requestObject.As<NuGetRequest>()) {
                // Nice-to-have put a debug message in that tells what's going on.
                request.Debug("Calling '{0}::InitializeProvider'", ProviderName);

                // Check to see if we're ok to proceed.
                if (!request.IsReady(false)) {
                }
            }
        }

        public override void GetDynamicOptions(string category, IRequestObject requestObject) {
            using (var request = requestObject.As<NuGetRequest>()) {
                try {
                    request.Debug("Calling '{0}::GetDynamicOptions' '{1}'", "NuGet", category);

                    switch ((category ?? string.Empty).ToLowerInvariant()) {
                        case "package":
                            request.YieldDynamicOption("Tag", Constants.OptionType.StringArray, false);
                            request.YieldDynamicOption("Contains", Constants.OptionType.String, false);
                            request.YieldDynamicOption("AllowPrereleaseVersions", Constants.OptionType.Switch, false);
                            break;

                        case "source":
                            request.YieldDynamicOption("ConfigFile", Constants.OptionType.String, false);
                            request.YieldDynamicOption("SkipValidate", Constants.OptionType.Switch, false);
                            break;

                        case "install":
                            request.YieldDynamicOption("Destination", Constants.OptionType.Path, true);
                            request.YieldDynamicOption("SkipDependencies", Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("ContinueOnFailure", Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("ExcludeVersion", Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("PackageSaveMode", Constants.OptionType.String, false, new[] {
                                "nuspec", "nupkg", "nuspec;nupkg"
                            });
                            break;
                    }
                } catch {
                    // this makes it ignore new OptionCategories that it doesn't know about.
                }
            }
        }

        /* NOT SUPPORTED BY NUGET -- AT THIS TIME 
        public void FindPackageByUri(Uri uri, int id, IRequestObject requestObject) {
            using (var request =requestObject.As<Request>()) {
                request.Debug("Calling 'NuGet::FindPackageByUri'");

                // check if this URI is a valid source
                // if it is, get the list of packages from this source

                // otherwise, download the Uri and see if it's a package 
                // that we support.
            }
        }
         */
    }
}