using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("Outercurve Foundation")]
[assembly: AssemblyProduct("NuGet")]
[assembly: AssemblyCopyright("\x00a9 Outercurve Foundation. All rights reserved.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyTrademark("")]

[assembly: ComVisible(false)]

// When built on the build server, the NuGet release version is specified in
// Build\Build.proj.
// When built locally, the NuGet release version is the values specified in this file.
#if !FIXED_ASSEMBLY_VERSION
[assembly: AssemblyVersion("2.8.3.6")]
[assembly: AssemblyInformationalVersion("2.8.3-oneget")]
#endif

[assembly: NeutralResourcesLanguage("en-US")]





