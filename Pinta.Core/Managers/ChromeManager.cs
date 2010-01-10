// 
// ChromeManager.cs
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
using Gtk;

namespace Pinta.Core
{
	public class ChromeManager
	{
		private Toolbar tool_toolbar;
		private Label status_bar_text;
		private DrawingArea drawing_area;
		
		public Toolbar ToolToolBar { get { return tool_toolbar; } }
		public DrawingArea DrawingArea { get { return drawing_area; } }
		
		public ChromeManager ()
		{
		}
		
		public void Initialize (Toolbar toolToolBar, Label statusBarText, DrawingArea drawingArea)
		{
			tool_toolbar = toolToolBar;
			status_bar_text = statusBarText;
			drawing_area = drawingArea;
		}
		
		public void SetStatusBarText (string text)
		{
			status_bar_text.Text = text;
		}
	}
}
