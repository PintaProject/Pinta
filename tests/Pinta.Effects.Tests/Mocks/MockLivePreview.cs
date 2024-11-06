using Pinta.Core;

namespace Pinta.Effects.Tests;

internal sealed class MockLivePreview : ILivePreview
{
	public RectangleI RenderBounds { get; }
	internal MockLivePreview (RectangleI renderBounds)
	{
		RenderBounds = renderBounds;
	}
}
