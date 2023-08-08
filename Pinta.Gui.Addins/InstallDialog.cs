using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Addins;
using Mono.Addins.Description;
using Mono.Addins.Setup;
using Pinta.Core;

namespace Pinta.Gui.Addins;

internal sealed class InstallDialog : Adw.Window
{
	private readonly SetupService service;
	private readonly PackageCollection packages_to_install = new ();
	private List<string> addins_to_remove = new ();
	private readonly InstallErrorReporter error_reporter = new ();

	private readonly Adw.WindowTitle window_title = new ();
	private readonly StatusProgressBar progress_bar;

	private readonly Gtk.Label error_heading_label;
	private readonly Gtk.Label error_label;
	private readonly Gtk.Label warning_heading_label;
	private readonly Gtk.Label warning_label;
	private readonly Gtk.Label install_heading_label;
	private readonly Gtk.Label install_label;
	private readonly Gtk.Label uninstall_heading_label;
	private readonly Gtk.Label uninstall_label;
	private readonly Gtk.Label dependencies_heading_label;
	private readonly Gtk.Label dependencies_label;

	private readonly Gtk.Button install_button;
	private readonly Gtk.Button cancel_button;

	public event EventHandler? OnSuccess;

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
		labels.SetAllMargins (6);

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

		dependencies_heading_label = Gtk.Label.New (Translations.GetString ("The following dependencies could not be resolved:"));
		dependencies_heading_label.AddCssClass (AdwaitaStyles.Title4);
		labels.Append (dependencies_heading_label);

		dependencies_label = new Gtk.Label ();
		dependencies_label.AddCssClass (AdwaitaStyles.Body);
		dependencies_label.AddCssClass (AdwaitaStyles.Error);
		labels.Append (dependencies_label);

		// Left align all labels.
		Gtk.Widget? label = labels.GetFirstChild ();
		while (label != null) {
			label.Halign = Gtk.Align.Start;
			label = label.GetNextSibling ();
		}

		var scroll = Gtk.ScrolledWindow.New ();
		scroll.Child = labels;
		scroll.HscrollbarPolicy = Gtk.PolicyType.Never;
		scroll.Vexpand = true;

		progress_bar = new StatusProgressBar (scroll, error_reporter);
		content.Append (progress_bar);

		var buttons = Gtk.Box.New (Gtk.Orientation.Horizontal, 12);
		buttons.Halign = Gtk.Align.End;
		buttons.SetAllMargins (12);

		cancel_button = Gtk.Button.NewWithLabel (Translations.GetString ("Cancel"));
		buttons.Append (cancel_button);

		install_button = Gtk.Button.NewWithLabel (Translations.GetString ("Install"));
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

	public bool InitForInstall (string[] files_to_install)
	{
		try {
			foreach (string file in files_to_install)
				packages_to_install.Add (Package.FromFile (file));

			DisplayInstallInfo ();

		} catch (Exception) {
			var dialog = Adw.MessageDialog.New (
				TransientFor,
				Translations.GetString ("Failed to load extension package"),
				Translations.GetString ("The file may be an invalid or corrupt extension package"));

			const string ok_response = "ok";
			dialog.AddResponse (ok_response, Translations.GetString ("_OK"));
			dialog.DefaultResponse = ok_response;
			dialog.CloseResponse = ok_response;

			dialog.Present ();
			return false;
		}

		return true;
	}

	public void InitForUninstall (Addin[] addins_to_uninstall)
	{
		window_title.Title = Translations.GetString ("Uninstall");
		install_button.Label = Translations.GetString ("Uninstall");
		install_button.AddCssClass (AdwaitaStyles.DestructiveAction);

		addins_to_remove = addins_to_uninstall.Select (a => a.Id).ToList ();

		error_heading_label.Visible = error_label.Visible = false;
		warning_heading_label.Visible = warning_label.Visible = false;
		install_heading_label.Visible = install_label.Visible = false;

		uninstall_heading_label.SetLabel (Translations.GetString ("The following packages will be uninstalled:"));
		uninstall_label.SetLabel (string.Join (Environment.NewLine, addins_to_uninstall.Select (a => a.Name)));

		var dependents = new HashSet<Addin> ();
		foreach (string id in addins_to_remove) {
			dependents.UnionWith (service.GetDependentAddins (id, true));
		}

		dependencies_heading_label.Visible = dependencies_label.Visible = dependents.Any ();
		if (dependents.Any ()) {
			dependencies_heading_label.SetLabel (Translations.GetString (
				"There are other extension packages that depend on the previous ones which will also be uninstalled:"));
			dependencies_label.SetLabel (string.Join (Environment.NewLine, dependents.Select (a => a.Name)));
		}
	}

	private void DisplayInstallInfo ()
	{
		window_title.Title = Translations.GetString ("Install");
		install_button.AddCssClass (AdwaitaStyles.SuggestedAction);

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

		dependencies_heading_label.Visible = dependencies_label.Visible = unresolved.Count > 0;
		if (dependencies_label.Visible) {
			sb.Clear ();

			foreach (Dependency p in unresolved)
				sb.AppendLine (p.Name);

			dependencies_label.SetLabel (sb.ToString ());
		}

		install_button.Sensitive = success;
	}

	private async void HandleInstallClicked ()
	{
		install_button.Sensitive = false;
		cancel_button.Sensitive = false;

		error_reporter.Clear ();
		progress_bar.ShowProgress ();

		if (addins_to_remove.Any ())
			await Uninstall ();
		else
			await Install ();

		progress_bar.HideProgress ();

		install_button.Visible = false;
		cancel_button.Sensitive = true;
		cancel_button.SetLabel (Translations.GetString ("Close"));

		install_heading_label.Visible = install_label.Visible = false;
		uninstall_heading_label.Visible = uninstall_label.Visible = false;
		dependencies_heading_label.Visible = dependencies_label.Visible = false;

		error_heading_label.Visible = error_label.Visible = error_reporter.Errors.Any ();
		if (error_label.Visible)
			error_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Errors));
		else {
			warning_heading_label.Visible = warning_label.Visible = error_reporter.Warnings.Any ();
			if (warning_label.Visible)
				warning_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Warnings));
			else
				Close (); // Success with no warnings!

			OnSuccess?.Invoke (this, EventArgs.Empty);
		}
	}

	private Task Install ()
	{
		error_heading_label.SetLabel (Translations.GetString ("The installation failed!"));
		warning_heading_label.SetLabel (Translations.GetString ("The installation has completed with warnings."));

		return Task.Run (() => {
			service.Install (progress_bar, packages_to_install);
		});
	}

	private Task Uninstall ()
	{
		error_heading_label.SetLabel (Translations.GetString ("The uninstallation failed!"));
		warning_heading_label.SetLabel (Translations.GetString ("The uninstallation has completed with warnings."));

		return Task.Run (() => {
			service.Uninstall (progress_bar, addins_to_remove);
		});
	}
}

internal sealed class InstallErrorReporter : IErrorReporter
{
	private readonly List<string> errors;
	public ReadOnlyCollection<string> Errors { get; }

	private readonly List<string> warnings;
	public ReadOnlyCollection<string> Warnings { get; }

	public InstallErrorReporter ()
	{
		errors = new List<string> ();
		Errors = new ReadOnlyCollection<string> (errors);
		warnings = new List<string> ();
		Warnings = new ReadOnlyCollection<string> (warnings);
	}

	public void ReportError (string message, Exception exception)
	{
		errors.Add (message);
	}

	public void ReportWarning (string message)
	{
		warnings.Add (message);
	}

	public void Clear ()
	{
		errors.Clear ();
		warnings.Clear ();
	}
}
