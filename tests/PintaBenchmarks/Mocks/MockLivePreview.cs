using Pinta.Core;

namespace PintaBenchmarks;

internal sealed class MockLivePreview : ILivePreview
{
	public RectangleI RenderBounds { get; }
	internal MockLivePreview (RectangleI renderBounds)
	{
		RenderBounds = renderBounds;
	}
}
