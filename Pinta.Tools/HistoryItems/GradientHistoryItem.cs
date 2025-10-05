using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public class GradientHistoryItem : BaseHistoryItem
{
	private readonly GradientTool tool;

	private readonly UserLayer user_layer;

	private readonly SurfaceDiff? user_surface_diff;
	private ImageSurface? user_surface;

	private GradientData gradient_data;

	public GradientHistoryItem (string icon, string text, ImageSurface passedUserSurface, UserLayer passedUserLayer, GradientData passedData, GradientTool passedTool) : base (icon, text)
	{
		tool = passedTool;

		gradient_data = passedData;

		user_layer = passedUserLayer;
		user_surface_diff = SurfaceDiff.Create (passedUserSurface, user_layer.Surface, true);
		if (user_surface_diff == null) {
			user_surface = passedUserSurface;
		}
	}



	public override void Undo ()
	{
		Swap ();
		PintaCore.Tools.SetCurrentTool (tool);
	}

	public override void Redo ()
	{
		Swap ();
	}

	private void Swap ()
	{
		ImageSurface surf = user_layer.Surface;

		if (user_surface_diff != null) {
			user_surface_diff.ApplyAndSwap (surf);

			PintaCore.Workspace.Invalidate (user_surface_diff.GetBounds ());
		} else {

			user_layer.Surface = user_surface!;

			user_surface = surf;

			PintaCore.Workspace.Invalidate ();
		}

		SwapData (ref gradient_data, tool);
	}

	private static void SwapData (ref GradientData new_data, GradientTool tool)
	{
		GradientHandle handle = tool.handle;

		GradientData old_data = new GradientData (
			handle.StartPosition,
			handle.EndPosition,
			handle.Active,
			tool.color_button
		);

		tool.color_button = new_data.ColorButton;
		handle.ApplyData (
			new_data.StartPosition,
			new_data.EndPosition,
			new_data.Active
			);

		new_data = old_data;
	}
}

// It's a class instead of a struct so that GradientTool can have a null ref when tool hasn't been used yet and there's no undo data
public class GradientData
{
	public PointD StartPosition { get; set; }
	public PointD EndPosition { get; set; }
	public bool Active { get; set; }
	public MouseButton ColorButton { get; set; }

	public GradientData (PointD startPosition, PointD endPosition, bool active, MouseButton colorButton)
	{
		this.StartPosition = startPosition;
		this.EndPosition = endPosition;
		this.Active = active;
		this.ColorButton = colorButton;
	}

}
