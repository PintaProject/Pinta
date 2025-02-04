// 
// ConfigurableEffectHelper.cs
//  
// Author:
//       Greg Lowe <greg@vis.net.nz>
// 
// Copyright (c) 2010 Greg Lowe
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

using System.Threading.Tasks;
using Mono.Addins.Localization;
using Pinta.Core;

namespace Pinta;

internal static class EffectHelper
{
	/// <summary>
	/// Launch an effect dialog using Pinta's translation template.
	/// </summary>
	internal static Task<bool> LaunchSimpleEffectDialog (this IChromeService chrome, BaseEffect effect, IWorkspaceService workspace)
		=> chrome.LaunchSimpleEffectDialog (
			chrome.MainWindow,
			effect,
			new PintaLocalizer (),
			workspace);
}

/// <summary>
/// Wrapper around Pinta's translation template.
/// </summary>
internal sealed class PintaLocalizer : IAddinLocalizer
{
	public string GetString (string msgid)
		=> Translations.GetString (msgid);
};
