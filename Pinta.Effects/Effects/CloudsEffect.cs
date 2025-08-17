/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects;

public sealed class CloudsEffect : BaseEffect
{
	private static readonly object render_lock = new ();

	public sealed override bool IsTileable
		=> true;

	public override string Icon
		=> Resources.Icons.EffectsRenderClouds;

	public override string Name
		=> Translations.GetString ("Clouds");

	public override bool IsConfigurable
		=> true;

	public override string EffectMenuCategory
		=> Translations.GetString ("Render");

	public CloudsData Data
		=> (CloudsData) EffectData!;  // NRT - Set in constructor

	private readonly IPaletteService palette;
	private readonly IWorkspaceService workspace;
	private readonly IChromeService chrome;
	public CloudsEffect (IServiceProvider services)
	{
		chrome = services.GetService<IChromeService> ();
		palette = services.GetService<IPaletteService> ();
		workspace = services.GetService<IWorkspaceService> ();
		EffectData = new CloudsData ();
	}

	public override Task<bool> LaunchConfiguration ()
		=> chrome.LaunchSimpleEffectDialog (this, workspace);

	// Algorithm Code Ported From PDN

	private static void RenderClouds (
		ImageSurface surface,
		RectangleI rect,
		int scale,
		byte seed,
		double power,
		ColorGradient<ColorBgra> gradient)
	{
		int w = surface.Width;
		int h = surface.Height;
		int bottom = rect.Bottom;

		var data = surface.GetPixelData ();

		for (int y = rect.Top; y <= bottom; ++y) {
			var row = data.Slice ((y - rect.Top) * w, w);
			int dy = 2 * y - h;
			for (int x = rect.Left; x <= rect.Right; ++x)
				row[x - rect.Left] = GetFinalPixelColor (scale, seed, power, gradient, w, dy, x); ;
		}
	}

	private static ColorBgra GetFinalPixelColor (
		int scale,
		byte seed,
		double power,
		ColorGradient<ColorBgra> gradient,
		int w,
		int dy,
		int x)
	{
		int dx = 2 * x - w;
		double val = 0;
		double mult = 1;
		int div = scale;

		for (int i = 0; i < 12 && mult > 0.03 && div > 0; ++i) {

			PointD dr = new (
				X: 65536 + dx / (double) div,
				Y: 65536 + dy / (double) div);

			PointI dd = new (
				X: (int) dr.X,
				Y: (int) dr.Y);

			dr = new (
				X: dr.X - dd.X,
				Y: dr.Y - dd.Y);

			double noise = PerlinNoise.Compute (
				unchecked((byte) dd.X),
				unchecked((byte) dd.Y),
				dr, //(double)dxr / div, (double)dyr / div
				(byte) (seed ^ i)
			);

			val += noise * mult;
			div /= 2;
			mult *= power;
		}

		return gradient.GetColor ((val + 1) / 2);
	}

	protected override void Render (
		ImageSurface src,
		ImageSurface dst,
		RectangleI roi)
	{
		RectangleD r = roi.ToDouble ();

		ImageSurface temp = CairoExtensions.CreateImageSurface (Format.Argb32, roi.Width, roi.Height);

		var baseGradient =
			GradientHelper
			.CreateBaseGradientForEffect (
				palette,
				Data.ColorSchemeSource,
				Data.ColorScheme,
				Data.ColorSchemeSeed)
			.Resized (0, 1);

		RenderClouds (
			temp,
			roi,
			Data.Scale,
			unchecked((byte) Data.Seed.Value),
			Data.Power / 100.0,
			Data.ReverseColorScheme ? baseGradient.Reversed () : baseGradient
		);

		temp.MarkDirty ();

		// Have to lock because effect renderer is multithreaded
		lock (render_lock) {

			using Context g = new (dst);

			// - Clear any previous render from the destination
			// - Copy the source to the destination
			// - Blend the clouds over the source

			g.Clear (r);
			g.BlendSurface (src, r);
			g.BlendSurface (temp, r.Location (), (BlendMode) CloudsData.BlendOps[Data.BlendMode]);
		}
	}

	public sealed class CloudsData : EffectData
	{
		[Skip]
		public override bool IsDefault => Power == 0;

		[Caption ("Scale"), MinimumValue (2), MaximumValue (1000)]
		public int Scale { get; set; } = 250;

		[Caption ("Power"), MinimumValue (0), MaximumValue (100)]
		public int Power { get; set; } = 50;

		[Skip]
		public static ReadOnlyDictionary<string, object> BlendOps { get; }

		[Skip]
		private static readonly string default_blend_op;

		static CloudsData ()
		{
			BlendOps =
				UserBlendOps.GetAllBlendModeNames ()
				.ToDictionary (
					o => o,
					o => (object) UserBlendOps.GetBlendModeByName (o))
				.AsReadOnly ();

			default_blend_op = UserBlendOps.GetBlendModeName (Pinta.Core.BlendMode.Normal);
		}

		[StaticList ("BlendOps")]
		public string BlendMode { get; set; } = default_blend_op;

		[Caption ("Random Noise Seed")]
		public RandomSeed Seed { get; set; } = new (0);

		[Caption ("Color Scheme Source")]
		public ColorSchemeSource ColorSchemeSource { get; set; } = ColorSchemeSource.SelectedColors;

		[Caption ("Color Scheme")]
		public PresetGradients ColorScheme { get; set; } = PresetGradients.BeautifulItaly;

		[Caption ("Random Color Scheme Seed")]
		public RandomSeed ColorSchemeSeed { get; set; } = new (0);

		[Caption ("Reverse Color Scheme")]
		public bool ReverseColorScheme { get; set; } = false;
	}
}
