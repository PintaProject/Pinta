//
// ToolOption.cs
//
// Author:
//       Paul Korecky <https://github.com/spaghetti22>
//
// Copyright (c) 2025 Paul Korecky
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

namespace Pinta.Core;

/// <summary>
/// Defines a custom option that the user can set in the toolbar. Usable e.g. for
/// custom options on brushes.
/// </summary>
public interface ToolOption
{
	/// <summary>
	/// Retrieve an application-wide unique name usable for identifying this
	/// option.
	/// </summary>
	/// <returns>The unique name of the option.</returns>
	public string GetUniqueName ();

	/// <summary>
	/// Instruct option to set its own value from the provided settings service.
	/// </summary>
	/// <param name="settingsService">Reference to ISettingsService.</param>
	public void LoadValueFromSettings (ISettingsService settingsService);

	/// <summary>
	/// Save the current value of the option to the provided settings service.
	/// </summary>
	/// <param name="settingsService">Reference to ISettingsService.</param>
	public void SaveValueToSettings (ISettingsService settingsService);
}
