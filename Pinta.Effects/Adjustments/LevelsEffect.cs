/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using Cairo;
using Pinta.Core;

namespace Pinta.Effects
{
	public class LevelsEffect : BaseEffect
	{
		public override string Icon => Pinta.Resources.Icons.AdjustmentsLevels;

		public override string Name => Translations.GetString ("Levels");

		public override bool IsConfigurable => true;

		public override string AdjustmentMenuKey => "L";

		public override string AdjustmentMenuKeyModifiers => "<Primary>";

		public LevelsData Data => (LevelsData) EffectData!;  // NRT - Set in constructor

		public LevelsEffect ()
		{
			EffectData = new LevelsData ();
		}

		public override void LaunchConfiguration ()
		{
			var dialog = new LevelsDialog (Data) {
				Title = Name,
				IconName = Icon,
			};

			dialog.OnResponse += (_, args) => {
				if (args.ResponseId != (int) Gtk.ResponseType.None) {
					OnConfigDialogResponse (args.ResponseId == (int) Gtk.ResponseType.Ok);
					dialog.Destroy ();
				}
			};

			dialog.Present ();
		}

		public override void Render (ImageSurface src, ImageSurface dest, Core.RectangleI[] rois)
		{
			Data.Levels.Apply (dest, src, rois);
		}
	}

	public class LevelsData : EffectData
	{
		public UnaryPixelOps.Level Levels { get; set; }

		public LevelsData ()
		{
			Levels = new UnaryPixelOps.Level ();
		}

		public override EffectData Clone ()
		{
			return new LevelsData () { Levels = (UnaryPixelOps.Level) Levels.Clone () };
		}
	}
}
