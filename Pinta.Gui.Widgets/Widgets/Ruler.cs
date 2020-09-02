//
// Ruler.cs
//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2020 Cameron White
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
using Gtk;

namespace Pinta.Gui.Widgets
{
    public enum MetricType
    {
        Pixels,
        Inches,
        Centimeters
    }

    /// <summary>
    /// Replacement for Gtk.Ruler, which was removed in GTK3.
    /// TODO-GTK3 - implement this.
    /// </summary>
    public class Ruler : DrawingArea
    {
        /// <summary>
        /// Whether the ruler is horizontal or vertical.
        /// </summary>
        public Orientation Orientation { get; private set; }

        /// <summary>
        /// Metric type used for the ruler.
        /// </summary>
        public MetricType Metric { get; set; } = MetricType.Pixels;

        /// <summary>
        /// The position of the mark along the ruler.
        /// </summary>
        public double Position { get; set; } = 0;

        public Ruler(Orientation orientation)
        {
            Orientation = orientation;
        }

        public void SetRange(double lower, double upper, double position, double max_size)
        {
            // TODO
        }
    }
}
