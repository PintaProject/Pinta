using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinta.Actions
{
	interface IActionHandler
	{
		void Initialize ();
		void Uninitialize ();
	}
}
