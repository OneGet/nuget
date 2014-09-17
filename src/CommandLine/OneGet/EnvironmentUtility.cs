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
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    public static class EnvironmentUtility {
        private const Int32 HWND_BROADCAST = 0xffff;
        private const Int32 WM_SETTINGCHANGE = 0x001A;
        private const Int32 SMTO_ABORTIFHUNG = 0x0002;

        public static IEnumerable<string> SystemPath {
            get {
                var path = GetSystemEnvironmentVariable("PATH");
                return string.IsNullOrEmpty(path) ? new string[] {
                } : path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set {
                var newValue = value.ToPathString();
                if (newValue != GetSystemEnvironmentVariable("PATH")) {
                    SetSystemEnvironmentVariable("PATH", newValue);
                }
            }
        }

        public static IEnumerable<string> UserPath {
            get {
                var path = GetUserEnvironmentVariable("PATH");
                return string.IsNullOrEmpty(path) ? new string[] {
                } : path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set {
                var newValue = value.ToPathString();
                if (newValue != GetUserEnvironmentVariable("PATH")) {
                    SetUserEnvironmentVariable("PATH", newValue);
                }
            }
        }

        public static IEnumerable<string> Path {
            get {
                var path = GetEnvironmentVariable("PATH");
                return string.IsNullOrEmpty(path) ? new string[] {
                } : path.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            }
            set {
                var newValue = value.ToPathString();
                if (newValue != GetEnvironmentVariable("PATH")) {
                    SetEnvironmentVariable("PATH", newValue);
                }
            }
        }

        public static void BroadcastChange() {
            Task.Factory.StartNew(() => {NativeMethods.SendMessageTimeout(HWND_BROADCAST, WM_SETTINGCHANGE, 0, "Environment", SMTO_ABORTIFHUNG, 1000, IntPtr.Zero);}, TaskCreationOptions.LongRunning);
        }

        public static string GetSystemEnvironmentVariable(string name) {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Machine);
        }

        public static void SetSystemEnvironmentVariable(string name, string value) {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Machine);
        }

        public static string GetUserEnvironmentVariable(string name) {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User);
        }

        public static void SetUserEnvironmentVariable(string name, string value) {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.User);
        }

        public static string GetEnvironmentVariable(string name) {
            return Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
        }

        public static void SetEnvironmentVariable(string name, string value) {
            Environment.SetEnvironmentVariable(name, value, EnvironmentVariableTarget.Process);
        }

        public static string ToPathString(this IEnumerable<string> value) {
            return value.SafeAggregate((current, each) => current + ";" + each) ?? string.Empty;
        }

        public static string[] Append(this IEnumerable<string> searchPath, string pathToAdd) {
            var p = searchPath.ToArray();

            if (p.Any(s => s.EqualsIgnoreCase(pathToAdd))) {
                return p;
            }
            return p.Union(new[] {
                pathToAdd
            }).ToArray();
        }

        public static string[] Prepend(this IEnumerable<string> searchPath, string pathToAdd) {
            var p = searchPath.ToArray();

            if (p.Any(s => s.EqualsIgnoreCase(pathToAdd))) {
                return p;
            }

            return new[] {
                pathToAdd
            }.Union(p).ToArray();
        }

        public static string[] Remove(this string[] searchPath, string pathToRemove) {
            return searchPath.Where(s => !s.EqualsIgnoreCase(pathToRemove)).ToArray();
        }

        public static string[] RemoveMissingFolders(this string[] searchPath) {
            return searchPath.Where(Directory.Exists).ToArray();
        }

        public static void Rehash() {
            var system = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine);
            var user = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User);

            // do system/shared variables first
            foreach (var key in system.Keys) {
                var value = system[key].ToString();
                if (string.IsNullOrEmpty(value)) {
                    continue;
                }

                // merge path-like variables.
                if (key.ToString().IndexOf("path", StringComparison.OrdinalIgnoreCase) > -1 && user.Contains(key)) {
                    value = value + ";" + user[key];
                    user.Remove(key);
                }

                Environment.SetEnvironmentVariable(key.ToString(), value, EnvironmentVariableTarget.Process);
            }

            // do user variables next
            foreach (var key in user.Keys) {
                var value = user[key].ToString();
                if (string.IsNullOrEmpty(value)) {
                    continue;
                }

                Environment.SetEnvironmentVariable(key.ToString(), value, EnvironmentVariableTarget.Process);
            }
        }


        /// <summary>
        ///     Gets the relative path between two paths.
        /// </summary>
        /// <param name="currentDirectory"> The current directory. </param>
        /// <param name="pathToMakeRelative"> The path to make relative. </param>
        /// <returns> </returns>
        /// <remarks>
        /// </remarks>
        public static string RelativePathTo(this string currentDirectory, string pathToMakeRelative) {
            if (string.IsNullOrEmpty(currentDirectory)) {
                throw new ArgumentNullException("currentDirectory");
            }

            if (string.IsNullOrEmpty(pathToMakeRelative)) {
                throw new ArgumentNullException("pathToMakeRelative");
            }

            currentDirectory = System.IO.Path.GetFullPath(currentDirectory);
            pathToMakeRelative = System.IO.Path.GetFullPath(pathToMakeRelative);

            if (!System.IO.Path.GetPathRoot(currentDirectory).Equals(System.IO.Path.GetPathRoot(pathToMakeRelative), StringComparison.CurrentCultureIgnoreCase)) {
                return pathToMakeRelative;
            }

            var relativePath = new List<string>();
            var currentDirectoryElements = currentDirectory.Split(System.IO.Path.DirectorySeparatorChar);
            var pathToMakeRelativeElements = pathToMakeRelative.Split(System.IO.Path.DirectorySeparatorChar);
            var commonDirectories = 0;

            for (; commonDirectories < Math.Min(currentDirectoryElements.Length, pathToMakeRelativeElements.Length); commonDirectories++) {
                if (
                    !currentDirectoryElements[commonDirectories].Equals(pathToMakeRelativeElements[commonDirectories], StringComparison.CurrentCultureIgnoreCase)) {
                    break;
                }
            }

            for (var index = commonDirectories; index < currentDirectoryElements.Length; index++) {
                if (currentDirectoryElements[index].Length > 0) {
                    relativePath.Add("..");
                }
            }

            for (var index = commonDirectories; index < pathToMakeRelativeElements.Length; index++) {
                relativePath.Add(pathToMakeRelativeElements[index]);
            }

            return string.Join(System.IO.Path.DirectorySeparatorChar.ToString(CultureInfo.InvariantCulture), relativePath);
        }
    }
}