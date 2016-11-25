using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Spatial4n.Core.NTS")]
[assembly: AssemblyDescription("A Geospatial Library for .NET - port of Spatial4j. Spatial4n extensions for NetTopologySuite.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyProduct("Spatial4n.Core.NTS")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en")]
[assembly: CLSCompliant(true)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("efd57d2c-a197-4030-87ed-781ed35e4dc4")]

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
[assembly: AssemblyVersion("0.4.1.0")]

// NOTE: AssemblyFileVersion and AssemblyInformationalVersion attributes are managed
// by the build process in the common VersionInfo.cs file. Because we are strong-named,
// we cannot update the AssemblyVersion without making a binary-incompatible assembly.
// However, these dirty details are normally hidden behind NuGet, which uninstalls the old
// assembly and reinstalls the new one. We supply the ACTUAL version number (with a build segment) as the AssemblyFileVersion
// and, if there is any pre-release info, it is supplied as the AssemblyInformationalVersion, which is
// the only built-in .NET attribute which allows us to provide a custom version format.
