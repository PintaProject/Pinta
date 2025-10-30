using Cairo;
using Pinta.Core;

namespace Pinta;

public enum BackgroundType
{
	White,
	Transparent,
	SecondaryColor,
}

public readonly record struct NewImageDialogOptions (Size Size, BackgroundType Background, bool UsingClipboard);
public readonly record struct NewImageOptions (Size NewImageSize, Color NewImageBackgroundColor, BackgroundType NewImageBackgroundType);
public readonly record struct ResizeImageOptions (Size NewSize, ResamplingMode ResamplingMode);
public readonly record struct ResizeCanvasOptions (Size NewSize, Anchor Anchor, CompoundHistoryItem? CompoundAction);
