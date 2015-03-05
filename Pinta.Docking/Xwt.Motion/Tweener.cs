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
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace Xwt.Motion
{
	internal class Ticker
	{
		static Ticker ticker;
		public static Ticker Default {
			internal set { ticker = value; }
			get { return ticker ?? (ticker = new Ticker ()); }
		}

		readonly List<Tuple<int, Func<long, bool>>> timeouts;
		int count;
		bool enabled;
		readonly Stopwatch stopwatch;
		IDisposable timer;

		internal Ticker ()
		{
			count = 0;
			timeouts = new List<Tuple<int, Func<long, bool>>> ();
			stopwatch = new Stopwatch ();
		}

		bool HandleElapsed ()
		{	
			if (timeouts.Count > 0) {
				SendSignals ();
				stopwatch.Reset ();
				stopwatch.Start ();
			}
			return enabled;
		}

		protected void SendSignals (int timestep = -1)
		{
			long step = (timestep >= 0) ? timestep : stopwatch.ElapsedMilliseconds;
			stopwatch.Reset ();
			stopwatch.Start ();

			var localCopy = new List<Tuple<int, Func<long, bool>>> (timeouts);
			foreach (var timeout in localCopy) {
				bool remove = !timeout.Item2 (step);
				if (remove)
					timeouts.RemoveAll (t => t.Item1 == timeout.Item1);
			}

			if (!timeouts.Any ()) {
				enabled = false;
				Disable ();
			}
		}

		void Enable ()
		{
			stopwatch.Reset ();
			stopwatch.Start ();
			EnableTimer ();
		}

		void Disable ()
		{
			stopwatch.Reset ();
			DisableTimer ();
		}

		protected virtual void EnableTimer ()
		{
			timer = TimeoutInvoke (16, HandleElapsed);
		}

		protected virtual void DisableTimer ()
		{
			timer.Dispose ();
			timer = null;
		}

		public virtual int Insert (Func<long, bool> timeout)
		{
			count++;
			timeouts.Add (new Tuple<int,Func<long, bool>> (count, timeout));

			if (!enabled) {
				enabled = true;
				Enable ();
			}

			return count;
		}

		public virtual void Remove (int handle)
		{
			timeouts.RemoveAll (t => t.Item1 == handle);

			if (!timeouts.Any ()) {
				enabled = false;
				Disable ();
			}
		}

        public static object TimerInvoke (Func<bool> action, TimeSpan timeSpan)
        {
            if (action == null)
                throw new ArgumentNullException ("action");
            if (timeSpan.TotalMilliseconds < 0)
                throw new ArgumentException ("Timer period must be >=0", "timeSpan");

            return GLib.Timeout.Add ((uint)timeSpan.TotalMilliseconds, delegate {
                return action ();
            });
        }

        public static void CancelTimerInvoke (object id)
        {
            if (id == null)
                throw new ArgumentNullException ("id");

            GLib.Source.Remove ((uint)id);
        }

        /// <summary>
        /// Invokes an action in the GUI thread after the provided time span
        /// </summary>
        /// <returns>
        /// A timer object
        /// </returns>
        /// <param name='action'>
        /// The action to execute.
        /// </param>
        /// <remarks>
        /// This method schedules the execution of the provided function. The function
        /// must return 'true' if it has to be executed again after the time span, or 'false'
        /// if the timer can be discarded.
        /// The execution of the funciton can be canceled by disposing the returned object.
        /// </remarks>
        public static IDisposable TimeoutInvoke (int ms, Func<bool> action)
        {
            if (action == null)
                throw new ArgumentNullException ("action");
            if (ms < 0)
                throw new ArgumentException ("ms can't be negative");

            return TimeoutInvoke (TimeSpan.FromMilliseconds (ms), action);
        }

        /// <summary>
        /// Invokes an action in the GUI thread after the provided time span
        /// </summary>
        /// <returns>
        /// A timer object
        /// </returns>
        /// <param name='action'>
        /// The action to execute.
        /// </param>
        /// <remarks>
        /// This method schedules the execution of the provided function. The function
        /// must return 'true' if it has to be executed again after the time span, or 'false'
        /// if the timer can be discarded.
        /// The execution of the funciton can be canceled by disposing the returned object.
        /// </remarks>
        public static IDisposable TimeoutInvoke (TimeSpan timeSpan, Func<bool> action)
        {
            if (action == null)
                throw new ArgumentNullException ("action");
            if (timeSpan.Ticks < 0)
                throw new ArgumentException ("timeSpan can't be negative");

            Timer t = new Timer ();
            t.Id = TimerInvoke (delegate {
                bool res = false;
                try {
                    res = action ();
                } catch (Exception) {
                }
                return res;
            }, timeSpan);
            return t;
        }

        class Timer : IDisposable
        {
            public object Id;
            public void Dispose ()
            {
                CancelTimerInvoke (Id);
            }
        }
    }

	class Tweener
	{
		public uint Length { get; private set; }
		public uint Rate { get; private set; }
		public double Value { get; private set; }
		public Easing Easing { get; set; }
		public bool Loop { get; set; }
		public string Handle { get; set; }
		
		public event EventHandler ValueUpdated;
		public event EventHandler Finished;

		int timer;
		long lastMilliseconds;
		
		public Tweener (uint length, uint rate)
		{
			Value = 0.0f;
			Length = length;
			Loop = false;
			Rate = rate;
			Easing = Easing.Linear;
		}
		
		~Tweener ()
		{
			if (timer != 0)
				Ticker.Default.Remove (timer);
			timer = 0;
		}
		
		public void Start ()
		{
			Pause ();

			lastMilliseconds = 0;
			timer = Ticker.Default.Insert (step => {
				var ms = step + lastMilliseconds;

				double rawValue = Math.Min (1.0f, ms / (double) Length);
				Value = Easing.Func (rawValue);

				lastMilliseconds = ms;

				if (ValueUpdated != null)
					ValueUpdated (this, EventArgs.Empty);
				
				if (rawValue >= 1.0f)
				{
					if (Loop) {
						lastMilliseconds = 0;
						Value = 0.0f;
						return true;
					}
					
					if (Finished != null)
						Finished (this, EventArgs.Empty);
					Value = 0.0f;
					timer = 0;
					return false;
				}
				return true;
			});
		}
		
		public void Stop ()
		{
			Pause ();
			Value = 1.0f;
			if (Finished != null)
				Finished (this, EventArgs.Empty);
			Value = 0.0f;
		}
		
		public void Pause ()
		{	
			if (timer != 0) {
				Ticker.Default.Remove (timer);
				timer = 0;
			}
		}
	}
}
