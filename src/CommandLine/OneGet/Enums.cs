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

    #region copy PackageProvider-types
/* Synced/Generated code =================================================== */

    public enum OptionCategory {
        Package = 0,
        Provider = 1,
        Source = 2,
        Install = 3
    }

    public enum OptionType {
        String = 0,
        StringArray = 1,
        Int = 2,
        Switch = 3,
        Folder = 4,
        File = 5,
        Path = 6,
        Uri = 7,
        SecureString = 8
    }

    public enum EnvironmentContext {
        All = 0,
        User = 1,
        System = 2
    }

    #endregion

    #region copy errorcategory-implementation
/* generated code ====================================================== */

    public enum ErrorCategory {
        NotSpecified,
        OpenError,
        CloseError,
        DeviceError,
        DeadlockDetected,
        InvalidArgument,
        InvalidData,
        InvalidOperation,
        InvalidResult,
        InvalidType,
        MetadataError,
        NotImplemented,
        NotInstalled,
        ObjectNotFound,
        OperationStopped,
        OperationTimeout,
        SyntaxError,
        ParserError,
        PermissionDenied,
        ResourceBusy,
        ResourceExists,
        ResourceUnavailable,
        ReadError,
        WriteError,
        FromStdErr,
        SecurityError,
        ProtocolError,
        ConnectionError,
        AuthenticationError,
        LimitsExceeded,
        QuotaExceeded,
        NotEnabled,
    }

    #endregion

}