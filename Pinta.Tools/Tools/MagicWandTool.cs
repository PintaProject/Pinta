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
		private CombineMode combineMode;
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.S; } }

		public MagicWandTool()
		{
			LimitToSelection = false;
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
			get { return new Gdk.Cursor(PintaCore.Chrome.Canvas.Display, PintaCore.Resources.GetIcon("Tools.MagicWand.png"), 0, 0); }
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

			//Left Click (usually) - also the default
			combineMode = CombineMode.Replace;

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

			List<List<IntPoint>> newPolygons = new List<List<IntPoint>>();

			foreach (Point[] pA in polygonSet)
			{
				List<IntPoint> newPolygon = new List<IntPoint>();

				foreach (Point p in pA)
				{
					newPolygon.Add(new IntPoint((long)p.X, (long)p.Y));
				}

				//Add the first point again.
				newPolygon.Add(new IntPoint((long)pA[0].X, (long)pA[0].Y));

				newPolygons.Add(newPolygon);
			}

			SelectionHistoryItem undoAction = new SelectionHistoryItem(this.Icon, this.Name);
			undoAction.TakeSnapshot();

			using (Context g = new Context(PintaCore.Layers.CurrentLayer.Surface))
			{
				switch (combineMode)
				{
					case CombineMode.Union:
						//Everything in Union is a Subject, not a Clip.

						List<List<IntPoint>> resultingPolygons = new List<List<IntPoint>>();
						Point[][] resultingPolygonSet;

						doc.SelectionClipper.AddPolygons(newPolygons, PolyType.ptSubject);
						doc.SelectionClipper.Execute(ClipType.ctUnion, resultingPolygons);

						doc.SelectionClipper.Clear();
						doc.SelectionClipper.AddPolygons(resultingPolygons, PolyType.ptSubject);

						resultingPolygonSet = new Point[resultingPolygons.Count][];

						int polygonNumber = 0;

						foreach (List<IntPoint> ipL in resultingPolygons)
						{
							resultingPolygonSet[polygonNumber] = new Point[ipL.Count];

							int pointNumber = 0;

							foreach (IntPoint ip in ipL)
							{
								resultingPolygonSet[polygonNumber][pointNumber] = new Point((int)ip.X, (int)ip.Y);

								++pointNumber;
							}

							++polygonNumber;
						}

						doc.SelectionPath = g.CreatePolygonPath(resultingPolygonSet);
						break;
					case CombineMode.Xor:

						doc.SelectionPath = g.CopyPath();
						break;
					case CombineMode.Exclude:

						break;
					case CombineMode.Intersect:

						break;
					default:
						//Set the resulting selection path to the new selection path.
						doc.SelectionPath = g.CreatePolygonPath(polygonSet);
						doc.SelectionClipper.Clear();
						doc.SelectionClipper.AddPolygons(newPolygons, PolyType.ptSubject);
						break;
				}
			}

			doc.History.PushNewItem(undoAction);
			doc.Workspace.Invalidate();
		}
	}
}