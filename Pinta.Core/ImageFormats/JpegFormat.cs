// 
// JpegFormat.cs
//  
// Author:
//       Maia Kozheva <sikon@ubuntu.com>
// 
// Copyright (c) 2010 Maia Kozheva <sikon@ubuntu.com>
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
using System.IO;

using Gdk;

namespace Pinta.Core
{
	public class JpegFormat: GdkPixbufFormat
	{
		//This is used to remember the previous JPG export compression quality value. It must be
		//a number between 1-100. It starts out at 101 so Pinta knows it hasn't been loaded yet.
		private static int jpgCompression = 101;

		public JpegFormat()
			: base ("jpeg")
		{
			//Save the JPG export compression quality to the settings file before Pinta closes.
			PintaCore.Actions.File.BeforeQuit += new EventHandler(SaveJPGCompression);
		}

		void SaveJPGCompression(object sender, EventArgs e)
		{
			//Save the JPG export compression quality to the settings file before Pinta closes.
			PintaCore.Settings.PutSetting("jpg-quality", jpgCompression);
		}

		protected override void DoSave(Pixbuf pb, string fileName, string fileType)
		{
			//If the JPG export compression quality is equal to 101,
			//it hasn't been loaded from the settings file yet.
			if (jpgCompression == 101)
			{
				//Load the JPG export compression quality, with a default of 85.
				jpgCompression = PintaCore.Settings.GetSetting<int>("jpg-quality", 85);
			}

			int level = jpgCompression;

			//Check to see if the document has been saved before. If it has,
			//ask the user for the JPG compression quality to export with.
			if (!PintaCore.Workspace.ActiveDocument.HasBeenSaved)
			{
				//Show the user the JPG compression export quality.
				level = PintaCore.Actions.File.RaiseModifyCompression(jpgCompression);
			}

			if (level != -1)
			{
				//Store the "previous" JPG export compression quality value (before saving with it).
				jpgCompression = level;

				//Save the file.
				pb.Savev(fileName, fileType, new string[] { "quality", null }, new string[] { level.ToString(), null });
			}
		}
	}
}
