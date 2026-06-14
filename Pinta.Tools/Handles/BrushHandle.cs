using System;
using Cairo;
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
    private readonly BaseBrushTool tool;
    private readonly IWorkspaceService workspace;

    public BrushHandle (IWorkspaceService workspace, BaseBrushTool tool)
    {
        this.workspace = workspace;
        this.tool = tool;
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
        int halfOfShapeWidth = clampedWidth / 2;
        int twiceShapeWidth = clampedWidth * 2;

        RectangleD shapeRect = new () {
            X = halfOfShapeWidth,
            Y = halfOfShapeWidth,
            Width = clampedWidth,
            Height = clampedWidth
        };

        var bounds = new Graphene.Rect ();
        bounds.Init ((float) (CanvasPosition.X * zoom) - clampedWidth, (float) (CanvasPosition.Y * zoom) - clampedWidth, twiceShapeWidth, twiceShapeWidth);

        ImageSurface imageSurface = CairoExtensions.CreateImageSurface (
            Format.Argb32,
            (int) bounds.GetWidth (),
            (int) bounds.GetHeight ()
        );

        Color outerColor = new (255, 255, 255, 0.75);
        Color innerColor = new (0, 0, 0);

        using Context g = new Context (imageSurface);
        g.DrawEllipse (shapeRect, outerColor, 2);
        shapeRect = shapeRect.Inflated (-1, -1);
        g.DrawEllipse (shapeRect, innerColor, 1);

        snapshot.AppendTexture (Gdk.Texture.NewForPixbuf (imageSurface.ToPixbuf ()), bounds);
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