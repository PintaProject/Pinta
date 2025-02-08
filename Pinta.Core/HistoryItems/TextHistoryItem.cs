// 
// TextHistoryItem.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2012 Andrew Davis, GSoC 2012
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

using Cairo;

namespace Pinta.Core;

public sealed class TextHistoryItem : BaseHistoryItem
{
	readonly UserLayer user_layer;

	readonly SurfaceDiff? text_surface_diff;
	ImageSurface? text_surface;

	readonly SurfaceDiff? user_surface_diff;
	ImageSurface? user_surface;

	TextEngine text_engine;
	RectangleI text_bounds;

	private readonly IWorkspaceService workspace;

	/// <summary>
	/// A history item for when text is created, edited, and/or finalized.
	/// </summary>
	/// <param name="icon">The history item's icon.</param>
	/// <param name="text">The history item's title.</param>
	/// <param name="passedTextSurface">The stored TextLayer surface.</param>
	/// <param name="passedUserSurface">The stored UserLayer surface.</param>
	/// <param name="passedTextEngine">The text engine being used.</param>
	/// <param name="passedUserLayer">The UserLayer being modified.</param>
	public TextHistoryItem (
		IWorkspaceService workspace,
		string icon,
		string text,
		ImageSurface passedTextSurface,
		ImageSurface passedUserSurface,
		TextEngine passedTextEngine,
		UserLayer passedUserLayer
	)
		: base (icon, text)
	{
		user_layer = passedUserLayer;

		text_surface_diff = SurfaceDiff.Create (
			original: passedTextSurface,
			updated_surf: user_layer.TextLayer.Layer.Surface,
			force: true);

		if (text_surface_diff == null) {
			text_surface = passedTextSurface;
		}

		user_surface_diff = SurfaceDiff.Create (
			original: passedUserSurface,
			updated_surf: user_layer.Surface,
			force: true);

		if (user_surface_diff == null) {
			user_surface = passedUserSurface;
		}

		text_engine = passedTextEngine;

		text_bounds = new RectangleI (
			user_layer.TextBounds.X,
			user_layer.TextBounds.Y,
			user_layer.TextBounds.Width,
			user_layer.TextBounds.Height);

		this.workspace = workspace;
	}

	public override void Undo ()
	{
		Swap ();
	}

	public override void Redo ()
	{
		Swap ();
	}

	private void Swap ()
	{
		// Grab the original surface
		ImageSurface surf = user_layer.TextLayer.Layer.Surface;

		if (text_surface_diff != null) {
			text_surface_diff.ApplyAndSwap (surf);
			workspace.Invalidate (text_surface_diff.GetBounds ());
		} else {
			// Undo to the "old" surface
			user_layer.TextLayer.Layer.Surface = text_surface!; // NRT - Will be not-null if text_surface_diff is null

			// Store the original surface for Redo
			text_surface = surf;
		}

		// Grab the original surface
		surf = user_layer.Surface;

		if (user_surface_diff != null) {
			user_surface_diff.ApplyAndSwap (surf);
			workspace.Invalidate (user_surface_diff.GetBounds ());
		} else {
			// Undo to the "old" surface
			user_layer.Surface = user_surface!; // NRT - Will be not-null if user_surface_diff is null

			// Store the original surface for Redo
			user_surface = surf;
		}

		//Redraw everything since surfaces were swapped.
		workspace.Invalidate ();

		//Store the old text data temporarily.
		TextEngine oldTextEngine = text_engine;
		RectangleI oldTextBounds = text_bounds;

		//Swap half of the data.
		text_engine = user_layer.TextEngine;
		text_bounds = user_layer.TextBounds;

		//Swap the other half.
		user_layer.TextEngine = oldTextEngine;
		user_layer.TextBounds = oldTextBounds;
	}
}
