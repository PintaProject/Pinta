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
	private readonly WindowActions window_actions;
	private readonly WorkspaceManager workspace_manager;
	internal SaveAllDocumentsAction (
		WindowActions windowActions,
		WorkspaceManager workspaceManager)
	{
		window_actions = windowActions;
		workspace_manager = workspaceManager;
	}

	void IActionHandler.Initialize ()
	{
		window_actions.SaveAll.Activated += Activated;
	}

	void IActionHandler.Uninitialize ()
	{
		window_actions.SaveAll.Activated -= Activated;
	}

	private void Activated (object sender, EventArgs e)
	{
		foreach (Document doc in workspace_manager.OpenDocuments) {

			if (!doc.IsDirty && doc.HasFile)
				continue;

			window_actions.SetActiveDocument (doc);

			// Loop through all of these until we get a cancel
			if (!doc.Save (false))
				break;
		}
	}
}
