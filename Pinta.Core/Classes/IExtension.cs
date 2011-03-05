using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Addins;

namespace Pinta.Core
{
	[TypeExtensionPoint]
	public interface IExtension
	{
		void Initialize ();
		void Uninitialize ();
	}
}
