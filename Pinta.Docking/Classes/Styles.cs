//
// Styles.cs
//
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc
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
using Pinta.Docking;

namespace Pinta.Docking
{
	public static class Styles
	{
		public static event EventHandler Changed;

#if false
		public static Theme CurrentStyle { get { return IdeTheme.UserInterfaceTheme; } }
#endif

		public static Color BackgroundColor { get; internal set; }        // must be the bg color from Gtkrc
		public static Color BaseBackgroundColor { get; internal set; }    // must be the base color from Gtkrc
		public static Color BaseForegroundColor { get; internal set; }    // must be the text color from Gtkrc
		public static Color BaseSelectionBackgroundColor { get; internal set; }
		public static Color BaseSelectionTextColor { get; internal set; }
		public static Color BaseIconColor { get; internal set; }
		public static Color LinkForegroundColor { get; internal set; }
		public static Color BorderColor { get; internal set; }
		public static Color FrameBoxBorderColor { get; internal set; }
		public static Color SecondaryTextColor { get; internal set; }
		public static string SecondaryTextColorHexString { get; internal set; }
		public static Color SecondarySelectionTextColor { get; internal set; }

		public static Color FocusColor { get; internal set; }

		public static Color ErrorForegroundColor { get; internal set; }
		public static Color WarningForegroundColor { get; internal set; }
		public static Color InformationForegroundColor { get; internal set; }

		public static Color ErrorBoxForegroundColor { get; internal set; }
		public static Color ErrorBoxBackgroundColor { get; internal set; }
		public static Color WarningBoxForegroundColor { get; internal set; }
		public static Color WarningBoxBackgroundColor { get; internal set; }
		public static Color InformationBoxForegroundColor { get; internal set; }
		public static Color InformationBoxBackgroundColor { get; internal set; }
		
		public static Pango.FontDescription DefaultFont { get; internal set; }
		public static string DefaultFontName { get; internal set; }

		public static double FontScale11 = 0.92308;
		public static double FontScale12 = 1;
		public static double FontScale13 = 1.07693;
		public static double FontScale14 = 1.15385;

		public static Color ThinSplitterColor { get; internal set; }
		public static Color SeparatorColor { get; internal set; }
		public static Color PrimaryBackgroundColor { get; internal set; }
		public static Color SecondaryBackgroundLighterColor { get; internal set; }
		public static Color SecondaryBackgroundDarkerColor { get; internal set; }

		[Obsolete ("Please use SecondaryTextColor")]
		public static Color DimTextColor { get => SecondaryTextColor; }

		[Obsolete ("Please use SecondaryTextColorHexString")]
		public static string DimTextColorHexString { get => SecondaryTextColorHexString; }

		public static Color StatusInformationBackgroundColor { get; internal set; }
		public static Color StatusInformationTextColor { get; internal set; }
		public static Color StatusWarningBackgroundColor { get; internal set; }
		public static Color StatusWarningTextColor { get; internal set; }
		public static Color StatusErrorBackgroundColor { get; internal set; }
		public static Color StatusErrorTextColor { get; internal set; }

		// Document tab bar

		public static Color TabBarBackgroundColor { get; internal set; }
		public static Color TabBarActiveTextColor { get; internal set; }
		public static Color TabBarNotifyTextColor { get; internal set; }
		public static Color TabBarInactiveTextColor { get; internal set; }

		public static Color BreadcrumbBackgroundColor { get; internal set; }
		public static Color BreadcrumbTextColor { get; internal set; }
		public static Color BreadcrumbButtonFillColor { get; internal set; }
		public static Color BreadcrumbBottomBorderColor { get; internal set; }

		// Document Subview Tabs

		public static Color SubTabBarBackgroundColor { get; internal set; }
		public static Color SubTabBarTextColor { get; internal set; }
		public static Color SubTabBarActiveBackgroundColor { get; internal set; }
		public static Color SubTabBarActiveTextColor { get; internal set; }
		public static Color SubTabBarHoverBackgroundColor { get; internal set; }
		public static Color SubTabBarSeparatorColor { get; internal set; }

		// Dock pads

		public static Color PadBackground { get; internal set; }
		public static Color InactivePadBackground { get; internal set; }
		public static Color PadLabelColor { get; internal set; }
		public static Color InactivePadLabelColor { get; internal set; }
		public static Color DockFrameBackground { get; internal set; }
		public static Color DockSeparatorColor { get; internal set; }
		public static Color DockBarBackground { get; internal set; }
		public static Color DockBarPrelightColor { get; internal set; }
		public static Color DockBarLabelColor { get; internal set; }

