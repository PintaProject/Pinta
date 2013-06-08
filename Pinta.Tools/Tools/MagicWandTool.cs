// 
// MagicWandTool.cs
//  
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright (c) 2010 Olivier Dufour
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
using Cairo;
using Pinta.Core;
using Mono.Unix;
using ClipperLibrary;
using System.Collections.Generic;

namespace Pinta.Tools
{
	public class MagicWandTool : FloodTool
	{
		private Gtk.ToolItem selection_sep;
		private ToolBarLabel selection_label;
		private ToolBarComboBox selection_combo_box;

		private Dictionary<int, string> selectionCombinations = new Dictionary<int, string>();

		private int SelectionMode = 0;

		private CombineMode combineMode;
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.S; } }

		public MagicWandTool()
		{
			LimitToSelection = false;

			selectionCombinations.Add(0, Catalog.GetString("Replace"));
			selectionCombinations.Add(1, Catalog.GetString("Union (+) (Ctrl + Left Click)"));
			selectionCombinations.Add(2, Catalog.GetString("Exclude (-) (Right Click)"));
			selectionCombinations.Add(3, Catalog.GetString("Xor (Ctrl + Right Click)"));
			selectionCombinations.Add(4, Catalog.GetString("Intersect (Shift + Left Click)"));
		}

		protected override void OnBuildToolBar(Gtk.Toolbar tb)
		{
			base.OnBuildToolBar(tb);

			if (selection_sep == null)
				selection_sep = new Gtk.SeparatorToolItem();

			tb.AppendItem(selection_sep);

			if (selection_label == null)
				selection_label = new ToolBarLabel(Catalog.GetString(" Selection Mode: "));

			tb.AppendItem(selection_label);


			if (selection_combo_box == null)
			{
				selection_combo_box = new ToolBarComboBox(170, 0, false);

				selection_combo_box.ComboBox.Changed += (o, e) =>
				{
					Gtk.TreeIter iter;

					if (selection_combo_box.ComboBox.GetActiveIter(out iter))
					{
						SelectionMode = ((KeyValuePair<int, string>)selection_combo_box.Model.GetValue(iter, 1)).Key;
					}
				};

				foreach(KeyValuePair<int, string> sel in selectionCombinations)
				{
					selection_combo_box.Model.AppendValues(sel.Value, sel);
				}

				selection_combo_box.ComboBox.Active = 0;
			}

			tb.AppendItem(selection_combo_box);
		}

		public override string Name
		{
			get { return Catalog.GetString("Magic Wand Select"); }
		}

		public override string Icon
		{
			get { return "Tools.MagicWand.png"; }
		}

		public override string StatusBarText
		{
			get { return Catalog.GetString("Click to select region of similar color."); }
		}

		public override Gdk.Cursor DefaultCursor
		{
			get { return new Gdk.Cursor(PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon("Cursor.MagicWand.png"), 21, 10); }
		}
		public override int Priority { get { return 17; } }

		private enum CombineMode
		{
			Union, //Control + Left Click
			Xor, //Control + Right Click
			Exclude, //Right Click
			Replace, //Left Click (and default)
			Intersect //Shift + Left Click
		}

		protected override void OnMouseDown(Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			//SetCursor (Cursors.WaitCursor);

			//Here is where the CombineMode for the Magic Wand Tool's selection is determined based on None/Ctrl/Shift + Left/Right Click.

			switch (SelectionMode)
			{
				case 1:
					combineMode = CombineMode.Union;
					break;
				case 2:
					combineMode = CombineMode.Exclude;
					break;
				case 3:
					combineMode = CombineMode.Xor;
					break;
				case 4:
					combineMode = CombineMode.Intersect;
					break;
				default:
					//Left Click (usually) - also the default
					combineMode = CombineMode.Replace;
					break;
			}

			if (args.Event.Button == 1)
			{
				if (args.Event.IsControlPressed())
				{
					//Control + Left Click
					combineMode = CombineMode.Union;
				}
				else if (args.Event.IsShiftPressed())
				{
					//Shift + Left Click
					combineMode = CombineMode.Intersect;
				}
			}
			else if (args.Event.Button == 3)
			{
				if (args.Event.IsControlPressed())
				{
					//Control + Right Click
					combineMode = CombineMode.Xor;
				}
				else
				{
					//Right Click
					combineMode = CombineMode.Exclude;
				}
			}

			base.OnMouseDown(canvas, args, point);

			doc.ShowSelection = true;
		}

		protected override void OnFillRegionComputed(Point[][] polygonSet)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			SelectionHistoryItem undoAction = new SelectionHistoryItem(this.Icon, this.Name);
			undoAction.TakeSnapshot();

			//Convert Pinta's passed in Polygon Set to a Clipper Polygon collection.
			List<List<IntPoint>> newPolygons = DocumentSelection.ConvertToPolygons(polygonSet);

			using (Context g = new Context(PintaCore.Layers.CurrentLayer.Surface))
			{
				//Make sure time isn't wasted if the CombineMode is Replace - Replace is much simpler than the other 4 selection modes.
				if (combineMode == CombineMode.Replace)
				{
					//Clear any previously stored Polygons.
					doc.Selection.SelectionPolygons.Clear();

					//Set the resulting selection path to the new selection path.
					doc.Selection.SelectionPolygons = newPolygons;
					doc.Selection.SelectionPath = g.CreatePolygonPath(polygonSet);
				}
				else
				{
					List<List<IntPoint>> resultingPolygons = new List<List<IntPoint>>();

					//Specify the Clipper Subject (the previous Polygons) and the Clipper Clip (the new Polygons).
					//Note: for Union, ignore the Clipper Library instructions - the new polygon(s) should be Clips, not Subjects!
					doc.Selection.SelectionClipper.AddPolygons(doc.Selection.SelectionPolygons, PolyType.ptSubject);
					doc.Selection.SelectionClipper.AddPolygons(newPolygons, PolyType.ptClip);

					switch (combineMode)
					{
						case CombineMode.Xor:
							//Xor means "Combine both Polygon sets, but leave out any areas of intersection between the two."
							doc.Selection.SelectionClipper.Execute(ClipType.ctXor, resultingPolygons);
							break;
						case CombineMode.Exclude:
							//Exclude == Difference

							//Exclude/Difference means "Subtract any overlapping areas of the new Polygon set from the old Polygon set."
							doc.Selection.SelectionClipper.Execute(ClipType.ctDifference, resultingPolygons);
							break;
						case CombineMode.Intersect:
							//Intersect means "Leave only the overlapping areas between the new and old Polygon sets."
							doc.Selection.SelectionClipper.Execute(ClipType.ctIntersection, resultingPolygons);
							break;
						default:
							//Default should only be *CombineMode.Union*, but just in case...

							//Union means "Combine both Polygon sets, and keep any overlapping areas as well."
							doc.Selection.SelectionClipper.Execute(ClipType.ctUnion, resultingPolygons);
							break;
					}

					//After using Clipper, it has to be cleared so there are no conflicts with its next usage.
					doc.Selection.SelectionClipper.Clear();

					//Set the resulting selection path to the calculated ("clipped") selection path.
					doc.Selection.SelectionPolygons = resultingPolygons;
					doc.Selection.SelectionPath = g.CreatePolygonPath(DocumentSelection.ConvertToPolygonSet(resultingPolygons));
				}
			}

			doc.History.PushNewItem(undoAction);
			doc.Workspace.Invalidate();
		}
	}
}
