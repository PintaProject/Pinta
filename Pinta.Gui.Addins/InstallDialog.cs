using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Addins.Description;
using Mono.Addins.Setup;
using Pinta.Core;

namespace Pinta.Gui.Addins
{
	internal class InstallDialog : Adw.Window
	{
		private SetupService service;
		private PackageCollection packages_to_install = new ();
		private InstallErrorReporter error_reporter = new ();

		private Adw.WindowTitle window_title = new ();
		private StatusProgressBar progress_bar;

		private Gtk.Label error_heading_label;
		private Gtk.Label error_label;
		private Gtk.Label warning_heading_label;
		private Gtk.Label warning_label;
		private Gtk.Label install_heading_label;
		private Gtk.Label install_label;
		private Gtk.Label uninstall_heading_label;
		private Gtk.Label uninstall_label;
		private Gtk.Label unresolved_heading_label;
		private Gtk.Label unresolved_label;

		private Gtk.Button install_button;
		private Gtk.Button cancel_button;

		public InstallDialog (Gtk.Window parent, SetupService service)
		{
			this.service = service;

			TransientFor = parent;
			WidthRequest = 500;
			HeightRequest = 250;

			var content = Gtk.Box.New (Gtk.Orientation.Vertical, 12);
			content.Append (new Adw.HeaderBar () {
				TitleWidget = window_title
			});

			var labels = Gtk.Box.New (Gtk.Orientation.Vertical, 12);

			error_heading_label = Gtk.Label.New (
				Translations.GetString ("The selected extension packages can't be installed because there are dependency conflicts."));
			error_heading_label.AddCssClass (AdwaitaStyles.Title4);
			labels.Append (error_heading_label);

			error_label = new Gtk.Label ();
			error_label.AddCssClass (AdwaitaStyles.Body);
			error_label.AddCssClass (AdwaitaStyles.Error);
			labels.Append (error_label);

			warning_heading_label = new Gtk.Label ();
			warning_heading_label.AddCssClass (AdwaitaStyles.Title4);
			labels.Append (warning_heading_label);

			warning_label = new Gtk.Label ();
			warning_label.AddCssClass (AdwaitaStyles.Body);
			warning_label.AddCssClass (AdwaitaStyles.Warning);
			labels.Append (warning_label);

			install_heading_label = Gtk.Label.New (Translations.GetString ("The following packages will be installed:"));
			install_heading_label.AddCssClass (AdwaitaStyles.Title4);
			labels.Append (install_heading_label);

			install_label = new Gtk.Label ();
			install_label.AddCssClass (AdwaitaStyles.Body);
			labels.Append (install_label);

			uninstall_heading_label = Gtk.Label.New (Translations.GetString ("The following packages need to be uninstalled:"));
			uninstall_heading_label.AddCssClass (AdwaitaStyles.Title4);
			labels.Append (uninstall_heading_label);

			uninstall_label = new Gtk.Label ();
			uninstall_label.AddCssClass (AdwaitaStyles.Body);
			uninstall_label.AddCssClass (AdwaitaStyles.Warning);
			labels.Append (uninstall_label);

			unresolved_heading_label = Gtk.Label.New (Translations.GetString ("The following dependencies could not be resolved:"));
			unresolved_heading_label.AddCssClass (AdwaitaStyles.Title4);
			labels.Append (unresolved_heading_label);

			unresolved_label = new Gtk.Label ();
			unresolved_label.AddCssClass (AdwaitaStyles.Body);
			unresolved_label.AddCssClass (AdwaitaStyles.Error);
			labels.Append (unresolved_label);

			var scroll = Gtk.ScrolledWindow.New ();
			scroll.Child = labels;
			scroll.Vexpand = true;

			progress_bar = new StatusProgressBar (scroll, error_reporter);
			content.Append (progress_bar);

			var buttons = Gtk.Box.New (Gtk.Orientation.Horizontal, 12);
			buttons.Halign = Gtk.Align.End;
			buttons.SetAllMargins (12);

			cancel_button = Gtk.Button.NewWithLabel (Translations.GetString ("Cancel"));
			buttons.Append (cancel_button);

			install_button = Gtk.Button.NewWithLabel (Translations.GetString ("Install"));
			install_button.AddCssClass (AdwaitaStyles.SuggestedAction);
			buttons.Append (install_button);

			content.Append (buttons);
			Content = content;

			install_button.OnClicked += (_, _) => HandleInstallClicked ();
			cancel_button.OnClicked += (_, _) => Close ();
		}

