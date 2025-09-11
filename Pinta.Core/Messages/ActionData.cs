namespace Pinta.Core;

public readonly record struct ImageResizing (Size NewSize, ResamplingMode ResamplingMode);
public readonly record struct CanvasResizing (Size NewSize, Anchor Anchor, CompoundHistoryItem? CompoundAction);