		public static Color BrowserPadBackground { get; internal set; }
		public static Color InactiveBrowserPadBackground { get; internal set; }

		public static Color PadCategoryBackgroundColor { get; internal set; }
		public static Color PadCategoryBorderColor { get; internal set; }
		public static Color PadCategoryLabelColor { get; internal set; }

		public static Color PropertyPadLabelBackgroundColor { get; internal set; }
		public static Color PropertyPadDividerColor { get; internal set; }

		// Status area

		public static Color WidgetBorderColor { get; internal set; }

		public static Color StatusBarBorderColor { get; internal set; }

		public static Color StatusBarFill1Color { get; internal set; }
		public static Color StatusBarFill2Color { get; internal set; }
		public static Color StatusBarFill3Color { get; internal set; }
		public static Color StatusBarFill4Color { get; internal set; }

		public static Color StatusBarErrorColor { get; internal set; }

		public static Color StatusBarInnerColor { get; internal set; }
		public static Color StatusBarShadowColor1 { get; internal set; }
		public static Color StatusBarShadowColor2 { get; internal set; }
		public static Color StatusBarTextColor { get; internal set; }
		public static Color StatusBarProgressBackgroundColor { get; internal set; }
		public static Color StatusBarProgressOutlineColor { get; internal set; }

		public static readonly Pango.FontDescription StatusFont = Pango.FontDescription.FromString ("Normal");

		public static int StatusFontPixelHeight { get { return 11; } }
		public static int ProgressBarHeight { get { return 18; } }
		public static int ProgressBarInnerPadding { get { return 4; } }
		public static int ProgressBarOuterPadding { get { return 4; } }

		// Toolbar

		public static Color ToolbarBottomBorderColor { get; internal set; }

		// Code Completion

		public static readonly int TooltipInfoSpacing;

		// Popover Windows

		public static class PopoverWindow
		{
			public static readonly int PagerTriangleSize = 6;
			public static readonly int PagerHeight = 16;

			public static Color DefaultBackgroundColor { get; internal set; }
			public static Color ErrorBackgroundColor { get; internal set; }
			public static Color WarningBackgroundColor { get; internal set; }
			public static Color InformationBackgroundColor { get; internal set; }

			public static Color DefaultTextColor { get; internal set; }
			public static Color ErrorTextColor { get; internal set; }
			public static Color WarningTextColor { get; internal set; }
			public static Color InformationTextColor { get; internal set; }

			public static Color ShadowColor { get; internal set; }
			public static Color BorderColor { get; internal set; }

			public static class ParamaterWindows
			{
				public static Color GradientStartColor { get; internal set; }
				public static Color GradientEndColor { get; internal set; }
			}
		}

		// Code Completion

		public static class CodeCompletion
		{
			public static Color BackgroundColor { get; internal set; }
			public static Color TextColor { get; internal set; }
			public static Color CategoryColor { get; internal set; }
			public static Color HighlightColor { get; internal set; }
			public static Color SelectionBackgroundColor { get; internal set; }
			public static Color SelectionBackgroundInactiveColor { get; internal set; }
			public static Color SelectionTextColor { get; internal set; }
			public static Color SelectionHighlightColor { get; internal set; }
		}

		// Global Search

		public static class GlobalSearch
		{
			public static Color HeaderTextColor { get; internal set; }
			public static Color SeparatorLineColor { get; internal set; }
			public static Color HeaderBackgroundColor { get; internal set; }
			public static Color BackgroundColor { get; internal set; }
			public static Color SelectionBackgroundColor { get; internal set; }
			public static Color ResultTextColor { get; internal set; }
			public static Color ResultDescriptionTextColor { get; internal set; }
			public static Color ResultMatchTextColor { get; internal set; }
			public static Color SelectedResultTextColor { get; internal set; }
			public static Color SelectedResultDescriptionTextColor { get; internal set; }
			public static Color SelectedResultMatchTextColor { get; internal set; }
		}

		// New Project Dialog

