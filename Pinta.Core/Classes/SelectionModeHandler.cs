// 
// SelectionModeHandler.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2013
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
using Mono.Unix;
using ClipperLibrary;
using System.Collections.Generic;

namespace Pinta.Core
{
    public class SelectionModeHandler
    {
        private ToolBarLabel selection_label;
        private ToolBarComboBox selection_combo_box;

        private CombineMode selected_mode;
        private Dictionary<CombineMode, string> combine_modes;

        public SelectionModeHandler ()
        {
            combine_modes = new Dictionary<CombineMode, string> () {
                { CombineMode.Replace, Catalog.GetString ("Replace") },
                { CombineMode.Union, Catalog.GetString ("Union (+) (Ctrl + Left Click)") },
                { CombineMode.Exclude, Catalog.GetString ("Exclude (-) (Right Click)") },
                { CombineMode.Xor, Catalog.GetString ("Xor (Ctrl + Right Click)") },
                { CombineMode.Intersect, Catalog.GetString ("Intersect (Alt + Left Click)") },
            };
        }

        public void BuildToolbar (Gtk.Toolbar tb)
        {
            if (selection_label == null)
                selection_label = new ToolBarLabel (Catalog.GetString (" Selection Mode: "));

            tb.AppendItem (selection_label);

            if (selection_combo_box == null)
            {
                selection_combo_box = new ToolBarComboBox (170, 0, false);

                selection_combo_box.ComboBox.Changed += (o, e) =>
                {
                    Gtk.TreeIter iter;
                    if (selection_combo_box.ComboBox.GetActiveIter (out iter))
                        selected_mode = (CombineMode)selection_combo_box.Model.GetValue (iter, 1);
                };

                foreach (var mode in combine_modes)
                    selection_combo_box.Model.AppendValues (mode.Value, mode.Key);

                selection_combo_box.ComboBox.Active = 0;
            }

            tb.AppendItem (selection_combo_box);
        }

        /// <summary>
        /// Determine the current combine mode - various combinations of left/right click
        /// and Ctrl/Shift can override the selected mode from the toolbar.
        /// </summary>
        public CombineMode DetermineCombineMode (Gtk.ButtonPressEventArgs args)
        {
            CombineMode mode = selected_mode;

            if (args.Event.Button == GtkExtensions.MouseLeftButton)
            {
                if (args.Event.IsControlPressed ())
                    mode = CombineMode.Union;
                else if (args.Event.IsAltPressed ())
                    mode = CombineMode.Intersect;
            }
            else if (args.Event.Button == GtkExtensions.MouseRightButton)
            {
                if (args.Event.IsControlPressed ())
                    mode = CombineMode.Xor;
                else
                    mode = CombineMode.Exclude;
            }

            return mode;
        }

        public static void PerformSelectionMode (CombineMode mode,  List<List<IntPoint>> polygons)
        {
            var doc = PintaCore.Workspace.ActiveDocument;
            doc.Selection.Dispose ();
            doc.Selection = doc.PreviousSelection.Clone ();
            doc.Selection.Visible = true;

            using (Context g = new Context (PintaCore.Layers.CurrentLayer.Surface))
            {
                //Make sure time isn't wasted if the CombineMode is Replace - Replace is much simpler than the other 4 selection modes.
                if (mode == CombineMode.Replace)
                {
                    //Clear any previously stored Polygons.
                    doc.Selection.SelectionPolygons.Clear ();

                    //Set the resulting selection path to the new selection path.
                    doc.Selection.SelectionPolygons = polygons;
                }
                else
                {
                    var resultingPolygons = new List<List<IntPoint>> ();

                    //Specify the Clipper Subject (the previous Polygons) and the Clipper Clip (the new Polygons).
                    //Note: for Union, ignore the Clipper Library instructions - the new polygon(s) should be Clips, not Subjects!
                    doc.Selection.SelectionClipper.AddPolygons (doc.Selection.SelectionPolygons, PolyType.ptSubject);
                    doc.Selection.SelectionClipper.AddPolygons (polygons, PolyType.ptClip);

                    switch (mode)
                    {
                        case CombineMode.Xor:
                            //Xor means "Combine both Polygon sets, but leave out any areas of intersection between the two."
                            doc.Selection.SelectionClipper.Execute (ClipType.ctXor, resultingPolygons);
                            break;
                        case CombineMode.Exclude:
                            //Exclude == Difference

                            //Exclude/Difference means "Subtract any overlapping areas of the new Polygon set from the old Polygon set."
                            doc.Selection.SelectionClipper.Execute (ClipType.ctDifference, resultingPolygons);
                            break;
                        case CombineMode.Intersect:
                            //Intersect means "Leave only the overlapping areas between the new and old Polygon sets."
                            doc.Selection.SelectionClipper.Execute (ClipType.ctIntersection, resultingPolygons);
                            break;
                        default:
                            //Default should only be *CombineMode.Union*, but just in case...

                            //Union means "Combine both Polygon sets, and keep any overlapping areas as well."
                            doc.Selection.SelectionClipper.Execute (ClipType.ctUnion, resultingPolygons);
                            break;
                    }

                    //After using Clipper, it has to be cleared so there are no conflicts with its next usage.
                    doc.Selection.SelectionClipper.Clear ();

                    //Set the resulting selection path to the calculated ("clipped") selection path.
                    doc.Selection.SelectionPolygons = resultingPolygons;
                }

                doc.Selection.MarkDirty ();
            }
        }
    }

    public enum CombineMode
    {
        Union,
        Xor,
        Exclude,
        Replace,
        Intersect
    }
}
