//
// PrintDocumentAction.cs
//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2012 Cameron White
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
using Gtk;
using Pinta.Core;

namespace Pinta.Actions
{
	public class PrintDocumentAction : IActionHandler
	{
		#region IActionHandler implementation

		public void Initialize ()
		{
			PintaCore.Actions.File.Print.Activated += HandleActivated;
		}

		public void Uninitialize ()
		{
			PintaCore.Actions.File.Print.Activated -= HandleActivated;
		}

		#endregion
		
		void HandleActivated (object sender, EventArgs e)
		{
			// Commit any pending changes.
			PintaCore.Tools.Commit ();

			var op = new PrintOperation ();
			op.BeginPrint += HandleBeginPrint;
			op.DrawPage += HandleDrawPage;

			var result = op.Run (PrintOperationAction.PrintDialog, PintaCore.Chrome.MainWindow);

			if (result == PrintOperationResult.Apply) {
				// TODO - save print settings.
			} else if (result == PrintOperationResult.Error) {
				// TODO - show a proper dialog.
				System.Console.WriteLine ("Printing error");
			}
		}

		void HandleDrawPage (object o, DrawPageArgs args)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			// TODO - support scaling to fit page, centering image, etc.

			using (var surface = doc.GetFlattenedImage ()) {
				using (var context = args.Context.CairoContext) {
					context.SetSourceSurface (surface, 0, 0);
					context.Paint ();
				}
			}
		}

		void HandleBeginPrint (object o, BeginPrintArgs args)
		{
			PrintOperation op = (PrintOperation)o;
			op.NPages = 1;
		}
	}
}

