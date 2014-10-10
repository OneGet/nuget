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
    using global::OneGet.ProviderSDK;
    using IRequestObject = System.Object;

    /// <summary>
    /// Chocolatey Package provider for OneGet.
    /// 
    /// 
    /// Important notes:
    ///    - Required Methods: Not all methods are required; some package providers do not support some features. If the methods isn't used or implemented it should be removed (or commented out)
    ///    - Error Handling: Avoid throwing exceptions from these methods. To properly return errors to the user, use the request.Error(...) method to notify the user of an error conditionm and then return.
    ///    - Communicating with the HOST and CORE: each method takes a IRequestObject (in reality, an alias for System.Object), which can be used in one of two ways:
    ///         - use the c# 'dynamic' keyword, and call functions on the object directly.
    ///         - use the <code><![CDATA[ .As<Request>() ]]></code> extension method to strongly-type it to the Request type (which calls upon the duck-typer to generate a strongly-typed wrapper).  The strongly-typed wrapper also implements several helper functions to make using the request object easier.
    /// </summary>
    public class ChocolateyProvider : CommonProvider<ChocolateyRequest> {

        static ChocolateyProvider() {
            _features = new Dictionary<string, string[]> {
                { Constants.Features.SupportedSchemes, new [] {"http", "https", "file"} },
                {Constants.Features.SupportedExtensions, new [] {"nupkg"} },
                { Constants.Features.MagicSignatures, new [] {Constants.Signatures.Zip } },
            };
        }

        /// <summary>
        /// The name of this Package Provider
        /// </summary>
        internal override string ProviderName {
            get {
                return "Chocolatey";
            }
        }

        /// <summary>
        /// Performs one-time initialization of the PROVIDER.
        /// </summary>
        /// <param name="requestObject">An object passed in from the CORE that contains functions that can be used to interact with the CORE and HOST</param>
        public void InitializeProvider(IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<ChocolateyRequest>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::InitializeProvider'", ProviderName);

                    // Check to see if we're ok to proceed.
                    if (!request.IsReady(false)) {
                        return;
                    }
                }
            }
            catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::InitializeProvider' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }


        public override void GetDynamicOptions(string category, IRequestObject requestObject) {
            using (var request = requestObject.As<ChocolateyRequest>()) {
                try {
                    request.Debug("Calling '{0}::GetDynamicOptions' '{1}'", ProviderName, category);

                    switch ((category ?? string.Empty).ToLowerInvariant()) {
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
                            request.YieldDynamicOption("SkipDependencies", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("ContinueOnFailure", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("ExcludeVersion", global::OneGet.ProviderSDK.Constants.OptionType.Switch, false);
                            request.YieldDynamicOption("PackageSaveMode", global::OneGet.ProviderSDK.Constants.OptionType.String, false, new[] {
                                "nuspec", "nupkg", "nuspec;nupkg"
                            });
                            break;
                    }
                }
                catch {
                    // this makes it ignore new OptionCategories that it doesn't know about.
                }
            }
        }

        public void ExecuteElevatedAction(string payload, IRequestObject requestObject) {
            try {
                // create a strongly-typed request object.
                using (var request = requestObject.As<ChocolateyRequest>()) {
                    // Nice-to-have put a debug message in that tells what's going on.
                    request.Debug("Calling '{0}::ExecuteElevatedAction' '{1}'", ProviderName, payload);
                    if (!request.Invoke(payload)) {
                        request.Error(ErrorCategory.InvalidResult, "Chocolatey Install Script", Constants.Messages.PackageFailedInstall);
                    }
                }
            } catch (Exception e) {
                // We shoudn't throw exceptions from here, it's not-optimal. And if the exception class wasn't properly Serializable, it'd cause other issues.
                // Really this is just here as a precautionary to behave correctly.
                // At the very least, we'll write it to the system debug channel, so a developer can find it if they are looking for it.
                Debug.WriteLine(string.Format("Unexpected Exception thrown in '{0}::ExecuteElevatedAction' -- {1}\\{2}\r\n{3}", ProviderName, e.GetType().Name, e.Message, e.StackTrace));
            }
        }
    }
}