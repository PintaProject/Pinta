//
// LevelsEffect.cs
//  
// Author:
//       Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using Cairo;

namespace Pinta.Core
{
	public class LevelsEffect : BaseEffect
	{		
		public override string Icon {
			get { return "Menu.Adjustments.Levels.png"; }
		}

		public override string Text {
			get { return Mono.Unix.Catalog.GetString ("Levels"); }
		}

		public override bool IsConfigurable {
			get { return true; }
		}
		
		public LevelsData Data { get { return EffectData as LevelsData; } }
		
		public LevelsEffect ()
		{
			EffectData = new LevelsData ();
		}
		
		public override bool LaunchConfiguration ()
		{			
			var dialog = new LevelsDialog (Data);
			dialog.Title = Text;
			dialog.Icon = PintaCore.Resources.GetIcon (Icon);
			
			int response = dialog.Run ();

			dialog.Destroy ();
			
			return (response == (int)Gtk.ResponseType.Ok);
		}
		
		public override void RenderEffect (ImageSurface src, ImageSurface dest, Gdk.Rectangle[] rois)
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
