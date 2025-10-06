using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public class GradientHistoryItem : BaseHistoryItem
{
	private readonly GradientTool tool;

	private readonly SimpleHistoryItem surface;

	private GradientData gradient_data;

	public GradientHistoryItem (string icon, string text, ImageSurface passedUserSurface, int layerIndex, GradientData passedData, GradientTool passedTool) : base (icon, text)
	{
		tool = passedTool;

		gradient_data = passedData;

		surface = new SimpleHistoryItem (string.Empty, string.Empty, passedUserSurface, layerIndex);
	}


	public override void Undo ()
	{
		surface.Undo ();
		Swap ();
		PintaCore.Tools.SetCurrentTool (tool);
	}

	public override void Redo ()
	{
		surface.Redo ();
		Swap ();
	}

	private void Swap ()
	{
		SwapData (ref gradient_data, tool);
	}

	private static void SwapData (ref GradientData new_data, GradientTool tool)
	{
		LineHandle handle = tool.handle;

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
