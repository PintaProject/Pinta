using System;
using Mono.Addins;

namespace Pinta.Gui.Addins
{
	/// <summary>
	/// Implementation of IProgressStatus to display status with an overlaid progress bar,
	/// and toasts for any warnings / errors
	/// Since the add-in queries may happen from a background thread, any UI updates are
	/// invoked on the UI thread.
	/// </summary>
	internal class StatusProgressBar : Adw.Bin, IProgressStatus
	{
		private Adw.ToastOverlay toast_overlay = new ();
		private Gtk.Overlay progress_overlay = new ();
		private Gtk.ProgressBar progress_bar;

		public StatusProgressBar (Gtk.Widget primary_widget)
		{
			progress_bar = new Gtk.ProgressBar () {
				Fraction = 0.5,
				ShowText = true
			};
			progress_bar.AddCssClass (Pinta.Core.AdwaitaStyles.Osd);

			progress_overlay.Child = primary_widget;
			toast_overlay.Child = progress_overlay;
			Child = toast_overlay;
		}

		public void ShowProgress ()
		{
			progress_overlay.AddOverlay (progress_bar);
		}

		public void HideProgress ()
		{
			progress_overlay.RemoveOverlay (progress_bar);
		}

		#region IProgressStatus implementation. These functions may be called from a background thread.

		public int LogLevel => 1; // Normal log level

		public bool IsCanceled => false;

		public void Cancel ()
		{
			throw new NotImplementedException ();
		}

		public void Log (string msg)
		{
			Console.WriteLine ("Info: {0}", msg);
		}

		public void ReportError (string message, Exception exception)
		{
			Console.WriteLine ("Error: {0}\n{1}", message, exception);

			GLib.Functions.IdleAddFull (0, (_) => {
				toast_overlay.AddToast (Adw.Toast.New (message));
				return false;
			});
		}

		public void ReportWarning (string message)
		{
			Console.WriteLine ("Warning: {0}", message);

			GLib.Functions.IdleAddFull (0, (_) => {
				toast_overlay.AddToast (Adw.Toast.New (message));
				return false;
			});
		}

		public void SetMessage (string msg)
		{
			GLib.Functions.IdleAddFull (0, (_) => {
				progress_bar.Text = msg;
				return false;
			});
		}

		public void SetProgress (double progress)
		{
			GLib.Functions.IdleAddFull (0, (_) => {
				progress_bar.Fraction = progress;
				return false;
			});
		}
		#endregion
	}
}
