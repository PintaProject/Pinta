using System.Collections.Immutable;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public class GradientHandle : IToolHandle
{
	private readonly IWorkspaceService workspace;
	private readonly ImmutableArray<MoveHandle> handles;

	private PointD start_position;
	private PointD end_position;
	private MoveHandle? active_handle;
	private MoveHandle? hover_handle;

	public bool Active { get; set; }
	public bool IsDragging => active_handle is not null;

	private MoveHandle StartHandle => handles[0];
	private MoveHandle EndHandle => handles[1];
	public PointD StartPosition => start_position;
	public PointD EndPosition => end_position;

	public GradientHandle (IWorkspaceService workspace)
	{
		this.workspace = workspace;
		handles = [new MoveHandle (workspace), new MoveHandle (workspace)];
	}

	public GradientHandle PartialClone ()
	{
		//Copies the necessary states for undo
		GradientHandle clone = new (workspace);
		clone.start_position = this.start_position;
		clone.end_position = this.end_position;
		clone.Active = this.Active;

		clone.UpdateHandles ();

		return clone;
	}

	public void Draw (Snapshot snapshot)
	{
		foreach (MoveHandle handle in handles) {
			handle.Selected = (handle == hover_handle);
			handle.Draw (snapshot);
		}
	}

	public bool BeginDrag (PointD canvasPos)
	{
		if (!Active)
			return false;

		PointD viewPos = workspace.CanvasPointToView (canvasPos);
		active_handle = handles.FirstOrDefault (c => c.ContainsPoint (viewPos));
		return active_handle is not null;
	}

	public bool UpdateHoverHandle (PointD canvasPos, out RectangleI dirty)
	{
		if (!Active) {
			dirty = RectangleI.Zero;
			return false;
		}

		dirty = ComputeInvalidateRect ();
		PointD viewPos = workspace.CanvasPointToView (canvasPos);
		hover_handle = handles.FirstOrDefault (c => c.ContainsPoint (viewPos));
		dirty.Union (ComputeInvalidateRect ());
		return hover_handle is not null;
	}

	public RectangleI StartNewGradient (PointD canvasPos)
	{
		start_position = canvasPos;
		end_position = canvasPos;

		active_handle = EndHandle;
		hover_handle = EndHandle;
		Active = true;

		return UpdateHandles ();
	}

	private RectangleI UpdateHandles ()
	{
		RectangleI dirty = ComputeInvalidateRect ();

		StartHandle.CanvasPosition = start_position;
		EndHandle.CanvasPosition = end_position;

		dirty.Union (ComputeInvalidateRect ());
		return dirty;
	}

	public RectangleI Drag (PointD canvasPos)
	{
		if (active_handle == StartHandle) {
			start_position = canvasPos;
		} else {
			end_position = canvasPos;
		}
		return UpdateHandles ();

	}

	public void EndDrag ()
	{
		active_handle = null;
	}

	private RectangleI ComputeInvalidateRect ()
		=> MoveHandle.UnionInvalidateRects (handles);

}
