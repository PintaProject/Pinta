using System;
using Cairo;
using Gtk;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class AlignObjectEffect : BaseEffect
{
	public override string Icon => Pinta.Resources.Icons.EffectsAlignObject;

	public override string Name => Translations.GetString ("Align Object");

	public override bool IsConfigurable => true;

	public override bool IsTileable => false;

	public AlignObjectData Data => (AlignObjectData) EffectData!; // NRT - Set in constructor

	private readonly IChromeService chrome;

	public AlignObjectEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		EffectData = new AlignObjectData ();
	}
	public override void LaunchConfiguration ()
	{
		var dialog = new AlignmentDialog (chrome);

		// Align to the default position
		Data.Position = dialog.SelectedPosition;

		dialog.PositionChanged += (sender, e) => {
			Data.Position = dialog.SelectedPosition;
		};

		dialog.OnResponse += (_, args) => {
			OnConfigDialogResponse (args.ResponseId == (int) Gtk.ResponseType.Ok);
			dialog.Destroy ();
		};

		dialog.Present ();
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

	private PointI CalculateNewPosition (RectangleI objectBounds, AlignPosition align, RectangleI selectionBounds)
	{
		int x = 0;
		int y = 0;

		// Align with the selection bounds
		switch (align) {
			case AlignPosition.TopLeft:
				x = selectionBounds.X;
				y = selectionBounds.Y;
				break;
			case AlignPosition.TopCenter:
				x = selectionBounds.X + selectionBounds.Width / 2 - objectBounds.Width / 2;
				y = selectionBounds.Y;
				break;
			case AlignPosition.TopRight:
				x = selectionBounds.Right - objectBounds.Width;
				y = selectionBounds.Y;
				break;
			case AlignPosition.CenterLeft:
				x = selectionBounds.X;
				y = selectionBounds.Y + selectionBounds.Height / 2 - objectBounds.Height / 2;
				break;
			case AlignPosition.Center:
				x = selectionBounds.X + selectionBounds.Width / 2 - objectBounds.Width / 2;
				y = selectionBounds.Y + selectionBounds.Height / 2 - objectBounds.Height / 2;
				break;
			case AlignPosition.CenterRight:
				x = selectionBounds.Right - objectBounds.Width;
				y = selectionBounds.Y + selectionBounds.Height / 2 - objectBounds.Height / 2;
				break;
			case AlignPosition.BottomLeft:
				x = selectionBounds.X;
				y = selectionBounds.Bottom - objectBounds.Height;
				break;
			case AlignPosition.BottomCenter:
				x = selectionBounds.X + selectionBounds.Width / 2 - objectBounds.Width / 2;
				y = selectionBounds.Bottom - objectBounds.Height;
				break;
			case AlignPosition.BottomRight:
				x = selectionBounds.Right - objectBounds.Width;
				y = selectionBounds.Bottom - objectBounds.Height;
				break;
		}

		return new PointI (x, y);
	}

	private void MoveObject (ImageSurface src, ImageSurface dest, RectangleI objectBounds, PointI newPosition, RectangleI selectionBounds)
	{
		var src_data = src.GetReadOnlyPixelData ();
		var dst_data = dest.GetPixelData ();
		int width = src.Width;

		// Clear the selection area
		for (int y = 0; y < selectionBounds.Height; y++) {
			var dst_row = dst_data.Slice ((selectionBounds.Y + y) * width + selectionBounds.X, selectionBounds.Width);

			for (int i = 0; i < dst_row.Length; i++) {
				dst_row[i] = src.GetColorBgra (PointI.Zero);
			}
		}

		// Draw the object in the new position
		for (int y = 0; y < objectBounds.Height; y++) {
			var src_row = src_data.Slice ((objectBounds.Y + y) * width + objectBounds.X, objectBounds.Width);
			var dst_row = dst_data.Slice ((newPosition.Y + y) * width + newPosition.X, objectBounds.Width);

			for (int i = 0; i < src_row.Length; i++) {
				dst_row[i] = src_row[i];
			}
		}
	}

	public sealed class AlignObjectData : EffectData
	{
		private AlignPosition position = AlignPosition.Center;

		[Caption ("Position")]
		public AlignPosition Position {
			get => position;
			set {
				if (value != position) {
					position = value;
					FirePropertyChanged (nameof (Position));
				}
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
	BottomRight
}
