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
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using global::NuGet;
    using global::OneGet.ProviderSDK;
    using RequestImpl = System.Object;

    public class NuGetProvider : CommonProvider<NuGetRequest> {
        static NuGetProvider() {
            _features = new Dictionary<string, string[]> {
                { "supports-powershell-modules", _empty },
                { "schemes", new[] { "http", "https", "file" }
                }, { "extensions", new[] { "nupkg" }
                }, { "magic-signatures", _empty },
                { global::OneGet.ProviderSDK.Constants.Features.AutomationOnly, _empty }
            };
        }

        internal override string ProviderName {
            get {
                return "NuGet";
            }
        }

        public void InitializeProvider(object dynamicInterface, RequestImpl requestImpl) {
            RequestExtensions.RemoteDynamicInterface = dynamicInterface;
            _features.AddOrSet("exe", new[] {
                Assembly.GetAssembly(typeof (global::NuGet.PackageSource)).Location
            });
        }

        public override void GetDynamicOptions(string category, RequestImpl requestImpl) {
            using (var request = requestImpl.As<NuGetRequest>()) {
                try {
                    request.Debug("Calling '{0}::GetDynamicOptions' '{1}'", "NuGet", category);

                    switch((category??string.Empty).ToLowerInvariant()){
                        case "package":
                            request.YieldDynamicOption("Tag", global::OneGet.ProviderSDK.Constants.OptionType.StringArray, false);
                            request.YieldDynamicOption("Contains", global::OneGet.ProviderSDK.Constants.OptionType.String, false);
                            request.YieldDynamicOption("AllowPrereleaseVersions", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            break;

                        case "source":
                            request.YieldDynamicOption("ConfigFile", global::OneGet.ProviderSDK.Constants.OptionType.String, false);
                            request.YieldDynamicOption("SkipValidate", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            break;

                        case "install":
                            request.YieldDynamicOption("Destination", global::OneGet.ProviderSDK.Constants.OptionType.Path, true);
                            request.YieldDynamicOption("SkipDependencies", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("ContinueOnFailure", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("ExcludeVersion", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("PackageSaveMode", global::OneGet.ProviderSDK.Constants.OptionType.String, false, new[] {
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


    }
}
