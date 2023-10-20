//
// Services.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
using Mono.Addins.Description;
using Mono.Addins.Setup;

namespace Pinta.Gui.Addins;

internal static class Utilities
{
	public static bool InApplicationNamespace (SetupService service, string id)
	{
		return service.ApplicationNamespace == null || id.StartsWith (service.ApplicationNamespace + ".");
	}

	public readonly record struct MissingDepInfo (string Addin, string Required, string Found);

	public static IEnumerable<MissingDepInfo> GetMissingDependencies (Addin addin, bool roots_only = false)
	{
		IEnumerable<Addin> allAddins = AddinManager.Registry.GetAddinRoots ();
		if (!roots_only)
			allAddins = allAddins.Union (AddinManager.Registry.GetAddins ());

		foreach (var dep in addin.Description.MainModule.Dependencies) {

			if (dep is not AddinDependency adep)
				continue;

			if (allAddins.Any (a => Addin.GetIdName (a.Id) == Addin.GetIdName (adep.FullAddinId) && a.SupportsVersion (adep.Version)))
				continue;

			Addin? found = allAddins.FirstOrDefault (a => Addin.GetIdName (a.Id) == Addin.GetIdName (adep.FullAddinId));
			yield return new MissingDepInfo (
				Addin: Addin.GetIdName (adep.FullAddinId),
				Required: adep.Version,
				Found: found?.Version ?? string.Empty
			);
		}
	}

	/// <summary>
	/// Returns whether the add-in repository entry is compatible with the addin roots (e.g. compatible with this version of the application).
	/// </summary>
	public static bool IsCompatibleWithAddinRoots (AddinRepositoryEntry a)
	{
		var roots = AddinManager.Registry.GetAddinRoots ();
		foreach (var dep in a.Addin.Dependencies) {
			if (dep is not AddinDependency adep)
				continue;
			if (roots.Any (root => Addin.GetIdName (root.Id) == Addin.GetIdName (adep.FullAddinId) && !root.SupportsVersion (adep.Version)))
				return false;
		}
		return true;
	}
}

