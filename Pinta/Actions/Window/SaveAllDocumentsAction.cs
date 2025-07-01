//
// SaveAllDocumentsAction.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2010 Jonathan Pobst
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

using System;
using Pinta.Core;

namespace Pinta.Actions;

internal sealed class SaveAllDocumentsAction : IActionHandler
{
	private readonly WindowActions window;
	private readonly WorkspaceManager workspace;
	internal SaveAllDocumentsAction (
		WindowActions window,
		WorkspaceManager workspace)
	{
		this.window = window;
		this.workspace = workspace;
	}

	void IActionHandler.Initialize ()
	{
		window.SaveAll.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		window.SaveAll.Activated -= Activated;
	}

	private async void Activated (object sender, EventArgs e)
	{
		for (int i = 0; i < workspace.OpenDocuments.Count; ++i) {
			Document doc = workspace.OpenDocuments[i];

			if (!doc.IsDirty && doc.HasFile)
				continue;

			workspace.SetActiveDocument (i);

			// Loop through all of these until we get a cancel
			if (!await doc.Save (false))
				break;
		}
	}
}
