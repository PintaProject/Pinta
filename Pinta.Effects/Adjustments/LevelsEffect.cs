/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using Pinta.Core;

namespace Pinta.Effects
{
	public class LevelsEffect : BaseEffect
	{		
		public override string Icon {
			get { return "Menu.Adjustments.Levels.png"; }
		}

		public override string Name {
			get { return Mono.Unix.Catalog.GetString ("Levels"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override Gdk.Key AdjustmentMenuKey {
			get { return Gdk.Key.L; }
		}

		public override Gdk.ModifierType AdjustmentMenuKeyModifiers {
			get { return Gdk.ModifierType.ControlMask; }
		}
		
		public LevelsData Data { get { return EffectData as LevelsData; } }
		
		public LevelsEffect ()
		{
			EffectData = new LevelsData ();
		}
		
		public override bool LaunchConfiguration ()
		{			
			var dialog = new LevelsDialog (Data);
			dialog.Title = Name;
			dialog.Icon = PintaCore.Resources.GetIcon (Icon);
			
			int response = dialog.Run ();

			dialog.Destroy ();
			
			return (response == (int)Gtk.ResponseType.Ok);
		}
		
		public override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
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
