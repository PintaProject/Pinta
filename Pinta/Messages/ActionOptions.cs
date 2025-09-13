using Pinta.Core;

namespace Pinta;

public readonly record struct ResizeImageOptions (Size NewSize, ResamplingMode ResamplingMode);
public readonly record struct ResizeCanvasOptions (Size NewSize, Anchor Anchor, CompoundHistoryItem? CompoundAction);
