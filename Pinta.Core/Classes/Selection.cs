// 
// Selection.cs
//  
// Author:
//       Volodymyr <${AuthorEmail}>
// 
// Copyright (c) 2012 Volodymyr
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
	public class Selection : IDisposable, ICloneable
	{
		#region Members
		private Path m_path;
		#endregion

		#region Properties
		/// <summary>
		/// Gets the path.
		/// </summary>
		/// <value>
		/// The path.
		/// </value>
		public Path Path
		{
			get
			{
				return m_path;
			}
			set
			{
				m_path = value;
			}
		}
		#endregion

		#region Constroctors
		/// <summary>
		/// Initializes a new instance of the <see cref="Pinta.Core.Selection"/> class.
		/// </summary>
		public Selection ()
		{
		}
		#endregion

		#region Public methods
		/// <summary>
		/// Clip the specified context.
		/// </summary>
		/// <param name='g'>
		/// Cairo context
		/// </param>
		public void Clip(Context g)
		{
			g.AppendPath (m_path);
			g.FillRule = Cairo.FillRule.EvenOdd;
			g.Clip ();
		}
		/// <summary>
		/// Draw the specified g and fillSelection.
		/// </summary>
		/// <param name='g'>
		/// Context
		/// </param>
		/// <param name='fillSelection'>
		/// Fill selection.
		/// </param>
		public void Draw(Context g, double scale, bool fillSelection)
		{
			g.Save ();
			g.Translate (0.5, 0.5);
			g.Scale (scale, scale);

			g.AppendPath (m_path);

			if (fillSelection)
			{
				g.Color = new Cairo.Color (0.7, 0.8, 0.9, 0.2);
				g.FillRule = Cairo.FillRule.EvenOdd;
				g.FillPreserve ();
			}

			g.LineWidth = 1 / scale;

			// Draw a white line first so it shows up on dark backgrounds
			g.Color = new Cairo.Color (1, 1, 1);
			g.StrokePreserve ();

			// Draw a black dashed line over the white line
			g.SetDash (new double[] { 2 / scale, 4 / scale }, 0);
			g.Color = new Cairo.Color (0, 0, 0);

			g.Stroke ();
			g.Restore ();
		}
		/// <summary>
		/// Releases all resource used by the <see cref="Pinta.Core.Selection"/> object.
		/// </summary>
		/// <remarks>
		/// Call <see cref="Dispose"/> when you are finished using the <see cref="Pinta.Core.Selection"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Pinta.Core.Selection"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="Pinta.Core.Selection"/> so the garbage
		/// collector can reclaim the memory that the <see cref="Pinta.Core.Selection"/> was occupying.
		/// </remarks>
		public void Dispose()
		{
			(m_path as IDisposable).Dispose();
		}
		/// <summary>
		/// Clone this instance.
		/// </summary>
		public Selection Clone()
		{
			Selection sel = new Selection();

			sel.m_path = this.m_path.Clone();

			return sel;
		}
		#endregion

		#region Implementation
		/// <summary>
		/// Clone this instance.
		/// </summary>
		object ICloneable.Clone()
		{
			return this.Clone();
		}
		#endregion
	}
}

