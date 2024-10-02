using System;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class AlignObjectEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsAlignObject;

	public override string Name => Translations.GetString ("Align Object");

	public override string EffectMenuCategory => Translations.GetString ("Object");

	public override bool IsConfigurable => true;

	public override bool IsTileable => false;

	public AlignObjectData Data => (AlignObjectData) EffectData!; // NRT - Set in constructor

	private readonly IChromeService chrome;

	public AlignObjectEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new AlignObjectData ();
	}
	public override Task<Gtk.ResponseType> LaunchConfiguration ()
	{
		TaskCompletionSource<Gtk.ResponseType> completionSource = new ();

		AlignmentDialog dialog = new (chrome);

		// Align to the default position
		Data.Position = dialog.SelectedPosition;

		dialog.PositionChanged += (_, _) => Data.Position = dialog.SelectedPosition;

		dialog.OnResponse += (_, args) => {
			completionSource.SetResult ((Gtk.ResponseType) args.ResponseId);
			dialog.Destroy ();
		};

		dialog.Present ();

		return completionSource.Task;
	}

	public override void Render (ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
	{
		// If no selection, it's the whole image
		RectangleI selection = rois[0];
		AlignPosition align = Data.Position;

		RectangleI objectBounds = Utility.GetObjectBounds (src, selection);

		// Calculate the new position for the object
		PointI newPosition = CalculateNewPosition (objectBounds, align, selection);

		// Draw the object in the new position
		MoveObject (src, dest, objectBounds, newPosition, selection);
	}

	private static PointI CalculateNewPosition (
		RectangleI objectBounds,
		AlignPosition align,
		RectangleI selectionBounds)
	{
		return align switch {
			AlignPosition.TopLeft => new (
				selectionBounds.X,
				selectionBounds.Y),
			AlignPosition.TopCenter => new (
				selectionBounds.X + selectionBounds.Width / 2 - objectBounds.Width / 2,
				selectionBounds.Y),
			AlignPosition.TopRight => new (
				selectionBounds.Right - objectBounds.Width,
				selectionBounds.Y),
			AlignPosition.CenterLeft => new (
				selectionBounds.X,
				selectionBounds.Y + selectionBounds.Height / 2 - objectBounds.Height / 2),
			AlignPosition.Center => new (
				selectionBounds.X + selectionBounds.Width / 2 - objectBounds.Width / 2,
				selectionBounds.Y + selectionBounds.Height / 2 - objectBounds.Height / 2),
			AlignPosition.CenterRight => new (
				selectionBounds.Right - objectBounds.Width,
				selectionBounds.Y + selectionBounds.Height / 2 - objectBounds.Height / 2),
			AlignPosition.BottomLeft => new (
				selectionBounds.X,
				selectionBounds.Bottom - objectBounds.Height),
			AlignPosition.BottomCenter => new (
				selectionBounds.X + selectionBounds.Width / 2 - objectBounds.Width / 2,
				selectionBounds.Bottom - objectBounds.Height),
			AlignPosition.BottomRight => new (
				selectionBounds.Right - objectBounds.Width,
				selectionBounds.Bottom - objectBounds.Height),
			_ => PointI.Zero,
		};
	}

	private static void MoveObject (
		ImageSurface src,
		ImageSurface dest,
		RectangleI objectBounds,
		PointI newPosition,
		RectangleI selectionBounds)
	{
		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dest.GetPixelData ();
		int width = src.Width;

		// Clear the selection area
		var backgroundColor = src.GetColorBgra (new PointI (selectionBounds.Left, selectionBounds.Top));
		for (int y = 0; y < selectionBounds.Height; y++) {
			var dst_row = dst_data.Slice ((selectionBounds.Y + y) * width + selectionBounds.X, selectionBounds.Width);
			dst_row.Fill (backgroundColor);
		}

		// Draw the object in the new position
		for (int y = 0; y < objectBounds.Height; y++) {
			var src_row = src_data.Slice ((objectBounds.Y + y) * width + objectBounds.X, objectBounds.Width);
			var dst_row = dst_data.Slice ((newPosition.Y + y) * width + newPosition.X, objectBounds.Width);
			src_row.CopyTo (dst_row);
		}
	}

	public sealed class AlignObjectData : EffectData
	{
		private AlignPosition position = AlignPosition.Center;

		[Caption ("Position")]
		public AlignPosition Position {
			get => position;
			set {
				if (value == position) return;
				position = value;
				FirePropertyChanged (nameof (Position));
			}
		}

		[Skip]
		public override bool IsDefault => Position == AlignPosition.Center;
	}
}

public enum AlignPosition
{
	TopLeft,
	TopCenter,
	TopRight,
	CenterLeft,
	Center,
	CenterRight,
	BottomLeft,
	BottomCenter,
	BottomRight,
}