		public static class NewProjectDialog
		{
			public static Color BannerBackgroundColor { get; internal set; }
			public static Color BannerLineColor { get; internal set; }
			public static Color BannerForegroundColor { get; internal set; }
			public static Color CategoriesBackgroundColor { get; internal set; }
			public static Color TemplateListBackgroundColor { get; internal set; }
			public static Color TemplateBackgroundColor { get; internal set; }
			public static Color TemplateSectionSeparatorColor { get; internal set; }
			public static Color TemplateLanguageButtonBackground { get; internal set; }
			public static Color TemplateLanguageButtonTriangle { get; internal set; }
			public static Color ProjectConfigurationLeftHandBackgroundColor { get; internal set; }
			public static Color ProjectConfigurationRightHandBackgroundColor { get; internal set; }
			public static Color ProjectConfigurationPreviewLabelColor { get; internal set; }
			public static Color ProjectConfigurationSeparatorColor { get; internal set; }
		}

		// Wizards

		public static class Wizard
		{
			public static Color BannerBackgroundColor { get; internal set; }
			public static Color BannerShadowColor { get; internal set; }
			public static Color BannerForegroundColor { get; internal set; }
			public static Color BannerSecondaryForegroundColor { get; internal set; }
			public static Color PageBackgroundColor { get; internal set; }
			public static Color PageSeparatorColor { get; internal set; }
			public static Color RightSideBackgroundColor { get; internal set; }
			public static Color ContentShadowColor { get; internal set; }
			public static Color ContentSeparatorColor { get; internal set; }
		}


		// Editor

		public static class Editor
		{
			public static Color SmartTagMarkerColorLight { get; internal set; }
			public static Color SmartTagMarkerColorDark { get; internal set; }
			public static Color SearchErrorForegroundColor { get; internal set; }
			public static Color SearchMarkerFallbackColor { get; internal set; }
			public static Color SearchMarkerSelectedFallbackColor { get; internal set; }
		}

		public static class KeyBindingsPanel
		{
			public static Color KeyBackgroundColor { get; internal set; }
			public static Color KeyForegroundColor { get; internal set; }
			public static Color KeyDuplicateBackgroundColor { get; internal set; }
			public static Color KeyDuplicateForegroundColor { get; internal set; }
			public static Color KeyConflictBackgroundColor { get; internal set; }
			public static Color KeyConflictForegroundColor { get; internal set; }
		}

		// Notification bar
		internal static class NotificationBar
		{
			public static Color BarBackgroundColor { get; internal set; }
			public static Color ButtonLabelColor { get; internal set; }
			public static Color BarBorderColor { get; } = CairoExtensions.ParseColor("#000000", 0.1);
		}

		// Helper methods

		internal static Color Shift (Color color, double factor)
		{
			return new Color (color.R * factor, color.G * factor, color.B * factor, color.A);
		}

#if false
		internal static Color MidColor (double factor)
		{
			return BaseBackgroundColor.BlendWith (BaseForegroundColor, factor);
		}

		internal static Color ReduceLight (Color color, double factor)
		{
			color.Light *= factor;
			return color;
		}

		internal static Color IncreaseLight (Color color, double factor)
		{
			color.Light += (1 - color.Light) * factor;
			return color;
		}
#endif

		public static string ColorGetHex (Color color, bool withAlpha = false)
		{
			if (withAlpha) {
				return String.Format("#{0:x2}{1:x2}{2:x2}{3:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
				                     (byte)(color.B * 255), (byte)(color.A * 255));
			} else {
				return String.Format("#{0:x2}{1:x2}{2:x2}", (byte)(color.R * 255), (byte)(color.G * 255),
				                     (byte)(color.B * 255));
			}
		}

		static Styles ()
		{
			if (Platform.IsWindows)
				TooltipInfoSpacing = 0;
			else
				TooltipInfoSpacing = -4;
			LoadStyle ();
		}

