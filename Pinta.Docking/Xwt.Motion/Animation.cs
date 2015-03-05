//
// Tweener.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using System.Diagnostics;

namespace Xwt.Motion
{
	class Animation
	{
		double beginAt;
		double finishAt;
		Easing easing;
		Action<double> step;
		List<Animation> children;
		Action finished;
		bool finishedTriggered;
		
		public Animation ()
		{
			children = new List<Animation> ();
			easing = Easing.Linear;
			step = f => {};
		}
		
		public Animation (Action<double> callback, double start = 0.0f, double end = 1.0f, Easing easing = null, Action finished = null)
		{
			children = new List<Animation> ();
			this.easing = easing ?? Easing.Linear;
			this.finished = finished;
			
			var transform = AnimationExtensions.Interpolate (start, end);
			step = f => callback (transform (f));
		}

		public IEnumerable<Animation> ConcurrentAnimations {
			get { return children; }
		}
		
		public void Commit (IAnimatable owner, string name, uint rate = 16, uint length = 250, 
		                    Easing easing = null, Action<double, bool> finished = null, Func<bool> repeat = null)
		{
			owner.Animate (name, this, rate, length, easing, finished, repeat);
		}
		
		public Animation AddConcurrent (Animation animation, double beginAt = 0.0f, double finishAt = 1.0f)
		{
			if (beginAt < 0 || beginAt > 1)
				throw new ArgumentOutOfRangeException ("beginAt");

			if (finishAt < 0 || finishAt > 1)
				throw new ArgumentOutOfRangeException ("finishAt");

			if (finishAt <= beginAt)
				throw new ArgumentException ("finishAt must be greater than beginAt");

			animation.beginAt = beginAt;
			animation.finishAt = finishAt;
			children.Add (animation);
			return this;
		}
		
		public Animation AddConcurrent (Action<double> callback, double start = 0.0f, double end = 1.0f, Easing easing = null, double beginAt = 0.0f, double finishAt = 1.0f)
		{
			if (beginAt < 0 || beginAt > 1)
				throw new ArgumentOutOfRangeException ("beginAt");

			if (finishAt < 0 || finishAt > 1)
				throw new ArgumentOutOfRangeException ("finishAt");

			if (finishAt <= beginAt)
				throw new ArgumentException ("finishAt must be greater than beginAt");

			Animation child = new Animation (callback, start, end, easing);
			child.beginAt = beginAt;
			child.finishAt = finishAt;
			children.Add (child);
			return this;
		}
		
		public Action<double> GetCallback ()
		{
			Action<double> result = f => {
				step (easing.Func (f));
				foreach (var animation in children) {
					if (animation.finishedTriggered)
						continue;
					
					double val = Math.Max (0.0f, Math.Min (1.0f, (f - animation.beginAt) / (animation.finishAt - animation.beginAt)));
					
					if (val <= 0.0f) // not ready to process yet
						continue;
					
					var callback = animation.GetCallback ();
					callback (val);
					
					if (val >= 1.0f) {
						animation.finishedTriggered = true;
						if (animation.finished != null)
							animation.finished ();
					}
				}
			};
			return result;
		}
	}
	
	
}

