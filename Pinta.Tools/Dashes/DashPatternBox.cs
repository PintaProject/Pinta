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
	public class DashPatternBox
	{
		private bool dashChangeSetup = false;

		//The number to multiply each dash and space character by when generating the dash pattern double[].
		//This makes it easier to set the standard size of a single dash or space.
		private static double dashFactor = 1.0;

		private ToolBarLabel dashPatternLabel;
		private ToolItem dashPatternSep;

		public ToolBarComboBox comboBox;



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

			if (comboBox == null)
			{
				comboBox = new ToolBarComboBox(100, 0, true,
					"-", " -", " --", " ---", "  -", "   -", " - --", " - - --------", " - - ---- - ----");
			}

			tb.AppendItem(comboBox);

			if (dashChangeSetup)
			{
				return null;
			}
			else
			{
				dashChangeSetup = true;

				return comboBox.ComboBox;
			}
		}



		/// <summary>
		/// Generates a double[] given a string pattern that consists of any combination of dashes and spaces.
		/// </summary>
		/// <param name="dashPattern">The dash pattern string.</param>
		/// <param name="brush_width">The width of the brush.</param>
		/// <returns>The double[] generated.</returns>
		public static double[] GenerateDashArray(string dashPattern, double brushWidth)
		{
			List<double> dashList = new List<double>();

			//For each consecutive dash character, extent will increase by 1.
			//For each consecutive space character, extent will dicrease by 1.
			int extent = 0;

			/* The expected input for the dash pattern string is any combination of dashes and spaces; however, every character is allowed to
			 * be entered. Only dash characters will count as dashes, and any non-dash character (including spaces, 'a', '$', '=', or etc.) will
			 * be counted as if it were a space, so to speak. So, "-- - --- -" is considered the same as "--b-$---=-".
			 * 
			 * This code goes through the string, character by character, counting up the number of consecutive dashes and spaces. Whenever a
			 * series of one or more dashes or spaces (exclusively) is followed by the opposing type (e.g. "----" is then followed by ' '),
			 * the extent as to how far the series went (how many consecutive characters were met; in this case, 4) is then added to the
			 * resulting dashList.
			 * 
			 * To understand this code, it is necessary to first understand how Cairo's dashing system works. I myself have only bothered to
			 * understand it (as it can become slightly difficult) only to the extent as to which is necessary to be able to systematically
			 * derive the resembling dash pattern array from the given string. Here are the rules that I have come up with that work:
			 * 
			 *     1. Alternate the number of consecutive dashes and spaces. "---  - " would result in { 3.0, 2.0, 1.0, 1.0 }.
			 *     
			 *     2. Every pattern must start with a dash representation and end with a space representation. If the pattern started
			 *        with a space and/or ended with a dash, use 0.0 as a placeholder (the result of a 0.0 will not be directly visual).
			 *        " ---- --" would thus result in { 0.0, 1.0, 4.0, 1.0, 2.0, 0.0 }. This order AND ending is mandatory; I don't
			 *        understand why, but I do know that this way it works perfectly well and that it wasn't working perfectly otherwise.
			 * 
			 * Note: "extent" is only ever 0 at the very beginning; otherwise, it will always be > 0 or < 0. After the foreach loop, "extent" will
			 * never be equal to 0, and the final series must be added onto the dash pattern array outside of the loop, thus tying off the loose end. */
			foreach (char c in dashPattern)
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
						//There were previously one or more non-dash characters.
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
						//There were previously one or more dash characters.
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