		public void InitForInstall (AddinRepositoryEntry[] addins_to_install)
		{
			foreach (var addin in addins_to_install)
				packages_to_install.Add (Package.FromRepository (addin));

			DisplayInstallInfo ();
		}

		public void InitForInstall (string[] files_to_install)
		{
			foreach (string file in files_to_install)
				packages_to_install.Add (Package.FromFile (file));

			DisplayInstallInfo ();
		}

		private void DisplayInstallInfo ()
		{
			window_title.Title = Translations.GetString ("Install");

			PackageCollection to_uninstall;
			DependencyCollection unresolved;
			error_reporter.Clear ();
			bool success = service.ResolveDependencies (progress_bar, packages_to_install, out to_uninstall, out unresolved);

			error_heading_label.Visible = error_label.Visible = !success;
			if (error_label.Visible)
				error_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Errors));

			warning_heading_label.Visible = false;
			warning_label.Visible = error_reporter.Warnings.Any ();
			if (warning_label.Visible)
				warning_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Warnings));

			var sb = new StringBuilder ();
			foreach (Package p in packages_to_install) {
				sb.Append (p.Name);
				if (!p.SharedInstall)
					sb.Append (Translations.GetString (" (in user directory)"));
				sb.AppendLine ();
			}
			install_label.SetLabel (sb.ToString ());

			uninstall_label.Visible = to_uninstall.Count > 0;
			if (uninstall_label.Visible) {
				sb.Clear ();

				foreach (Package p in to_uninstall)
					sb.AppendLine (p.Name);

				uninstall_label.SetLabel (sb.ToString ());
			}

			uninstall_heading_label.Visible = uninstall_label.Visible = to_uninstall.Count > 0;
			if (uninstall_label.Visible) {
				sb.Clear ();

				foreach (Package p in to_uninstall)
					sb.AppendLine (p.Name);

				uninstall_label.SetLabel (sb.ToString ());
			}

			unresolved_heading_label.Visible = unresolved_label.Visible = unresolved.Count > 0;
			if (unresolved_label.Visible) {
				sb.Clear ();

				foreach (Dependency p in unresolved)
					sb.AppendLine (p.Name);

				unresolved_label.SetLabel (sb.ToString ());
			}

			install_button.Sensitive = success;
		}

		private async void HandleInstallClicked ()
		{
			install_button.Sensitive = false;
			cancel_button.Sensitive = false;

			error_heading_label.SetLabel (Translations.GetString ("The installation failed!"));
			warning_heading_label.SetLabel (Translations.GetString ("The installation completed with warnings."));

			error_reporter.Clear ();
			progress_bar.ShowProgress ();
			await Install ();
			progress_bar.HideProgress ();

			install_button.Visible = false;
			cancel_button.Sensitive = true;
			cancel_button.SetLabel (Translations.GetString ("Close"));

			install_heading_label.Visible = install_label.Visible = false;
			uninstall_heading_label.Visible = uninstall_label.Visible = false;
			unresolved_heading_label.Visible = unresolved_label.Visible = false;

			error_heading_label.Visible = error_label.Visible = error_reporter.Errors.Any ();
			if (error_label.Visible)
				error_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Errors));

			warning_heading_label.Visible = warning_label.Visible = error_reporter.Warnings.Any ();
			if (!error_label.Visible && warning_label.Visible)
				warning_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Warnings));

			if (!error_label.Visible && !warning_label.Visible)
				Close (); // Success!
		}

		private Task Install ()
		{
			return Task.Run (() => {
#if false // For testing
				float progress = 0;
				while (progress < 1) {
					progress_bar.SetProgress (progress);
					progress_bar.SetMessage ($"Installing {progress}");
					System.Threading.Thread.Sleep (500);
					progress += 0.1f;
				}
				progress_bar.ReportError ("some warning", null!);
#else
				service.Install (progress_bar, packages_to_install);
#endif
			});
		}
	}

	internal class InstallErrorReporter : IErrorReporter
	{
		public List<string> Errors { get; private init; } = new ();
		public List<string> Warnings { get; private init; } = new ();

		public InstallErrorReporter ()
		{
		}

		public void ReportError (string message, Exception exception)
		{
			Errors.Add (message);
		}

		public void ReportWarning (string message)
		{
			Warnings.Add (message);
		}

		public void Clear ()
		{
			Errors.Clear ();
			Warnings.Clear ();
		}
	}
}
