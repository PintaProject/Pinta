// 
// ShapesHistoryItem.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2013 & 2014
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

using System.Collections.ObjectModel;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class ShapesHistoryItem : BaseHistoryItem
{
	private readonly BaseEditEngine ee;

	private readonly UserLayer user_layer;

	private readonly SurfaceDiff? user_surface_diff;
	private ImageSurface? user_surface;

	private Collection<ShapeEngine> s_engines;

	private int selected_point_index, selected_shape_index;

	private readonly bool redraw_everything;

	/// <summary>
	/// A history item for when shapes are finalized.
	/// </summary>
	/// <param name="passedEE">The EditEngine being used.</param>
	/// <param name="icon">The history item's icon.</param>
	/// <param name="text">The history item's title.</param>
	/// <param name="passedUserSurface">The stored UserLayer surface.</param>
	/// <param name="passedUserLayer">The UserLayer being modified.</param>
	/// <param name="passedSelectedPointIndex">The selected point's index.</param>
	/// <param name="passedSelectedShapeIndex">The selected point's shape index.</param>
	/// <param name="passedRedrawEverything">Whether every shape should be redrawn when undoing (e.g. finalization).</param>
	public ShapesHistoryItem (
		BaseEditEngine passedEE,
		string icon,
		string text,
		ImageSurface passedUserSurface,
		UserLayer passedUserLayer,
		int passedSelectedPointIndex,
		int passedSelectedShapeIndex,
		bool passedRedrawEverything
	)
		: base (icon, text)
	{
		ee = passedEE;

		user_layer = passedUserLayer;


		user_surface_diff = SurfaceDiff.Create (passedUserSurface, user_layer.Surface, true);

		if (user_surface_diff == null) {
			user_surface = passedUserSurface;
		}


		s_engines = new Collection<ShapeEngine> (BaseEditEngine.SEngines.PartialClone ());
		selected_point_index = passedSelectedPointIndex;
		selected_shape_index = passedSelectedShapeIndex;

		redraw_everything = passedRedrawEverything;
	}

	public override void Undo ()
	{
		Swap (redraw_everything);
	}

	public override void Redo ()
	{
		Swap (false);
	}

	private void Swap (bool redraw)
	{
		// Grab the original surface
		ImageSurface surf = user_layer.Surface;

		if (user_surface_diff != null) {
			user_surface_diff.ApplyAndSwap (surf);

			PintaCore.Workspace.Invalidate (user_surface_diff.GetBounds ());
		} else {
			// Undo to the "old" surface
			user_layer.Surface = user_surface!; // NRT - userSurface will be not-null in this branch

			// Store the original surface for Redo
			user_surface = surf;

			//Redraw everything since surfaces were swapped.
			PintaCore.Workspace.Invalidate ();
		}

		Swap (ref s_engines, ref BaseEditEngine.SEngines);

		//Ensure that all of the shapes that should no longer be drawn have their ReEditableLayer removed from the drawing loop.
		foreach (ShapeEngine se in s_engines) {
			//Determine if it is currently in the drawing loop and should no longer be. Note: a DrawingLayer could be both removed and then
			//later added in the same swap operation, but this is faster than looping through each ShapeEngine in BaseEditEngine.SEngines.
			if (se.DrawingLayer.InTheLoop && !BaseEditEngine.SEngines.Contains (se)) {
				se.DrawingLayer.TryRemoveLayer ();
			}
		}

		//Ensure that all of the shapes that should now be drawn have their ReEditableLayer in the drawing loop.
		foreach (ShapeEngine se in BaseEditEngine.SEngines) {
			//Determine if it is currently out of the drawing loop; if not, it should be.
			if (!se.DrawingLayer.InTheLoop) {
				se.DrawingLayer.TryAddLayer ();
			}
		}

		Swap (ref selected_point_index, ref ee.SelectedPointIndex);
		Swap (ref selected_shape_index, ref ee.SelectedShapeIndex);

		//Determine if the currently active tool matches the shape's corresponding tool, and if not, switch to it.
		if (BaseEditEngine.ActivateCorrespondingTool (ee.SelectedShapeIndex, true) != null) {
			//The currently active tool now matches the shape's corresponding tool.

			if (redraw) {
				((ShapeTool?) PintaCore.Tools.CurrentTool)?.EditEngine.DrawAllShapes ();
			}
		}
	}
}
