using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects.Tests;

internal sealed class MockLivePreview : ILivePreview
{
	public RectangleI RenderBounds { get; }

	public bool IsEnabled
		=> throw new NotImplementedException ();

	public ImageSurface LivePreviewSurface
		=> throw new NotImplementedException ();

	internal MockLivePreview (RectangleI renderBounds)
	{
		RenderBounds = renderBounds;
	}
}
