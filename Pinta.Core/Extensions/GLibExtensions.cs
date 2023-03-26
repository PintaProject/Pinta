using System;
using System.Threading;

namespace Pinta.Core
{
	public static class GLibExtensions
	{
		// Ported from GtkSharp
		// TODO-GTK4 (bindings, unsubmitted) - should this be added upstream in gir.core? (along with setting it up at startup)
		public class GLibSynchronizationContext : SynchronizationContext
		{
			public override void Post (SendOrPostCallback d, object? state)
			{
				PostAction (() => d (state));
			}

			public override void Send (SendOrPostCallback d, object? state)
			{
				var mre = new ManualResetEvent (false);
				Exception? error = null;

				PostAction (() => {
					try {
						d (state);
					} catch (Exception ex) {
						error = ex;
					} finally {
						mre.Set ();
					}
				});

				mre.WaitOne ();

				if (error != null) {
					throw error;
				}
			}

			void PostAction (Action action)
			{
				GLib.Functions.IdleAddFull (0, (_) => {
					action ();
					return false;
				});
			}
		}

	}
}
