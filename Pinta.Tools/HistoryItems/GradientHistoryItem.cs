using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public class GradientHistoryItem : BaseHistoryItem
{
	private readonly GradientTool tool;

	private readonly UserLayer user_layer;

	private readonly SurfaceDiff? user_surface_diff;
	private ImageSurface? user_surface;

	private GradientHandle handle;

	public GradientHistoryItem (string icon, string text, ImageSurface passedUserSurface, UserLayer passedUserLayer, GradientHandle passedHandle, GradientTool passedTool) : base (icon, text)
	{
		tool = passedTool;
		handle = passedHandle;

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

		Swap (ref handle, ref tool.handle);
	}
}
