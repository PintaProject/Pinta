// 
// BaseTransformTool.cs
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
using Pinta.Core;

namespace Pinta.Tools
{
	public abstract class BaseTransformTool : BaseTool
	{
		#region Members
		private Matrix transform = new Matrix();
		private Matrix inverted_transform = new Matrix();
		private Matrix inverted_rt_transform = new Matrix();
		private Matrix transform_update = new Matrix();
		private Matrix translation_matrix = new Matrix();
		private Matrix rotation_matrix = new Matrix();
		private Matrix resize_matrix = new Matrix();

		private Rectangle source_rect;
		private Rectangle destination_rect;
		private PointD start_point;
		private PointD old_point;

		private TransformControlPoint[] control_points = new TransformControlPoint[8];

		private bool is_dragging = false;
		private bool is_rotating = false;
		private TransformControlPoint selected_point;
		private Gdk.Cursor cursor_hand;
		#endregion

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="Pinta.Tools.BaseTransformTool"/> class.
		/// </summary>
		public BaseTransformTool ()
		{
			control_points[0] = new TransformControlPoint(TransformEdge.TopLeft);
			control_points[1] = new TransformControlPoint(TransformEdge.Top);
			control_points[2] = new TransformControlPoint(TransformEdge.TopRight);
			control_points[3] = new TransformControlPoint(TransformEdge.Right);
			control_points[4] = new TransformControlPoint(TransformEdge.BottomRight);
			control_points[5] = new TransformControlPoint(TransformEdge.Bottom);
			control_points[6] = new TransformControlPoint(TransformEdge.BottomLeft);
			control_points[7] = new TransformControlPoint(TransformEdge.Left);

			Gdk.Pixbuf handIcon = PintaCore.Resources.GetIcon ("Tools.Pan.png");
			cursor_hand = new Gdk.Cursor (PintaCore.Chrome.Canvas.Display,
			                              handIcon, handIcon.Width / 2, handIcon.Height / 2);

		}
		#endregion

		#region Implemenation

		protected abstract Cairo.Rectangle GetSourceRectangle();

		protected override void OnActivated ()
		{
			base.OnActivated ();

			source_rect = GetSourceRectangle();
			destination_rect = source_rect;

			for (int i = 0; i < control_points.Length; i++) {
				control_points[i].SetFromRectangle(source_rect);
			}

			resize_matrix.InitIdentity();
			translation_matrix.InitIdentity();
			rotation_matrix.InitIdentity();
			inverted_transform.InitIdentity();
			inverted_rt_transform.InitIdentity();
			transform.InitIdentity();
		}

		protected virtual void OnStartTransform()
		{
		}

		protected virtual void OnUpdateTransform(Matrix transform, Matrix update)
		{
		}

		protected virtual void OnFinishTransform()
		{
			PintaCore.Workspace.Invalidate();
		}

		protected override void OnMouseDown (Gtk.DrawingArea canvas,
		                                     Gtk.ButtonPressEventArgs args,
		                                     Cairo.PointD point)
		{
			if(is_dragging || is_rotating)
				return;

			old_point = point;
			start_point = point;

			if(args.Event.Button == MOUSE_RIGHT_BUTTON)
			{
				is_rotating = true;
			}
			else
			{
				selected_point = this.FindTransformPoint(point);
				is_dragging = true;

				if(selected_point != null)
					SetCursor(cursor_hand);
			}

			OnStartTransform();
		}

		protected override void OnMouseMove (object o,
		                                     Gtk.MotionNotifyEventArgs args,
		                                     Cairo.PointD point)
		{
			if (!is_dragging && !is_rotating)
				return;

			if(is_rotating)
			{
				PointD center = destination_rect.GetCenter();
				PointD transformedCenter = source_rect.GetCenter();
				transform.TransformPoint(ref transformedCenter);

				double cx1 = old_point.X - transformedCenter.X;
				double cy1 = old_point.Y - transformedCenter.Y;

				double cx2 = point.X - transformedCenter.X;
				double cy2 = point.Y - transformedCenter.Y;

				rotation_matrix.Translate(center.X, center.Y);
				rotation_matrix.Rotate(Math.Atan2(cy2, cx2) - Math.Atan2(cy1, cx1));
				rotation_matrix.Translate(-center.X, -center.Y);
			}
			else if(selected_point != null)
			{
				double tdx = point.X - old_point.X;
				double tdy = point.Y - old_point.Y;
				inverted_rt_transform.TransformDistance(ref tdx, ref tdy);

				double left = destination_rect.X;
				double top = destination_rect.Y;
				double right = destination_rect.GetRight();
				double bottom = destination_rect.GetBottom();

				if((selected_point.Edge & TransformEdge.Left) == TransformEdge.Left)
				{
					left += tdx;
				}
				else if((selected_point.Edge & TransformEdge.Right) == TransformEdge.Right)
				{
					right += tdx;
				}

				if((selected_point.Edge & TransformEdge.Top) == TransformEdge.Top)
				{
					top += tdy;
				}
				else if((selected_point.Edge & TransformEdge.Bottom) == TransformEdge.Bottom)
				{
					bottom += tdy;
				}

				destination_rect = CairoExtensions.FromLTRB(left, top, right, bottom);
				resize_matrix.InitRectToRect(source_rect, destination_rect);
			}
			else if(is_dragging)
			{
				double dx = point.X - old_point.X;
				double dy = point.Y - old_point.Y;

				translation_matrix.Translate(dx, dy);
			}

			old_point = point;

			inverted_rt_transform.InitIdentity();
			inverted_rt_transform.Multiply(rotation_matrix);
			inverted_rt_transform.Multiply(translation_matrix);

			transform.InitIdentity();
			transform.Multiply(resize_matrix);
			transform.Multiply(rotation_matrix);
			transform.Multiply(translation_matrix);

			transform_update.InitMatrix(inverted_transform);
			transform_update.Multiply(transform);

			inverted_transform.InitMatrix(transform);
			inverted_transform.Invert();

			inverted_rt_transform.Invert();

			OnUpdateTransform(transform, transform_update);
		}

		private TransformControlPoint FindTransformPoint(PointD point)
		{
			for(int i = 0; i < control_points.Length; i ++)
			{
				if(control_points[i].IsInside(transform, point))
					return control_points[i];
			}

			return null;
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			is_dragging = false;
			is_rotating = false;

			if(selected_point != null)
				SetCursor (DefaultCursor);

			selected_point = null;

			OnFinishTransform();
		}

		protected override void OnDraw (Cairo.Context g)
		{
			double scale = PintaCore.Workspace.Scale;

			if(is_rotating)
			{
				PointD center = source_rect.GetCenter();
				transform.TransformPoint(ref center);
				TransformControlPoint.DrawEllipse(g, center.X * scale, center.Y * scale);
			}
			else if(is_dragging == false)
			{
				for(int i = 0; i < control_points.Length; i ++)
				{
					control_points[i].Draw(g, transform, scale);
				}
			}
		}
		#endregion
	}
}