		internal static void LoadStyle ()
		{
			// Pinta TODO
#if false
			Gtk.Style defaultStyle;
			Gtk.Widget styledWidget;
			if (IdeApp.Workbench == null || IdeApp.Workbench.RootWindow == null) {
				styledWidget = new Gtk.Label (String.Empty);
				defaultStyle = styledWidget.Style;
            } else {
				styledWidget = IdeApp.Workbench.RootWindow;
				defaultStyle = Gtk.Rc.GetStyle (styledWidget);
			}

			BackgroundColor = defaultStyle.Background (Gtk.StateType.Normal).ToXwtColor ();	// must be the bg color from Gtkrc
			BaseBackgroundColor = defaultStyle.Base (Gtk.StateType.Normal).ToXwtColor ();	// must be the base color from Gtkrc
			BaseForegroundColor = defaultStyle.Foreground (Gtk.StateType.Normal).ToXwtColor ();	// must be the text color from Gtkrc
			BaseSelectionBackgroundColor = defaultStyle.Base (Gtk.StateType.Selected).ToXwtColor ();
			BaseSelectionTextColor = defaultStyle.Text (Gtk.StateType.Selected).ToXwtColor ();

			LinkForegroundColor = ((Gdk.Color)styledWidget.StyleGetProperty ("link-color")).ToXwtColor ();
			if (LinkForegroundColor == Colors.Black) // the style returs black when not initialized
				LinkForegroundColor = Colors.Blue;   // set the link color to generic blue until initialization is finished

			DefaultFont = defaultStyle.FontDescription.Copy ();
			DefaultFontName = DefaultFont.ToString ();
#endif

#if false
			if (IdeApp.Preferences == null || IdeApp.Preferences.UserInterfaceTheme == Theme.Light)
				LoadLightStyle ();
			else
				LoadDarkStyle ();
#else
			LoadLightStyle();
#endif

			// Shared colors

			DockBarLabelColor = BaseIconColor;
			DockSeparatorColor = DockFrameBackground;
			PropertyPadLabelBackgroundColor = PrimaryBackgroundColor;
			PadCategoryBorderColor = SeparatorColor;
			PadCategoryLabelColor = BaseForegroundColor;
			PadCategoryBackgroundColor = SecondaryBackgroundLighterColor;
			PadLabelColor = BaseForegroundColor;
			SubTabBarActiveBackgroundColor = BaseSelectionBackgroundColor;
			SubTabBarActiveTextColor = BaseSelectionTextColor;
			SubTabBarSeparatorColor = ThinSplitterColor;
			InactiveBrowserPadBackground = InactivePadBackground;

			// Tabs

			TabBarBackgroundColor = DockFrameBackground;
			TabBarInactiveTextColor = InactivePadLabelColor;
			TabBarActiveTextColor = BaseForegroundColor;

			// Breadcrumbs

			BreadcrumbTextColor = BaseForegroundColor;

			// Document Subview Tabs

			SubTabBarTextColor = BaseForegroundColor;

			// Popover Window

			PopoverWindow.InformationBackgroundColor = StatusInformationBackgroundColor;
			PopoverWindow.InformationTextColor = StatusInformationTextColor;
			PopoverWindow.WarningBackgroundColor = StatusWarningBackgroundColor;
			PopoverWindow.WarningTextColor = StatusWarningTextColor;
			PopoverWindow.ErrorBackgroundColor = StatusErrorBackgroundColor;
			PopoverWindow.ErrorTextColor = StatusErrorTextColor;

			// Code Completion

			CodeCompletion.SelectionBackgroundColor = BaseSelectionBackgroundColor;
			CodeCompletion.SelectionTextColor = BaseSelectionTextColor;

			// Global Search

			GlobalSearch.BackgroundColor = PrimaryBackgroundColor;
			GlobalSearch.HeaderBackgroundColor = SecondaryBackgroundLighterColor;
			GlobalSearch.HeaderTextColor = SecondaryTextColor;
			GlobalSearch.SeparatorLineColor = SeparatorColor;
			GlobalSearch.SelectionBackgroundColor = BaseSelectionBackgroundColor;
			GlobalSearch.ResultTextColor = BaseForegroundColor;
			GlobalSearch.ResultDescriptionTextColor = SecondaryTextColor;
			GlobalSearch.SelectedResultTextColor = BaseSelectionTextColor;
			GlobalSearch.SelectedResultDescriptionTextColor = BaseSelectionTextColor;
			GlobalSearch.SelectedResultMatchTextColor = BaseSelectionTextColor;

			// New Project Dialog

			NewProjectDialog.TemplateBackgroundColor = PrimaryBackgroundColor;
			NewProjectDialog.TemplateLanguageButtonTriangle = BaseIconColor;
			NewProjectDialog.ProjectConfigurationPreviewLabelColor = BaseForegroundColor;
			NewProjectDialog.CategoriesBackgroundColor = SecondaryBackgroundDarkerColor;
			NewProjectDialog.ProjectConfigurationLeftHandBackgroundColor = SecondaryBackgroundDarkerColor;
			NewProjectDialog.ProjectConfigurationRightHandBackgroundColor = PrimaryBackgroundColor;

			// Wizards

			Wizard.PageBackgroundColor = SecondaryBackgroundDarkerColor;
			Wizard.RightSideBackgroundColor = PrimaryBackgroundColor;

			// Editor

			Editor.SmartTagMarkerColorLight = CairoExtensions.ParseColor("#ff70fe", 0.5);
			Editor.SmartTagMarkerColorDark = CairoExtensions.ParseColor("#ffffff", 0.5);
			Editor.SearchErrorForegroundColor = ErrorForegroundColor;
			Editor.SearchMarkerFallbackColor = CairoExtensions.ParseColor ("#f3da2d");
			Editor.SearchMarkerSelectedFallbackColor = CairoExtensions.ParseColor ("#ffaf45");

			// Key Bindings Preferences

			KeyBindingsPanel.KeyBackgroundColor = BackgroundColor;
			KeyBindingsPanel.KeyForegroundColor = BaseForegroundColor;
			KeyBindingsPanel.KeyDuplicateBackgroundColor = StatusWarningBackgroundColor;
			KeyBindingsPanel.KeyDuplicateForegroundColor = StatusWarningTextColor;
			KeyBindingsPanel.KeyConflictBackgroundColor = StatusErrorBackgroundColor;
			KeyBindingsPanel.KeyConflictForegroundColor = StatusErrorTextColor;

			// Tooltips
			StatusInformationBackgroundColor = CairoExtensions.ParseColor ("#eeeeee");
			StatusInformationTextColor = CairoExtensions.ParseColor ("#272727");
			InformationBoxForegroundColor = CairoExtensions.ParseColor ("#272727");

			if (Changed != null)
				Changed (null, EventArgs.Empty);
		}

