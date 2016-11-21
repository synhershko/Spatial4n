using System.Resources;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Spatial4n.Core.Portable")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Spatial4n.Core.Portable")]
[assembly: AssemblyCopyright("Copyright ©  2016")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]

// for testing
#if FEATURE_STRONG_NAME
[assembly: InternalsVisibleTo("Spatial4n.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001005dca02a2e39664" +
        "1a842d2acd4de6bf3db174844bdb3433033ba836afd645541ad94bf84e76f81eb644177ae2430e" +
        "3cde2bc02411e97f76c6eca771acf561cecbdae333ec2448ffc546907862343938fe5458194350" +
        "ac45938302bc077806e4b8603e9515b3a5e5df9047f4bc9b641c4fc2a7567eff3e9bc85036666c" +
        "a4a66f9a")]
#else
[assembly: InternalsVisibleTo("Spatial4n.Tests")]
//[assembly: InternalsVisibleTo("Lucene.Net.Tests.Spatial")]
#endif

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
