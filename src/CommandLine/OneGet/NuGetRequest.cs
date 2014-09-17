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
    using System.Text;
    using System.Xml.Linq;
    using System.Xml.XPath;

    public abstract class NuGetRequest : BaseRequest {

        internal override string Destination {
            get {
                return Path.GetFullPath(GetOptionValue(OptionCategory.Install, "Destination"));
            }
        }

        internal XDocument Config {
            get {
                try {
                    var doc = XDocument.Load(ConfigurationFileLocation);
                    if (doc.Root != null && doc.Root.Name == "configuration") {
                        return doc;
                    }
                    // doc root isn't right. make a new one!
                }
                catch {
                    // a bad xml doc.
                }
                return XDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(Constants.DefaultConfig)));
            }
            set {
                if (value == null) {
                    return;
                }

                Verbose("Saving NuGet Config {0}", ConfigurationFileLocation);

                ProviderServices.CreateFolder(Path.GetDirectoryName(ConfigurationFileLocation), RemoteThis);
                value.Save(ConfigurationFileLocation);
            }
        }

        internal override IDictionary<string, PackageSource> RegisteredPackageSources {

            get {
                try {
                    return Config.XPathSelectElements("/configuration/packageSources/add")
                        .Where(each => each.Attribute("key") != null && each.Attribute("value") != null)
                        .ToDictionaryNicely(each => each.Attribute("key").Value, each => new PackageSource {
                            Name = each.Attribute("key").Value,
                            Location = each.Attribute("value").Value,
                            Trusted = each.Attributes("trusted").Any() && each.Attribute("trusted").Value.IsTrue(),
                            IsRegistered = true,
                            IsValidated = each.Attributes("validated").Any() && each.Attribute("validated").Value.IsTrue(),
                        }, StringComparer.OrdinalIgnoreCase);
                } catch (Exception e) {
                    e.Dump(this);
                }
                return new Dictionary<string, PackageSource>(StringComparer.OrdinalIgnoreCase) {
                    {
                        "nuget.org", new PackageSource {
                            Name = "nuget.org",
                            Location = "https://www.nuget.org/api/v2/",
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
            var source = config.XPathSelectElements(string.Format("/configuration/packageSources/add[@key='{0}']", id)).FirstOrDefault();
            if (source != null) {
                source.Remove();
                Config = config;
            }
        }

        internal override void AddPackageSource(string name, string location, bool isTrusted, bool isValidated) {
            if (SkipValidate || ValidateSourceLocation(location)) {
                var config = Config;
                var sources = config.XPathSelectElements("/configuration/packageSources").FirstOrDefault();
                if (sources == null) {
                    config.Root.Add(sources = new XElement("packageSources"));
                }
                var source = new XElement("add");
                source.SetAttributeValue("key", name);
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

        protected override string ConfigurationFileLocation {
            get {
                if (string.IsNullOrEmpty(_configurationFileLocation)) {
                    // get the value from the request
                    var path = GetOptionValue(OptionCategory.Source, "ConfigFile");
                    if (!string.IsNullOrEmpty(path)) {
                        return path;
                    }

                    //otherwise, use %APPDATA%/NuGet/NuGet.Config
                    _configurationFileLocation = Path.Combine(ProviderServices.GetKnownFolder("ApplicationData", RemoteThis), "NuGet", "NuGet.config");
                }
                return _configurationFileLocation;
            }
        }
    }
}