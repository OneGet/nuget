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
    using System.Management.Automation;

    public static class ChocolateyScript {
#if FALSE
    /// <summary>
    /// This creates an RPC server that has endpoints for all the HostAPIs, then creates an elevated process that can call back into this process to report progress.
    /// 
    /// 
    /// </summary>
    /// <param name="script"></param>
    /// <returns></returns>
        internal int InvokeElevatedViaRPC(string script) {

            var guid = Guid.NewGuid();

            // set up the server IPC channel
            var serverChannel = new IpcServerChannel(guid.ToString());
            ChannelServices.RegisterChannel(serverChannel, true);
            // RemotingConfiguration.RegisterWellKnownServiceType( typeof(BaseRequest), "Request", WellKnownObjectMode.Singleton);
            var objRef = RemotingServices.Marshal(_request);
            
            // Create the client elevated
            var process = AsyncProcess.Start(new ProcessStartInfo {
                FileName = BaseRequest.NuGetExePath,
                Arguments = string.Format("-rpc {0}", objRef.URI),
                // WorkingDirectory = workingDirectory,
                WindowStyle = ProcessWindowStyle.Hidden,
                Verb = "runas",
            });

            process.WaitForExit();

            RemotingServices.Disconnect(_request);


            return 0;
        }

        
        public void Invoke(string command, params string[] arguments) {
            var p = PowerShell.Create();
            p.Runspace.SessionStateProxy.SetVariable("request", _request);
            p.AddScript(_request.HelperModuleText,false);
            p.AddScript(command);
            foreach (var result in p.Invoke()) {
                // dunno what to do with the result yet.

            }
            p.Dispose();
            p = null;


            using (dynamic ps = new DynamicPowershell()) {
                // grant access to the current call request.
                ps["request"] = _request;

                // import our new helpers
                DynamicPowershellResult result = ps.ImportModule(Name: _request.HelperModulePath, PassThru: true);
                if (!result.Success) {
                    throw new Exception("Unable to load helper module for install script.");
                }

                result = ps.InvokeExpression(command);

                if (!result.Success) {
                    foreach (var i in result.Errors) {
                        _request.Error(i.CategoryInfo.Reason, i.Exception.Message, null);
                    }
                    throw new Exception("Failed executing chocolatey script.");
                }

                ps["request"] = null;
            }
        }

#endif

       
    }
}