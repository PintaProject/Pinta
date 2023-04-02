using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pinta.Core;

[assembly: Mono.Addins.AddinRoot ("Pinta", PintaCore.ApplicationVersion)]

namespace Pinta.Core
{
	[Mono.Addins.TypeExtensionPoint]
	public interface IExtension
	{
		void Initialize ();
		void Uninitialize ();
	}
}
