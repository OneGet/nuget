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
    using System.IO;
    using global::NuGet;
    using global::OneGet.ProviderSDK;

    internal class PackageSource {
        internal string Name {get; set;}
        internal string Location {get; set;}
        internal bool Trusted {get; set;}
        internal bool IsRegistered { get; set; }
        internal bool IsValidated { get; set; }

        private IPackageRepository _repository;
        internal IPackageRepository Repository {
            get {
                if (!IsSourceAFile) {
                    return _repository ?? (_repository = PackageRepositoryFactory.Default.CreateRepository(Location));
                }
                return null;
            }
        }

        internal bool IsSourceAFile {
            get {
                try {
                    if (!string.IsNullOrEmpty(Location) && File.Exists(Location)) {
                        return true;
                    }
                } catch {
                    // no worries.
                }
                return false;
            }
        }

        internal bool IsSourceADirectory {
            get {
                try {
                    if (!string.IsNullOrEmpty(Location) && Directory.Exists(Location)) {
                        return true;
                    }
                }
                catch {
                    // no worries.
                }
                return false;
            }
        }

        internal string Serialized {
            get {
                return Location.ToBase64();
            }
        }
    }
}