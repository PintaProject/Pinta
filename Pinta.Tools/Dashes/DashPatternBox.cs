// 
// DashPatternBox.cs
// 
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2013
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	class DashPatternBox
	{
		private bool dashChangeSetup = false;

		//The number to multiply each dash and space character by when generating the dash pattern double[].
		private static double dashFactor = 1.0;

		private ToolBarLabel dashPatternLabel;
		private ToolItem dashPatternSep;

		public ToolBarComboBox DashPatternBox;



		/// <summary>
		/// Sets up the DashPatternBox in the Toolbar.
		/// 
		/// Note that the dash pattern change event response code must be created manually outside of the DashPatternBox
		/// (using the returned Gtk.ComboBox from the SetupToolbar method) so that each tool that uses it
		/// can react to the change in pattern according to its usage.
		/// 
		/// Returns null if the DashPatternBox has already been setup; otherwise, returns the DashPatternBox itself.
		/// </summary>
		/// <param name="tb">The Toolbar to add the DashPatternBox to.</param>
		/// <returns>null if the DashPatternBox has already been setup; otherwise, returns the DashPatternBox itself.</returns>
		public Gtk.ComboBox SetupToolbar(Toolbar tb)
		{
			if (dashPatternSep == null)
			{
				dashPatternSep = new SeparatorToolItem();
			}

			tb.AppendItem(dashPatternSep);

			if (dashPatternLabel == null)
			{
				dashPatternLabel = new ToolBarLabel(string.Format(" {0}: ", Catalog.GetString("Dash")));
			}

			tb.AppendItem(dashPatternLabel);

			if (DashPatternBox == null)
			{
				DashPatternBox = new ToolBarComboBox(100, 0, true,
					"-", " -", " --", " ---", "  -", "   -", " - --", " - - --------", " - - ---- - ----");
			}

			tb.AppendItem(DashPatternBox);

			if (dashChangeSetup)
			{
				return null;
			}
			else
			{
				dashChangeSetup = true;

				return DashPatternBox.ComboBox;
			}
		}



		/// <summary>
		/// Generates a double[] given a string pattern that consists of any combination of dashes and spaces.
		/// </summary>
		/// <param name="dP">The dash pattern string.</param>
		/// <param name="brushWidth">The width of the brush.</param>
		/// <returns>The double[] generated.</returns>
		public static double[] GenerateDashArray(string dP, double brushWidth)
		{
			List<double> dashList = new List<double>();

			//For each consecutive dash character, extent will increase by 1.
			//For each consecutive space character, extent will dicrease by 1.
			int extent = 0;

			//Go through each character in the given dash pattern string.
			foreach (char c in dP)
			{
				if (c == '-')
				{
					//Dash character.

					if (extent >= 0)
					{
						++extent;
					}
					else
					{
						//There was previously one or more non-dash characters.
						dashList.Add((double)-extent * brushWidth * dashFactor);

						extent = 1;
					}
				}
				else
				{
					//Non-dash character.

					if (extent == 0)
					{
						//Pattern is starting with a non-dash character. Resulting double[] pattern must end
						//with a dash representation for this to be accurate: 0.0 is merely a placeholder.
						dashList.Add(0.0);

						--extent;
					}
					else if (extent < 0)
					{
						--extent;
					}
					else
					{
						//There was previously one or more dash characters.
						dashList.Add((double)extent * brushWidth * dashFactor);

						extent = -1;
					}
				}
			}

			//At this point, extent != 0.
			if (extent > 0)
			{
				//extent > 0. Pattern ended with a dash character.
				dashList.Add((double)extent * brushWidth * dashFactor);

				//Resulting double[] pattern must end with a space representation for this to be accurate: 0.0 is merely a placeholder.
				dashList.Add(0.0);
			}
			else
			{
				//extent < 0. Pattern ended with a non-dash character.
				dashList.Add((double)-extent * brushWidth * dashFactor);
			}

			return dashList.ToArray();
		}
	}
}
