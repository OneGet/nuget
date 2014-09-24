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
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;

    public class Snapshot {
        private Dictionary<string, FileInfo> _files;
        private BaseRequest _request;

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Required.")]
        internal Snapshot(BaseRequest request, string folder) {
            _request = request;
            Folder = folder;
            _request.Verbose("Taking Snapshot", folder);
            _request.ProviderServices.CreateFolder(folder, _request.REQ);
            _files = Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories).ToDictionary(each => each, each => new FileInfo(each), StringComparer.OrdinalIgnoreCase);
        }

        public string Folder {get; internal set;}

        public void WriteFileDiffLog(string logPath) {
            _request.Verbose("Diffing Snapshot", Folder);
            var now = Directory.EnumerateFiles(Folder, "*", SearchOption.AllDirectories).ToDictionary(each => each, each => new FileInfo(each), StringComparer.OrdinalIgnoreCase);

            // modified
            var modified = now.Keys.Where(each => _files.ContainsKey(each) && (_files[each].Length != now[each].Length || _files[each].LastWriteTime != now[each].LastWriteTimeUtc));

            //added
            var added = now.Keys.Where(each => !_files.ContainsKey(each));

            //deleted
            var deleted = _files.Keys.Where(each => !now.ContainsKey(each));

            File.WriteAllLines(logPath, modified.Concat(added).Concat(deleted));
        }
    }
}