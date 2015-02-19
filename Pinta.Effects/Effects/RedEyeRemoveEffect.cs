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

using Pinta.Gui.Widgets;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Effects
{
	public class RedEyeRemoveEffect : BaseEffect
	{
		private UnaryPixelOp op;
		
		public override string Icon {
			get { return "Menu.Effects.Photo.RedEyeRemove.png"; }
		}

		public override string Name {
			get { return Catalog.GetString ("Red Eye Removal"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}

		public override string EffectMenuCategory {
			get { return Catalog.GetString ("Photo"); }
		}

		public RedEyeRemoveData Data { get { return EffectData as RedEyeRemoveData; } }
		
		public RedEyeRemoveEffect ()
		{
			EffectData = new RedEyeRemoveData ();
		}
		
		public override bool LaunchConfiguration ()
		{
			SimpleEffectDialog dialog = new SimpleEffectDialog (Name, PintaCore.Resources.GetIcon (Icon), Data,
			                                                    new PintaLocalizer ());

			// Hookup event handling for live preview.
			dialog.EffectDataChanged += (o, e) => {
				if (EffectData != null) {
					op = new UnaryPixelOps.RedEyeRemove (Data.Tolerance, Data.Saturation);
					EffectData.FirePropertyChanged (e.PropertyName);
				}
			};
			
			int response = dialog.Run ();
			bool ret = (response == (int)Gtk.ResponseType.Ok);
			dialog.Destroy ();
			
			return ret;
		}
		
		public unsafe override void Render (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
		{
			op.Apply (dest, src, rois);
		}
	}
	
	public class RedEyeRemoveData : EffectData
	{
		[Caption ("Tolerance"), MinimumValue (0), MaximumValue (100)]
		public int Tolerance = 70;
		
		[MinimumValue (0), MaximumValue (100)]
		[Caption ("Saturation Percentage")]
		[Hint ("Hint: For best results, first use selection tools to select each eye.")]
		public int Saturation = 90;
	}
}

