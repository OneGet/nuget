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
    using RequestImpl = System.Object;

    public class NuGetProvider : CommonProvider<NuGetRequest> {
        static NuGetProvider() {
            _features = new Dictionary<string, string[]> {
                {
                    "supports-powershell-modules", _empty
                }, {
                    "schemes", new[] {
                        "http", "https", "file"
                    }
                }, {
                    "extensions", new[] {
                        "nupkg"
                    }
                }, {
                    "magic-signatures", _empty
                },
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
                            request.YieldDynamicOption(cat, "PackageSaveMode", OptionType.String, false, new[] {
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
