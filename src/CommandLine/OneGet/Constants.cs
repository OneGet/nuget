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

    internal static class Constants {
        #region copy constants-implementation
/* Synced/Generated code =================================================== */

        internal const string MSGPrefix = "MSG:";
        internal const string TerminatingError = "MSG:TerminatingError";
        internal const string SourceLocationNotValid = "MSG:SourceLocationNotValid_Location";
        internal const string UriSchemeNotSupported = "MSG:UriSchemeNotSupported_Scheme";
        internal const string UnableToResolveSource = "MSG:UnableToResolveSource_NameOrLocation";
        internal const string PackageFailedInstall = "MSG:UnableToInstallPackage_package_reason";
        internal const string DependencyResolutionError = "MSG:UnableToResolveDependency_dependencyPackage";
        internal const string DependentPackageFailedInstall = "MSG:DependentPackageFailedInstall_dependencyPackage";

        #endregion

        internal const string ProviderName = "NuGet";

        internal const string DefaultConfig = @"<?xml version=""1.0""?>
<configuration>
  <packageSources>
    <add key=""nuget.org"" value=""https://www.nuget.org/api/v2/"" />
  </packageSources>
</configuration>";

        // reasons why things go bad
        internal const string ReasonUnknown = "MSG:UnknownReason";

        // NuGet specific errors
        internal const string UnableToResolvePackageReference = "MSG:PackageReferenceInvalid";
        internal const string MultiplePackagesInstalledExpectedOne = "MSG:MultiplePackagesInstalledExpectedOne_package";

    }
}