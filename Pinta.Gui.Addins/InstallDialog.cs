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
	private readonly PackageCollection packages_to_install = [];
	private IReadOnlyList<string> addins_to_remove = [];
	private readonly InstallErrorReporter error_reporter;

	private readonly Adw.WindowTitle window_title;
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
		Adw.WindowTitle windowTitle = new ();

		BoxStyle spacedHorizontal = new (
			orientation: Gtk.Orientation.Horizontal,
			spacing: 12);

		BoxStyle spacedVertical = new (
			orientation: Gtk.Orientation.Vertical,
			spacing: 12);

		Gtk.Label errorHeadingLabel = Gtk.Label.New (Translations.GetString ("The selected extension packages can't be installed because there are dependency conflicts."));
		errorHeadingLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Label errorLabel = new ();
		errorLabel.AddCssClass (AdwaitaStyles.Body);
		errorLabel.AddCssClass (AdwaitaStyles.Error);

		Gtk.Label warningHeadingLabel = new ();
		warningHeadingLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Label warningLabel = new ();
		warningLabel.AddCssClass (AdwaitaStyles.Body);
		warningLabel.AddCssClass (AdwaitaStyles.Warning);

		Gtk.Label installHeadingLabel = Gtk.Label.New (Translations.GetString ("The following packages will be installed:"));
		installHeadingLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Label installLabel = new ();
		installLabel.AddCssClass (AdwaitaStyles.Body);

		Gtk.Label uninstallHeadingLabel = Gtk.Label.New (Translations.GetString ("The following packages need to be uninstalled:"));
		uninstallHeadingLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Label uninstallLabel = new ();
		uninstallLabel.AddCssClass (AdwaitaStyles.Body);
		uninstallLabel.AddCssClass (AdwaitaStyles.Warning);

		Gtk.Label dependenciesHeadingLabel = Gtk.Label.New (Translations.GetString ("The following dependencies could not be resolved:"));
		dependenciesHeadingLabel.AddCssClass (AdwaitaStyles.Title4);

		Gtk.Label dependenciesLabel = new ();
		dependenciesLabel.AddCssClass (AdwaitaStyles.Body);
		dependenciesLabel.AddCssClass (AdwaitaStyles.Error);

		Gtk.Box labels = GtkExtensions.Box (
			spacedVertical,
			[
				errorHeadingLabel,
				errorLabel,
				warningHeadingLabel,
				warningLabel,
				installHeadingLabel,
				installLabel,
				uninstallHeadingLabel,
				uninstallLabel,
				dependenciesHeadingLabel,
				dependenciesLabel,
			]);
		labels.SetAllMargins (6);

		// Left align all labels.
		Gtk.Widget? label = labels.GetFirstChild ();
		while (label != null) {
			label.Halign = Gtk.Align.Start;
			label = label.GetNextSibling ();
		}

		Gtk.ScrolledWindow scroll = Gtk.ScrolledWindow.New ();
		scroll.Child = labels;
		scroll.HscrollbarPolicy = Gtk.PolicyType.Never;
		scroll.Vexpand = true;

		InstallErrorReporter errorReporter = new ();

		StatusProgressBar progressBar = new (scroll, errorReporter);

		Gtk.Button cancelButton = Gtk.Button.NewWithLabel (Translations.GetString ("Cancel"));
		cancelButton.OnClicked += (_, _) => Close ();

		Gtk.Button installButton = Gtk.Button.NewWithLabel (Translations.GetString ("Install"));
		installButton.OnClicked += (_, _) => HandleInstallClicked ();

		Gtk.Box buttons = Gtk.Box.New (Gtk.Orientation.Horizontal, 12);
		buttons.Halign = Gtk.Align.End;
		buttons.SetAllMargins (12);
		buttons.Append (cancelButton);
		buttons.Append (installButton);

		// --- Initialization (Gtk.Widget)

		WidthRequest = 500;
		HeightRequest = 250;

		// --- Initialization (Gtk.Window)

		TransientFor = parent;

		// --- Initialization (Adw.Window)

		Content = GtkExtensions.Box (
			spacedVertical,
			[
				new Adw.HeaderBar { TitleWidget = windowTitle },
				progressBar,
				buttons,
			]);

		// --- References to keep

		this.service = service;

		window_title = windowTitle;

		error_heading_label = errorHeadingLabel;
		error_label = errorLabel;

		warning_heading_label = warningHeadingLabel;
		warning_label = warningLabel;

		install_heading_label = installHeadingLabel;
		install_label = installLabel;

		uninstall_heading_label = uninstallHeadingLabel;
		uninstall_label = uninstallLabel;

		dependencies_heading_label = dependenciesHeadingLabel;
		dependencies_label = dependenciesLabel;

		progress_bar = progressBar;

		cancel_button = cancelButton;
		install_button = installButton;

		error_reporter = errorReporter;
	}

	public void InitForInstall (AddinRepositoryEntry[] addins_to_install)
	{
		foreach (var addin in addins_to_install)
			packages_to_install.Add (Package.FromRepository (addin));

		DisplayInstallInfo ();
	}

	public bool InitForInstall (IEnumerable<string> files_to_install)
	{
		try {
			foreach (string file in files_to_install)
				packages_to_install.Add (Package.FromFile (file));

			DisplayInstallInfo ();

			return true;

		} catch {
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
	}

	public void InitForUninstall (IReadOnlyList<Addin> addinsToUninstall)
	{
		window_title.Title = Translations.GetString ("Uninstall");
		install_button.Label = Translations.GetString ("Uninstall");
		install_button.AddCssClass (AdwaitaStyles.DestructiveAction);

		addins_to_remove =
			addinsToUninstall
			.Select (a => a.Id)
			.ToArray ();

		error_heading_label.Visible = error_label.Visible = false;
		warning_heading_label.Visible = warning_label.Visible = false;
		install_heading_label.Visible = install_label.Visible = false;

		uninstall_heading_label.SetLabel (Translations.GetString ("The following packages will be uninstalled:"));
		uninstall_label.SetLabel (string.Join (Environment.NewLine, addinsToUninstall.Select (a => a.Name)));

		HashSet<Addin> dependents = [];
		foreach (string id in addins_to_remove)
			dependents.UnionWith (service.GetDependentAddins (id, true));

		dependencies_heading_label.Visible = dependencies_label.Visible = dependents.Count != 0;

		if (dependents.Count == 0)
			return;

		dependencies_heading_label.SetLabel (Translations.GetString ("There are other extension packages that depend on the previous ones which will also be uninstalled:"));
		dependencies_label.SetLabel (string.Join (Environment.NewLine, dependents.Select (a => a.Name)));
	}

	private void DisplayInstallInfo ()
	{
		window_title.Title = Translations.GetString ("Install");
		install_button.AddCssClass (AdwaitaStyles.SuggestedAction);

		error_reporter.Clear ();
		bool success = service.ResolveDependencies (progress_bar, packages_to_install, out var to_uninstall, out var unresolved);

		error_heading_label.Visible = error_label.Visible = !success;
		if (error_label.Visible)
			error_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Errors));

		warning_heading_label.Visible = false;
		warning_label.Visible = error_reporter.Warnings.Count != 0;
		if (warning_label.Visible)
			warning_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Warnings));

		StringBuilder sb = new ();
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

			foreach (Dependency p in unresolved.Cast<Dependency> ())
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

		if (addins_to_remove.Count != 0)
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

		error_heading_label.Visible = error_label.Visible = error_reporter.Errors.Count != 0;

		if (error_label.Visible) {
			error_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Errors));
			return;
		}

		warning_heading_label.Visible = warning_label.Visible = error_reporter.Warnings.Count != 0;

		if (warning_label.Visible)
			warning_label.SetLabel (string.Join (Environment.NewLine, error_reporter.Warnings));
		else
			Close (); // Success with no warnings!

		OnSuccess?.Invoke (this, EventArgs.Empty);
	}

	private Task<bool> Install ()
	{
		error_heading_label.SetLabel (Translations.GetString ("The installation failed!"));
		warning_heading_label.SetLabel (Translations.GetString ("The installation has completed with warnings."));
		return Task.Run (() => service.Install (progress_bar, packages_to_install));
	}

	private Task Uninstall ()
	{
		error_heading_label.SetLabel (Translations.GetString ("The uninstallation failed!"));
		warning_heading_label.SetLabel (Translations.GetString ("The uninstallation has completed with warnings."));
		return Task.Run (() => service.Uninstall (progress_bar, addins_to_remove));
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
		errors = [];
		Errors = new ReadOnlyCollection<string> (errors);
		warnings = [];
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
