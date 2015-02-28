using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Addins;
using Pinta.Core;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle ("Pinta.Effects")]
[assembly: AssemblyDescription ("")]
[assembly: AssemblyConfiguration ("")]
[assembly: AssemblyCompany ("")]
[assembly: AssemblyProduct ("Pinta.Effects")]
[assembly: AssemblyCopyright ("")]
[assembly: AssemblyTrademark ("")]
[assembly: AssemblyCulture ("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible (false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid ("7ecbb03b-56f4-4ee1-b214-cd1657fe888b")]

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
[assembly: AssemblyVersion (PintaCore.ApplicationVersion)]
[assembly: AssemblyFileVersion (PintaCore.ApplicationVersion)]

[assembly: Addin ("DefaultEffects", PintaCore.ApplicationVersion, Category = "Core")]
[assembly: AddinName ("Default Effects")]
[assembly: AddinDescription ("The default adjustments and effects that ship with Pinta")]
[assembly: AddinDependency ("Pinta", PintaCore.ApplicationVersion)]
