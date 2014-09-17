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

    public abstract class ProviderServicesApi {
        #region copy service-apis

        /* Synced/Generated code =================================================== */
        public abstract void DownloadFile(Uri remoteLocation, string localFilename, MarshalByRefObject requestImpl);

        public abstract bool IsSupportedArchive(string localFilename, MarshalByRefObject requestImpl);

        public abstract IEnumerable<string> UnpackArchive(string localFilename, string destinationFolder, MarshalByRefObject requestImpl);

        public abstract void AddPinnedItemToTaskbar(string item, MarshalByRefObject requestImpl);

        public abstract void RemovePinnedItemFromTaskbar(string item, MarshalByRefObject requestImpl);

        public abstract void CreateShortcutLink(string linkPath, string targetPath, string description, string workingDirectory, string arguments, MarshalByRefObject requestImpl);

        public abstract void SetEnvironmentVariable(string variable, string value, int context, MarshalByRefObject requestImpl);

        public abstract void RemoveEnvironmentVariable(string variable, int context, MarshalByRefObject requestImpl);

        public abstract void CopyFile(string sourcePath, string destinationPath, MarshalByRefObject requestImpl);

        public abstract void Delete(string path, MarshalByRefObject requestImpl);

        public abstract void DeleteFolder(string folder, MarshalByRefObject requestImpl);

        public abstract void CreateFolder(string folder, MarshalByRefObject requestImpl);

        public abstract void DeleteFile(string filename, MarshalByRefObject requestImpl);

        public abstract string GetKnownFolder(string knownFolder, MarshalByRefObject requestImpl);

        public abstract bool IsElevated {get;}

        #endregion
    }
}