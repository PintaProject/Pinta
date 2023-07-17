using System;
using Mono.Addins;

namespace Pinta.Gui.Addins
{
	internal interface IErrorReporter
	{
		void ReportError (string message, Exception exception);
		void ReportWarning (string message);
	}

	/// <summary>
	/// Implementation of IProgressStatus to display status with an overlaid progress bar,
	/// and toasts for any warnings / errors
	/// Since the add-in queries may happen from a background thread, any UI updates are
	/// invoked on the UI thread.
	/// </summary>
	internal class StatusProgressBar : Adw.Bin, IProgressStatus
	{
		private readonly Gtk.Overlay progress_overlay = new ();
		private readonly Gtk.ProgressBar progress_bar;
		private readonly IErrorReporter error_reporter;

		public StatusProgressBar (Gtk.Widget primary_widget, IErrorReporter error_reporter)
		{
			this.error_reporter = error_reporter;

			progress_bar = new Gtk.ProgressBar () {
				Fraction = 0.5,
				ShowText = true,
				Valign = Gtk.Align.End
			};
			progress_bar.AddCssClass (Pinta.Core.AdwaitaStyles.Osd);

			progress_overlay.Child = primary_widget;
			Child = progress_overlay;
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

		public void ReportError (string message, Exception exception) => error_reporter.ReportError (message, exception);

		public void ReportWarning (string message) => error_reporter.ReportWarning (message);

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
