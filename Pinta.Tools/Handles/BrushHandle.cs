using System;
using System.Drawing;
using Gsk;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

// That handle is used as second brush to show position of stamp tool origin
public class BrushHandle : IToolHandle
{
	private int brush_width;
	public int BrushWidth {
		get { return brush_width; }
		set {
			if (value > 0)
				brush_width = value;
		}
	}
	public bool Active { get; set; }
	public PointD CanvasPosition { get; set; }
	private readonly IWorkspaceService workspace;

	public BrushHandle (IWorkspaceService workspace)
	{
		this.workspace = workspace;
	}


	/// <summary>
	/// Drawing shape like in GdkExtensions.CreateIconWithShape
	/// </summary>
	public void Draw (Snapshot snapshot)
	{
		double zoom =
		    (PintaCore.Workspace.HasOpenDocuments)
		    ? Math.Min (30d, workspace.GetScale ())
		    : 1d;
		int clampedWidth = (int) Math.Min (800d, brush_width * zoom);

		if (clampedWidth < 3)
			return;

		int halfOfShapeWidth = clampedWidth / 2;

		RectangleF shapeRect = new () {
			X = (float) (CanvasPosition.X * zoom),
			Y = (float) (CanvasPosition.Y * zoom),
			Width = clampedWidth,
			Height = clampedWidth
		};

		Gdk.RGBA outerColor = new () {
			Red = 1.0f,
			Green = 1.0f,
			Blue = 1.0f,
			Alpha = .75f
		};
		Gdk.RGBA innerColor = new () {
			Red = 0,
			Green = 0,
			Blue = 0,
			Alpha = 1.0f
		};

		PathBuilder pathBuilder = PathBuilder.New ();
		var originPosition = new Graphene.Point ();
		originPosition.Init (shapeRect.X, shapeRect.Y);
		pathBuilder.AddCircle (originPosition, halfOfShapeWidth);
		Stroke stroke = Stroke.New (2);
		snapshot.AppendStroke (pathBuilder.ToPath (), stroke, outerColor);

		shapeRect.Inflate (-1, -1);
		pathBuilder.AddCircle (originPosition, halfOfShapeWidth);
		stroke.SetLineWidth (1);
		snapshot.AppendStroke (pathBuilder.ToPath (), stroke, innerColor);
	}

	public RectangleI InvalidateRect => ComputeWindowRect ().Inflated (2, 2).ToInt ();

	/// <summary>
	/// Bounding rectangle of the handle (in window space). Similar to MoveHandle.
	/// </summary>

	private RectangleD ComputeWindowRect ()
	{
		double diameter = brush_width;
		double radius = diameter / 2.0;

		PointD windowPt = workspace.CanvasPointToView (CanvasPosition);
		return new RectangleD (windowPt.X - radius, windowPt.Y - radius, diameter, diameter);
	}
}
