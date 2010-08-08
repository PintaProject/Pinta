// 
// ActionManager.cs
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
	public class ActionManager
	{
		public AccelGroup AccelGroup { get; private set; }
		
		public FileActions File { get; private set; }
		public EditActions Edit { get; private set; }
		public ViewActions View { get; private set; }
		public ImageActions Image { get; private set; }
		public LayerActions Layers { get; private set; }
		public AdjustmentsActions Adjustments { get; private set; }
		public EffectsActions Effects { get; private set; }
		public HelpActions Help { get; private set; }
		
		public ActionManager ()
		{
			AccelGroup = new AccelGroup ();
			
			File = new FileActions ();
			Edit = new EditActions ();
			View = new ViewActions ();
			Image = new ImageActions ();
			Layers = new LayerActions ();
			Adjustments = new AdjustmentsActions ();
			Effects = new EffectsActions ();
			Help = new HelpActions ();
		}
		
		public void CreateMainMenu (Gtk.MenuBar menu)
		{
			// File menu
			ImageMenuItem file = (ImageMenuItem)menu.Children[0];
			file.Submenu = new Menu ();
			File.CreateMainMenu ((Menu)file.Submenu);
			
			//Edit menu
			ImageMenuItem edit = (ImageMenuItem)menu.Children[1];
			edit.Submenu = new Menu ();
			Edit.CreateMainMenu ((Menu)edit.Submenu);
			
			// View menu
			ImageMenuItem view = (ImageMenuItem)menu.Children[2];
			View.CreateMainMenu ((Menu)view.Submenu);
			
			// Image menu
			ImageMenuItem image = (ImageMenuItem)menu.Children[3];
			image.Submenu = new Menu ();
			Image.CreateMainMenu ((Menu)image.Submenu);
			
			//Layers menu
			ImageMenuItem layer = (ImageMenuItem)menu.Children[4];
			layer.Submenu = new Menu ();
			Layers.CreateMainMenu ((Menu)layer.Submenu);
			
			//Adjustments menu
			ImageMenuItem adj = (ImageMenuItem)menu.Children[5];
			adj.Submenu = new Menu ();
			Adjustments.CreateMainMenu ((Menu)adj.Submenu);

			// Effects menu
			ImageMenuItem eff = (ImageMenuItem)menu.Children[6];
			eff.Submenu = new Menu ();
			Effects.CreateMainMenu ((Menu)eff.Submenu);
			
			//Help menu
			ImageMenuItem help = (ImageMenuItem)menu.Children[8];
			help.Submenu = new Menu ();
			Help.CreateMainMenu ((Menu)help.Submenu);
		}
		
		public void CreateToolBar (Gtk.Toolbar toolbar)
		{
			toolbar.AppendItem (File.New.CreateToolBarItem ());
			toolbar.AppendItem (File.Open.CreateToolBarItem ());
			toolbar.AppendItem (File.Save.CreateToolBarItem ());
			//toolbar.AppendItem (File.Print.CreateToolBarItem ());
			toolbar.AppendItem (new SeparatorToolItem ());
			toolbar.AppendItem (Edit.Cut.CreateToolBarItem ());
			toolbar.AppendItem (Edit.Copy.CreateToolBarItem ());
			toolbar.AppendItem (Edit.Paste.CreateToolBarItem ());
			toolbar.AppendItem (Image.CropToSelection.CreateToolBarItem ());
			toolbar.AppendItem (Edit.Deselect.CreateToolBarItem ());
			toolbar.AppendItem (new SeparatorToolItem ());
			toolbar.AppendItem (Edit.Undo.CreateToolBarItem ());
			toolbar.AppendItem (Edit.Redo.CreateToolBarItem ());
			View.CreateToolBar (toolbar);
		}
		
		public void RegisterHandlers ()
		{
			File.RegisterHandlers ();
			Edit.RegisterHandlers ();
			Image.RegisterHandlers ();
			Layers.RegisterHandlers ();
			View.RegisterHandlers ();
			Help.RegisterHandlers ();
		}
	}
}
