// 
// AddinSetupService.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#if false
// TODO - GTK3
using Mono.Addins;
using Mono.Addins.Setup;
using Pinta.Core;

namespace Pinta
{
	public class AddinSetupService: SetupService
	{
		internal AddinSetupService (AddinRegistry r): base (r)
		{
		}
		
		public bool AreRepositoriesRegistered ()
		{
			string url = GetPlatformRepositoryUrl ();
			return Repositories.ContainsRepository (url);
		}
		
		public void RegisterRepositories (bool enable)
		{
			RegisterRepository (GetPlatformRepositoryUrl (),
			                    Translations.GetString ("Pinta Community Addins - Platform-Specific"),
			                    enable);

			RegisterRepository (GetAllRepositoryUrl (),
			                    Translations.GetString ("Pinta Community Addins - Cross-Platform"),
			                    enable);
		}

		private void RegisterRepository(string url, string name, bool enable)
		{
			if (!Repositories.ContainsRepository (url)) {
				var rep = Repositories.RegisterRepository (null, url, false);
				rep.Name = name;
				// Although repositories are enabled by default, we should always call this method to ensure
				// that the repository name from the previous line ends up being saved to disk.
				Repositories.SetRepositoryEnabled (url, enable);
			}
		}
		
		private static string GetPlatformRepositoryUrl ()
		{
			string platform;
			if (SystemManager.GetOperatingSystem () == OS.Windows)
				platform = "Windows";
			else
				if (SystemManager.GetOperatingSystem () == OS.Mac)
					platform = "Mac";
				else
					platform = "Linux";

			return "http://pintaproject.github.io/Pinta-Community-Addins/repository/" + platform + "/main.mrep";
		}

		private static string GetAllRepositoryUrl ()
		{
			return "http://pintaproject.github.io/Pinta-Community-Addins/repository/All/main.mrep";
		}
	}
}
#endif