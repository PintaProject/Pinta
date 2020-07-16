using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinta.Core
{
	// TODO-GTK3 (addins)
	// [TypeExtensionPoint]
	public interface IExtension
	{
		void Initialize ();
		void Uninitialize ();
	}
}
