using Gio;
using Gtk;
using Mono.Addins.Localization;
using Pinta.Core;

namespace PintaBenchmarks;

internal sealed class MockChromeManager : IChromeManager
{
	public PointI LastCanvasCursorPoint { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

	public Box? MainToolBar => throw new NotImplementedException ();

	public Box ToolToolBar => throw new NotImplementedException ();

	public Box ToolBox => throw new NotImplementedException ();

	public Box StatusBar => throw new NotImplementedException ();

	public IProgressDialog ProgressDialog => throw new NotImplementedException ();

	public Menu AdjustmentsMenu => throw new NotImplementedException ();

	public Menu EffectsMenu => throw new NotImplementedException ();

	public Gtk.Application Application => throw new NotImplementedException ();

	public Window MainWindow => throw new NotImplementedException ();

	public Widget ImageTabsNotebook => throw new NotImplementedException ();

	public bool MainWindowBusy { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

	public event EventHandler? LastCanvasCursorPointChanged;
	public event EventHandler<TextChangedEventArgs>? StatusBarTextChanged;

	public void InitializeApplication (Gtk.Application application)
	{
		throw new NotImplementedException ();
	}

	public void InitializeErrorDialogHandler (ErrorDialogHandler handler)
	{
		throw new NotImplementedException ();
	}

	public void InitializeImageTabsNotebook (Widget notebook)
	{
		throw new NotImplementedException ();
	}

	public void InitializeMainMenu (Menu adj_menu, Menu effects_menu)
	{
		throw new NotImplementedException ();
	}

	public void InitializeMainToolBar (Box mainToolBar)
	{
		throw new NotImplementedException ();
	}

	public void InitializeMessageDialog (MessageDialogHandler handler)
	{
		throw new NotImplementedException ();
	}

	public void InitializeProgessDialog (IProgressDialog progressDialog)
	{
		throw new NotImplementedException ();
	}

	public void InitializeSimpleEffectDialog (SimpleEffectDialogHandler handler)
	{
		throw new NotImplementedException ();
	}

	public void InitializeStatusBar (Box statusbar)
	{
		throw new NotImplementedException ();
	}

	public void InitializeToolBox (Box toolbox)
	{
		throw new NotImplementedException ();
	}

	public void InitializeToolToolBar (Box toolToolBar)
	{
		throw new NotImplementedException ();
	}

	public void InitializeWindowShell (Window shell)
	{
		throw new NotImplementedException ();
	}

	public void LaunchSimpleEffectDialog (BaseEffect effect, IAddinLocalizer localizer)
	{
		throw new NotImplementedException ();
	}

	public void SetStatusBarText (string text)
	{
		throw new NotImplementedException ();
	}

	public void ShowErrorDialog (Window parent, string message, string body, string details)
	{
		throw new NotImplementedException ();
	}

	public void ShowMessageDialog (Window parent, string message, string body)
	{
		throw new NotImplementedException ();
	}
}