		internal static void LoadLightStyle ()
		{
			BaseIconColor = CairoExtensions.ParseColor ("#575757");
			BorderColor = CairoExtensions.ParseColor ("#eeeeee");
			FrameBoxBorderColor = CairoExtensions.ParseColor ("#a3a3a3");
			ThinSplitterColor = CairoExtensions.ParseColor ("#dadada");
			SeparatorColor = CairoExtensions.ParseColor ("#f2f2f4");
			PrimaryBackgroundColor = BaseBackgroundColor;
			SecondaryBackgroundDarkerColor = CairoExtensions.ParseColor ("#e7eaee");
			SecondaryBackgroundLighterColor = CairoExtensions.ParseColor ("#f9f9fb");
			SecondaryTextColorHexString = "#767676";
			SecondaryTextColor = CairoExtensions.ParseColor (SecondaryTextColorHexString);
			SecondarySelectionTextColor = CairoExtensions.ParseColor ("#ffffff");
			PadBackground = CairoExtensions.ParseColor ("#fafafa");
			InactivePadBackground = CairoExtensions.ParseColor ("#e8e8e8");
			InactivePadLabelColor = CairoExtensions.ParseColor ("#777777");
			DockFrameBackground = CairoExtensions.ParseColor ("#bfbfbf");
			DockBarBackground = CairoExtensions.ParseColor ("#dddddd");
			DockBarPrelightColor = CairoExtensions.ParseColor ("#eeeeee");
			BrowserPadBackground = CairoExtensions.ParseColor ("#ebedf0");
			PropertyPadDividerColor = CairoExtensions.ParseColor ("#efefef");
			FocusColor = CairoExtensions.ParseColor ("#4b4b4b");

			// these colors need to match colors from status icons
			InformationBoxBackgroundColor = StatusInformationBackgroundColor;
			InformationForegroundColor = CairoExtensions.ParseColor ("#5785bd");

			StatusWarningBackgroundColor = CairoExtensions.ParseColor ("#ffb269");
			StatusWarningTextColor = CairoExtensions.ParseColor ("#000000");
			WarningBoxBackgroundColor = StatusWarningBackgroundColor;
			WarningBoxForegroundColor = CairoExtensions.ParseColor ("#000000");
			WarningForegroundColor = CairoExtensions.ParseColor ("#8a5522");

			StatusErrorBackgroundColor = CairoExtensions.ParseColor ("#c42c3e");
			StatusErrorTextColor = CairoExtensions.ParseColor ("#ffffff");
			ErrorBoxBackgroundColor = StatusErrorBackgroundColor;
			ErrorBoxForegroundColor = StatusErrorTextColor;
			ErrorForegroundColor = CairoExtensions.ParseColor ("#c42c3e");

			// Tabs

			TabBarNotifyTextColor = CairoExtensions.ParseColor ("#1FAECE");

			// Breadcrumb

			BreadcrumbBackgroundColor = PadBackground;
			BreadcrumbButtonFillColor = BaseSelectionBackgroundColor.WithAlpha (0.2);
			BreadcrumbBottomBorderColor = DockBarBackground;

			// Document Subview Tabs

			SubTabBarBackgroundColor = PadBackground;
			SubTabBarHoverBackgroundColor = BaseSelectionBackgroundColor.WithAlpha (0.2);

			// WidgetBorderColor = CairoExtensions.ParseColor ("#ff00ff"); // TODO: 8c8c8c - UNUSED (used for custom drawn `SearchEntry` but it isn’t used anymore, so its deprecated)

			// Status area (GTK)

			StatusBarBorderColor = CairoExtensions.ParseColor ("#919191");
			StatusBarFill1Color = CairoExtensions.ParseColor ("#fcfcfc");
			StatusBarFill2Color = CairoExtensions.ParseColor ("#f2f2f2");
			StatusBarFill3Color = CairoExtensions.ParseColor ("#ebebeb");
			StatusBarFill4Color = CairoExtensions.ParseColor ("#e8e8e8");
			StatusBarErrorColor = ErrorForegroundColor;
			StatusBarInnerColor = CairoExtensions.ParseColor ("#000000").WithAlpha (.08);
			StatusBarShadowColor1 = CairoExtensions.ParseColor ("#000000").WithAlpha (.06);
			StatusBarShadowColor2 = CairoExtensions.ParseColor ("#000000").WithAlpha (.02);
			StatusBarTextColor = BaseForegroundColor;
			StatusBarProgressBackgroundColor = CairoExtensions.ParseColor ("#000000").WithAlpha (.1);
			StatusBarProgressOutlineColor = CairoExtensions.ParseColor ("#000000").WithAlpha (.1);

			// Toolbar

			ToolbarBottomBorderColor = CairoExtensions.ParseColor ("#afafaf");

			// Global Search

			GlobalSearch.ResultMatchTextColor = CairoExtensions.ParseColor ("#4d4d4d");

			// Popover Window

			PopoverWindow.DefaultBackgroundColor = CairoExtensions.ParseColor ("#f2f2f2"); // gtkrc @tooltip_bg_color
			PopoverWindow.DefaultTextColor = CairoExtensions.ParseColor ("#555555");
			PopoverWindow.ShadowColor = CairoExtensions.ParseColor ("#000000").WithAlpha (.05);
#if false
			PopoverWindow.BorderColor = Colors.Transparent; // disable border drawing
#else
			PopoverWindow.BorderColor = new Color(0, 0, 0, 0);
#endif

			PopoverWindow.ParamaterWindows.GradientStartColor = CairoExtensions.ParseColor ("#fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = CairoExtensions.ParseColor ("#fffcd1");

			// Code Completion

			CodeCompletion.BackgroundColor = CairoExtensions.ParseColor ("#eef1f2");
			CodeCompletion.TextColor = CairoExtensions.ParseColor ("#646566");
			CodeCompletion.CategoryColor = SecondaryTextColor;
			CodeCompletion.HighlightColor = CairoExtensions.ParseColor ("#ba3373");
			CodeCompletion.SelectionBackgroundInactiveColor = CairoExtensions.ParseColor ("#7e96c0");
			CodeCompletion.SelectionHighlightColor = CodeCompletion.HighlightColor;

			// Wizards

			Wizard.BannerBackgroundColor = CairoExtensions.ParseColor ("#f5f5f5");
			Wizard.BannerShadowColor = CairoExtensions.ParseColor ("#e0e0e0");
			Wizard.BannerForegroundColor = CairoExtensions.ParseColor ("#6b6b6b");
			Wizard.BannerSecondaryForegroundColor = SecondaryTextColor;
			Wizard.PageSeparatorColor = ThinSplitterColor;
			Wizard.ContentSeparatorColor = CairoExtensions.ParseColor ("#d2d5d9");
			Wizard.ContentShadowColor = ThinSplitterColor;

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = Wizard.BannerBackgroundColor;
			NewProjectDialog.BannerLineColor = Wizard.BannerShadowColor;
			NewProjectDialog.BannerForegroundColor = Wizard.BannerForegroundColor;
			NewProjectDialog.TemplateListBackgroundColor = CairoExtensions.ParseColor ("#f9f9fa");
			NewProjectDialog.TemplateSectionSeparatorColor = CairoExtensions.ParseColor ("#e2e2e2");
			NewProjectDialog.TemplateLanguageButtonBackground = BaseBackgroundColor;
			NewProjectDialog.ProjectConfigurationSeparatorColor = CairoExtensions.ParseColor ("#d2d5d9");

			// Notification Bar

			NotificationBar.BarBackgroundColor = CairoExtensions.ParseColor ("#f3f3f3");
			NotificationBar.ButtonLabelColor = CairoExtensions.ParseColor ("#444444");
		}

		internal static void LoadDarkStyle ()
		{
			BaseIconColor = CairoExtensions.ParseColor ("#bfbfbf");
			BorderColor = CairoExtensions.ParseColor ("#2e2e2e");
			FrameBoxBorderColor = BorderColor;
			ThinSplitterColor = BorderColor;
			SeparatorColor = CairoExtensions.ParseColor ("#4b4b4b");
			PrimaryBackgroundColor = BaseBackgroundColor;
			SecondaryBackgroundDarkerColor = CairoExtensions.ParseColor ("#484848");
			SecondaryBackgroundLighterColor = SeparatorColor;
			SecondaryTextColorHexString = "#ababab";
			SecondaryTextColor = CairoExtensions.ParseColor (SecondaryTextColorHexString);
			SecondarySelectionTextColor = CairoExtensions.ParseColor ("#ffffff");
			PadBackground = CairoExtensions.ParseColor ("#525252");
			InactivePadBackground = CairoExtensions.ParseColor ("#474747");
			InactivePadLabelColor = CairoExtensions.ParseColor ("#999999");
			DockFrameBackground = CairoExtensions.ParseColor ("#303030");
			DockBarBackground = PadBackground;
			DockBarPrelightColor = CairoExtensions.ParseColor ("#666666");
			BrowserPadBackground = CairoExtensions.ParseColor ("#484b55");
			PropertyPadDividerColor = SeparatorColor;
			FocusColor = CairoExtensions.ParseColor ("#f2f2f4");

			// these colors need to match colors from status icons
			InformationBoxBackgroundColor = StatusInformationBackgroundColor;
			InformationForegroundColor = CairoExtensions.ParseColor ("#9cc8ff");

			StatusWarningBackgroundColor = CairoExtensions.ParseColor ("#ffb269");
			StatusWarningTextColor = CairoExtensions.ParseColor ("#000000");
			WarningBoxBackgroundColor = StatusWarningBackgroundColor;
			WarningBoxForegroundColor = CairoExtensions.ParseColor ("#000000");
			WarningForegroundColor = CairoExtensions.ParseColor ("#ffb269");

			StatusErrorBackgroundColor = CairoExtensions.ParseColor ("#ffb3bc");
			StatusErrorTextColor = CairoExtensions.ParseColor ("#000000");
			ErrorBoxBackgroundColor = StatusErrorBackgroundColor;
			ErrorBoxForegroundColor = StatusErrorTextColor;
			ErrorForegroundColor = StatusErrorTextColor;

			// Tabs

			TabBarNotifyTextColor = CairoExtensions.ParseColor ("#4FCAE6");

			// Breadcrumb

			BreadcrumbBackgroundColor = PadBackground;
			BreadcrumbButtonFillColor = SecondaryBackgroundLighterColor;
			BreadcrumbBottomBorderColor = BreadcrumbBackgroundColor;

			// Document Subview Tabs

			SubTabBarBackgroundColor = PadBackground;
			SubTabBarHoverBackgroundColor = SecondaryBackgroundLighterColor;

			// Status area (GTK)

			StatusBarBorderColor = CairoExtensions.ParseColor ("#222222");
			StatusBarFill1Color = CairoExtensions.ParseColor ("#282828");
			StatusBarFill2Color = CairoExtensions.ParseColor ("#000000").WithAlpha (0); 
			StatusBarFill3Color = CairoExtensions.ParseColor ("#000000").WithAlpha (0); 
			StatusBarFill4Color = CairoExtensions.ParseColor ("#222222");
			StatusBarErrorColor = ErrorForegroundColor;
			StatusBarInnerColor = CairoExtensions.ParseColor ("#000000").WithAlpha (.08);
			StatusBarShadowColor1 = CairoExtensions.ParseColor ("#000000").WithAlpha (.06);
			StatusBarShadowColor2 = CairoExtensions.ParseColor ("#000000").WithAlpha (.02);
			StatusBarTextColor = BaseForegroundColor;
			StatusBarProgressBackgroundColor = CairoExtensions.ParseColor ("#ffffff").WithAlpha (.1);
			StatusBarProgressOutlineColor = CairoExtensions.ParseColor ("#ffffff").WithAlpha (.1);

			// Toolbar

			ToolbarBottomBorderColor = CairoExtensions.ParseColor ("#444444");

			// Global Search

			GlobalSearch.ResultMatchTextColor = BaseSelectionTextColor;

			// Popover window

			PopoverWindow.DefaultBackgroundColor = CairoExtensions.ParseColor ("#5e5e5e");
			PopoverWindow.DefaultTextColor = CairoExtensions.ParseColor ("#bdc1c1");
			PopoverWindow.ShadowColor = CairoExtensions.ParseColor ("#000000").WithAlpha (0); // transparent since dark theme doesn't need shadows
#if false
			PopoverWindow.BorderColor = Colors.Transparent; // disable border drawing
#else
			PopoverWindow.BorderColor = new Color(0, 0, 0, 0);
#endif

			PopoverWindow.ParamaterWindows.GradientStartColor = CairoExtensions.ParseColor ("#fffee6");
			PopoverWindow.ParamaterWindows.GradientEndColor = CairoExtensions.ParseColor ("#fffcd1");

			// Code Completion

			CodeCompletion.BackgroundColor = PopoverWindow.DefaultBackgroundColor;
			CodeCompletion.TextColor = CairoExtensions.ParseColor ("#c3c5c6");
			CodeCompletion.CategoryColor = CairoExtensions.ParseColor ("#a1a1a1");
			CodeCompletion.HighlightColor = CairoExtensions.ParseColor ("#f9d33c");
			CodeCompletion.SelectionBackgroundInactiveColor = CairoExtensions.ParseColor ("#7e96c0");
			CodeCompletion.SelectionHighlightColor = CodeCompletion.HighlightColor;

			// Wizards

			Wizard.BannerBackgroundColor = CairoExtensions.ParseColor ("#333333");
			Wizard.BannerShadowColor = CairoExtensions.ParseColor ("#2e2e2e");
			Wizard.BannerForegroundColor = CairoExtensions.ParseColor ("#c2c2c2");
			Wizard.BannerSecondaryForegroundColor = SecondaryTextColor;
			Wizard.PageSeparatorColor = ThinSplitterColor;
			Wizard.ContentSeparatorColor = CairoExtensions.ParseColor ("#6e6e6e");
			Wizard.ContentShadowColor = ThinSplitterColor;

			// New Project Dialog

			NewProjectDialog.BannerBackgroundColor = Wizard.BannerBackgroundColor;
			NewProjectDialog.BannerLineColor = Wizard.BannerShadowColor;
			NewProjectDialog.BannerForegroundColor = Wizard.BannerForegroundColor;
			NewProjectDialog.TemplateListBackgroundColor = DockBarBackground;
			NewProjectDialog.TemplateSectionSeparatorColor = ThinSplitterColor;
			NewProjectDialog.TemplateLanguageButtonBackground = SecondaryBackgroundDarkerColor;
			NewProjectDialog.ProjectConfigurationSeparatorColor = CairoExtensions.ParseColor ("#6e6e6e");

			// Notification Bar

			NotificationBar.BarBackgroundColor = CairoExtensions.ParseColor ("#222222");
			NotificationBar.ButtonLabelColor = CairoExtensions.ParseColor ("#BEBEBE");

		}

#if false
		static StylesStringTagModel tagModel;

		public static IStringTagModel GetStringTagModel ()
		{
			if (tagModel == null)
				tagModel = new StylesStringTagModel ();
			return tagModel;
		}
#endif
	}
}

