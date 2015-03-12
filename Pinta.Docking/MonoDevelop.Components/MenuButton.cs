// 
// MenuButton.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using Gtk;

namespace MonoDevelop.Components
{
	[System.ComponentModel.Category ("MonoDevelop.Components")]
	[System.ComponentModel.ToolboxItem (true)]
	class MenuButton : Button
	{
		MenuCreator creator;
		Label label;
		Image image;
		Arrow arrow;
		bool isOpen;
		
		public MenuButton ()
			: base ()
		{
			HBox box = new HBox ();
			box.Spacing = 6;
			Add (box);
			
			image = new Image ();
			image.NoShowAll = true;
			box.PackStart (image, false, false, 0);
			label = new Label ();
			label.NoShowAll = true;
			box.PackStart (label, false, false, 0);
			ArrowType = Gtk.ArrowType.Down;
			base.Label = null;
		}
		
		protected MenuButton (IntPtr raw)
			: base (raw)
		{
			
		}
		
		public MenuCreator MenuCreator {
			get { return creator; }
			set { creator = value; }
		}
		
		protected override void OnClicked ()
		{
			base.OnClicked ();
			
			if (creator != null) {
				Menu menu = creator (this);
				
				if (menu != null) {
					isOpen = true;
					
					//make sure the button looks depressed
					ReliefStyle oldRelief = this.Relief;
					this.Relief = ReliefStyle.Normal;
					
					//clean up after the menu's done
					menu.Hidden += delegate {
						this.Relief = oldRelief ;
						isOpen = false;
						this.State = StateType.Normal;
						
						//FIXME: for some reason the menu's children don't get activated if we destroy 
						//directly here, so use a timeout to delay it
						GLib.Timeout.Add (100, delegate {
							menu.Destroy ();
							return false;
						});
					};
					menu.Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
				}
			}
			
		}
		
		protected override void OnStateChanged(StateType previous_state)
		{
			base.OnStateChanged (previous_state);
			
			//while the menu's open, make sure the button looks depressed
			if (isOpen && this.State != StateType.Active)
				this.State = StateType.Active;
		}
		
		void PositionFunc (Menu mn, out int x, out int y, out bool push_in)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			Gdk.Rectangle rect = this.Allocation;
			x += rect.X;
			y += rect.Y + rect.Height;
			
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > this.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			
			//let GTK reposition the button if it still doesn't fit on the screen
			push_in = true;
		}
		
		public ArrowType? ArrowType {
			get { return arrow == null? (Gtk.ArrowType?) null : arrow.ArrowType; }
			set {
				if (value == null) {
					if (arrow != null) {
						((HBox)arrow.Parent).Remove (arrow);
						arrow.Destroy ();
						arrow = null;
					}
				} else {
					if (arrow == null ) {
						arrow = new Arrow (Gtk.ArrowType.Down, ShadowType.Out);
						arrow.Show ();
						((HBox)label.Parent).PackEnd (arrow, false, false, 0);
					}
					arrow.ArrowType = value?? Gtk.ArrowType.Down;
				}
			}
		}
		
		protected override void OnDestroyed ()
		{
			creator = null;
			base.OnDestroyed ();
		}

		public new string Label {
			get { return label.Text; }
			set {
				label.Text = value;
				label.Visible = !string.IsNullOrEmpty (value);
			}
		}
		
		public new bool UseUnderline {
			get { return label.UseUnderline; }
			set { label.UseUnderline = value; }
		}
		
		public string StockImage {
			set {
				image.Pixbuf = RenderIcon (value, IconSize.Button, null);
				image.Show ();
			}
		}
		
		public bool UseMarkup
		{
			get { return label.UseMarkup; }
			set { label.UseMarkup = value; }
		}
		
		public string Markup {
			set { label.Markup = value; }
		}
	}
	
	delegate Menu MenuCreator (MenuButton button);
}
