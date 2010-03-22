/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) Rick Brewster, Tom Jackson, and past contributors.            //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;

namespace Pinta.Core
{
    /// <summary>
    /// Provides a standard interface for allowing an object to draw itself on to a given Surface.
    /// </summary>
    public interface ISurfaceDraw
    {
        /// <summary>
        /// Draws the object on to the given Surface.
        /// </summary>
        /// <param name="dst">The Surface to draw to.</param>
        void Draw(ImageSurface dst);

        /// <summary>
        /// Draws the object on to the given Surface after passing each pixel through
        /// the given pixel operation as in: dst = pixelOp(dst, src)
        /// </summary>
        /// <param name="dst">The Surface to draw to.</param>
        /// <param name="pixelOp">The pixelOp to use for rendering.</param>
        void Draw(ImageSurface dst, PixelOp pixelOp);

        /// <summary>
        /// Draws the object on to the given Surface starting at the given (x,y) offset.
        /// </summary>
        /// <param name="g">The Surface to draw to.</param>
        /// <param name="transformX">The value to be added to every X coordinate that is used for drawing.</param>
        /// <param name="transformY">The value to be added to every Y coordinate that is used for drawing.</param>
        void Draw(ImageSurface dst, int tX, int tY);


        /// <summary>
        /// Draws the object on to the given Surface starting at the given (x,y) offset after
        /// passing each pixel through the given pixel operation as in: dst = pixelOp(dst, src)
        /// </summary>
        /// <param name="g">The Surface to draw to.</param>
        /// <param name="transformX">The value to be added to every X coordinate that is used for drawing.</param>
        /// <param name="transformY">The value to be added to every Y coordinate that is used for drawing.</param>
        void Draw(ImageSurface dst, int tX, int tY, PixelOp pixelOp);
    }
}
