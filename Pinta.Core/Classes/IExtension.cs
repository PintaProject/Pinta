using Pinta.Core;

[assembly: Mono.Addins.AddinRoot ("Pinta", PintaCore.ApplicationVersion, CompatVersion = PintaCore.ApplicationVersion)]

namespace Pinta.Core;

[Mono.Addins.TypeExtensionPoint]
public interface IExtension
{
	void Initialize ();
	void Uninitialize ();
}
