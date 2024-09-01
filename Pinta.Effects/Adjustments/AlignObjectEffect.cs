using System;
using Cairo;
using Pinta.Core;
using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public sealed class AlignObjectEffect : BaseEffect
{
    // TODO: Add icon
    public override string Icon => Pinta.Resources.Icons.ToolMove;

    public override string Name => Translations.GetString("Align Object");

    public override bool IsConfigurable => true;

    public override bool IsTileable => false;

    public AlignObjectData Data => (AlignObjectData)EffectData!; // NRT - Set in constructor

    private readonly IChromeService chrome;

    public AlignObjectEffect(IServiceProvider services)
    {
        chrome = services.GetService<IChromeService>();
        EffectData = new AlignObjectData();
    }

    public override void LaunchConfiguration()
        => chrome.LaunchSimpleEffectDialog(this);

    public override void Render(ImageSurface src, ImageSurface dest, ReadOnlySpan<RectangleI> rois)
    {
        RectangleI objectBounds = Utility.GetObjectBounds(src);

        AlignPosition align = Data.Position;

        // Calculate the new position for the object
        PointI newPosition = CalculateNewPosition(objectBounds, dest.GetSize (), align);

        // Draw the object in the new position
        MoveObject(src, dest, objectBounds, newPosition);
    }

    private PointI CalculateNewPosition(RectangleI objectBounds, Size destSize, AlignPosition align)
    {
        int newX = 0;
        int newY = 0;

        switch (align)
        {
            case AlignPosition.TopLeft:
                newX = 0;
                newY = 0;
                break;
            case AlignPosition.TopCenter:
                newX = (destSize.Width - objectBounds.Width) / 2;
                newY = 0;
                break;
            case AlignPosition.TopRight:
                newX = destSize.Width - objectBounds.Width;
                newY = 0;
                break;
            case AlignPosition.CenterLeft:
                newX = 0;
                newY = (destSize.Height - objectBounds.Height) / 2;
                break;
            case AlignPosition.Center:
                newX = (destSize.Width - objectBounds.Width) / 2;
                newY = (destSize.Height - objectBounds.Height) / 2;
                break;
            case AlignPosition.CenterRight:
                newX = destSize.Width - objectBounds.Width;
                newY = (destSize.Height - objectBounds.Height) / 2;
                break;
            case AlignPosition.BottomLeft:
                newX = 0;
                newY = destSize.Height - objectBounds.Height;
                break;
            case AlignPosition.BottomCenter:
                newX = (destSize.Width - objectBounds.Width) / 2;
                newY = destSize.Height - objectBounds.Height;
                break;
            case AlignPosition.BottomRight:
                newX = destSize.Width - objectBounds.Width;
                newY = destSize.Height - objectBounds.Height;
                break;
        }

        return new PointI(newX, newY);
    }

    private void MoveObject(ImageSurface src, ImageSurface dest, RectangleI objectBounds, PointI newPosition)
    {
        var src_data = src.GetReadOnlyPixelData();
        var dst_data = dest.GetPixelData();
        int width = src.Width;

        // Clear the whole destination surface
        for (int i = 0; i < dst_data.Length; i++)
        {
            dst_data[i] = src.GetColorBgra (PointI.Zero);
        }

        for (int y = 0; y < objectBounds.Height; y++)
        {
            var src_row = src_data.Slice((objectBounds.Y + y) * width + objectBounds.X, objectBounds.Width);
            var dst_row = dst_data.Slice((newPosition.Y + y) * width + newPosition.X, objectBounds.Width);

            for (int i = 0; i < src_row.Length; i++)
            {
                dst_row[i] = src_row[i];
            }
        }
    }

    public sealed class AlignObjectData : EffectData
    {
        private AlignPosition position = AlignPosition.Center;

        [Caption("Position")]
        public AlignPosition Position
        {
            get => position;
            set
            {
                if (value != position)
                {
                    position = value;
                    FirePropertyChanged(nameof(Position));
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
