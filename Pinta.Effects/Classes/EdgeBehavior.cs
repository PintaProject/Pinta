using Pinta.Gui.Widgets;

namespace Pinta.Effects;

public enum EdgeBehavior
{
	[Caption ("Clamp")]
	Clamp,

	[Caption ("Wrap")]
	Wrap,

	[Caption ("Reflect")]
	Reflect,

	[Caption ("Primary")]
	Primary,

	[Caption ("Secondary")]
	Secondary,

	[Caption ("Transparent")]
	Transparent,

	[Caption ("Original")]
	Original,
}
