# Change Log
All notable changes to this project (beginning with version 1.7) will be documented in this file.

## [Unreleased](https://github.com/PintaProject/Pinta/compare/2.1.2...HEAD)

Thanks to the following contributors who worked on this release:
- @cameronwhite
- @evgeniy-harchenko
- @yarikoptic
- @Lehonti
- @zWolfrost
- @ptixed
- @logiclrd
- @Matthieu-LAURENT39
- @khoidauminh
- @potatoes1286
- @TheodorLasse
- @f-i-l-i-p
- @solarnomad7

### Added
- Ported to GTK4 and libadwaita
- Upgraded the minimum required .NET version to 8.0
- Added an arm64 installer for macOS (Apple silicon)
- Restored support for add-ins, which had been disabled in Pinta 2.0 due to technical limitations
- Added a preference (in the `View` menu) for switching between a dark or light color scheme
- Added an improved color picker dialog (#570, #761, #1025)
- Added a canvas grid with customizable size, replacing the previous pixel grid (#1028, #1105)
- Added ability to choose tile type (#1051) and edge behavior (#1141) in tile reflection effect
- Added a new "Dithering" effect (#457)
- Added "Voronoi Diagram" effect (#692)
- Ported "Vignette" and "Dents" effects from Paint.NET 3.36 (#881, #885)
- Added "Feather Object" effect (#886, #953)
- Added "Align Object" effect (#936, #961)
- Added "Outline Object" effect (#971)
- Added support for exporting to portable pixmap (`.ppm`) files (#549)
- Added a nearest-neighbor resampling mode when resizing images (#596)
- Added support for customizable gradients in the fractal and clouds effects (#578, #678, #683)
- Added a new `Offset Selection` option to the `Edit` menu to expand or contract the current selection (#661, #740, #746)
- The Windows build of Pinta now supports loading `.webp` images (#770)
- Improved zooming behavior with trackpads, including support for the pinch to zoom gesture (#634, #715)
- The Windows installer is now signed, thanks to the support of [SignPath](https://about.signpath.io/) (#1054)
- The brush size and line width settings for many tools can now be adjusted with the `[` and `]` keyboard shortcuts (#796, #1155)

### Changed
- Due to API changes in GTK4, the File -> New Screenshot option now invokes platform-specific tools (the XDG screenshot portal on Linux, and the screenshot tool on maCOS). This is currently unsupported on Windows
- When building Pinta using the Makefile, 'dotnet publish' is now run during the build step rather than the install step.
- Added a "Reseed" button for the random noise used by several effects (such as "Add Noise" and "Frosted Glass").  Previously, the noise pattern changed every time the effect was computed (including when other parameters were changed).
- Saving an image already saved in a format that supports multiple layers to a format that does not support layers will now explicitly prompt the user to flatten the image before saving, rather than silently flattening it (#909)
- The add-in manager dialog now filters out old versions incompatible with the current version of Pinta, or new addins requiring future version of Pinta ([#1580205](https://bugs.launchpad.net/pinta/+bug/1580205))
- The tool windows on the right side of the dock layout can now be completely hidden (#1179)

### Fixed
- Twist effect applied locally based on selection instead of entire image (#1089)
- Zoom blur effect now zooms inside the image's bounds instead of way outside of them (#1125)
- Fixed issues where the system language settings on macOS did not properly take effect in Pinta ([#1976178](https://bugs.launchpad.net/pinta/+bug/1976178))
- Fixed an issue where the Pan tool's cursor could show up as a missing icon ([#2013047](https://bugs.launchpad.net/pinta/+bug/2013047))
- Fixed errors when saving a file that was opened with a missing or incorrect extension ([#2013050](https://bugs.launchpad.net/pinta/+bug/2013050))
- Fixed a bug where certain layer opacity settings could be incorrectly rounded ([#2020596](https://bugs.launchpad.net/pinta/+bug/2020596))
- Fixed bugs in the shape tools and the Lasso Select tool which prevented the last row and column of the image from being used (#467)
- Fixed issues where the Curves dialog could not easily edit existing control points ([#1973602](https://bugs.launchpad.net/pinta/+bug/1973602))
- Fixed a bug where dragging a control point in the Curves dialog could unexpectedly erase other control points ([#1973602](https://bugs.launchpad.net/pinta/+bug/1973602))
- Improved error handling when loading incompatible add-ins ([#2047274](https://bugs.launchpad.net/pinta/+bug/2047274))
- The Clone Stamp tool no longer resets the destination offset after each stroke ([#2031257](https://bugs.launchpad.net/pinta/+bug/2031257))
- Fixed potential errors when pasting in the text tool if the clipboard didn't contain text ([#2047495](https://bugs.launchpad.net/pinta/+bug/2047495))
- The text tool now supports pre-editing to display the intermediate characters entered by an input method ([#2047445](https://bugs.launchpad.net/pinta/+bug/2047445))
- Fixed layout issues in the effect dialogs ([#2049937](https://bugs.launchpad.net/pinta/+bug/2049937))
- Fixed a bug where the Flip Horizontal / Vertical items in the Image menu incorrectly activated the Layer menu's flip actions ([#2051430](https://bugs.launchpad.net/pinta/+bug/2051430))
- Fixed a bug where the `uninstall` Makefile target did not remove icons (#792)
- Fixed a potential crash on some platforms when entering characters in the text tool using an input method (#722)
- The angle picker widget now supports fractional angles (#807)
- Fixed issues with restoring saved settings in the Eraser tool (#839)
- Fixed dragging issues in the Curves adjustment dialog with modifiers such as Num Lock active (#871)
- Fixed a bug where the file picker dialogue would open the wrong directory after a failed save (#914)
- Fixed unexpected drawing behavior when using semi-transparent colors with the Paint Brush tool (#941)
- The Text tool now supports configuring whether antialiasing is enabled (#935)
- Fixed various artifacts in the shape tools, particularly with larger brush widths (#733, #955)
- Fixed an issue where the text tool did not immediately redraw after changes to the font or color of unfinalized text (#952, #975)
- Fixed an issue where the text tool could unexpectedly redraw existing text with the latest palette color, or fail to finalize uncommitted text (#1097, #1176)
- Fixed a bug where cut / paste operations did not behave as expected with complex selections (#951, #978)
- Fixed transparency behavior for several effects (#1184)

## [2.1.2](https://github.com/PintaProject/Pinta/releases/tag/2.1.2) - 2024/04/20

Thanks to the following contributors who worked on this release:
- @cameronwhite

### Added

### Changed
- Support building against .NET 8 (replacing .NET 7) in addition to .NET 6

### Fixed
- Fixed AppStream validation errors in `xdg/pinta.appdata.xml`
- Fixed issues where the system language settings on macOS did not properly take effect in Pinta ([#1976178](https://bugs.launchpad.net/pinta/+bug/1976178))
- Fixed issues on macOS with loading webp images (#770)

## [2.1.1](https://github.com/PintaProject/Pinta/releases/tag/2.1.1) - 2023/02/26

Thanks to the following contributors who worked on this release:
- @cameronwhite

### Changed
- Updated translations

### Fixed
- Fixed a bug where the Save As file dialog failed to open when using the file chooser portal (e.g. for sandboxed Snap or Flatpak packages), if the image's file type did not support exporting (e.g. SVG) ([#2002021](https://bugs.launchpad.net/pinta/+bug/2002021))
- Fixed a potential error when opening or closing Pinta, if the last dialog directory setting was an empty string ([#2002188](https://bugs.launchpad.net/pinta/+bug/2002188), [#2001734](https://bugs.launchpad.net/pinta/+bug/2001734))
- Fixed error messages when dragging and dropping to open a file ([#2003384](https://bugs.launchpad.net/pinta/+bug/2003384))
- Fixed an issue where the tab labels could not shrink, limiting the minimum size of the window ([#2006572](https://bugs.launchpad.net/pinta/+bug/2006572))
- Fixed errors on Windows when opening non-ASCII file paths through the "Open With" context menu ([#2006974](https://bugs.launchpad.net/pinta/+bug/2006974))
- Improved error handling in the add-in manager dialog ([#1419283](https://bugs.launchpad.net/pinta/+bug/1419283))

## [2.1](https://github.com/PintaProject/Pinta/releases/tag/2.1) - 2023/01/03

Thanks to the following contributors who worked on this release:
- @cameronwhite
- @jpobst
- @JanDeDinoMan
- @MrCarroll
- @supershadoe
- @Zekiah-A
- @Zeti123

### Added
- Updated icons to symbolic SVG icons, which are more usable for dark themes and high-res screens (#204, #207, [#1738106](https://bugs.launchpad.net/pinta/+bug/1738106), [#1909573](https://bugs.launchpad.net/pinta/+bug/1909573))
- Added "Transparency Mode" to the Gradient tool
- The selection move handles and shape control point handles are now much easier to use when working on zoomed in or small images (#211, #223, [#1173756](https://bugs.launchpad.net/pinta/+bug/1173756), [#1958924](https://bugs.launchpad.net/pinta/+bug/1958924), [#1958920](https://bugs.launchpad.net/pinta/+bug/1958920))
- The File -> New Screenshot command now uses the XDG screenshot portal if available. This fixes issues with taking screenshots under Wayland (#218, [#1955841](https://bugs.launchpad.net/pinta/+bug/1955841))
- Improved canvas rendering performance (#209, #210)
- Added support for loading files from virtual filesystems such as Google Drive mounts (#215, [#1958763](https://bugs.launchpad.net/pinta/+bug/1958763))
- Improved support for `.ora` files
  - Hidden layers are now round-tripped correctly for `.ora` files ([#1377566](https://bugs.launchpad.net/pinta/+bug/1377566))
  - When saving a `.ora` file, a flattened image (`mergedimage.png`) is now included in the archive. This is required by the spec to accommodate viewer software ([#1377566](https://bugs.launchpad.net/pinta/+bug/1377566))
- Image or palette files that have an unknown extension but have valid contents can now be loaded ([#1679570](https://bugs.launchpad.net/pinta/+bug/1679570))
  - The file dialog also now uses MIME types on Linux and macOS, allowing valid image files with unknown extensions to be included in the image file filter (#216)
- Updated the application icon (#220)
- WebP support
  - For Linux users, [webp-pixbuf-loader](https://github.com/aruiz/webp-pixbuf-loader/) is now a suggested dependency to enable WebP support in Pinta
  - `webp-pixbuf-loader` is now included with the macOS package for WebP support
- Upgraded to .NET 7
  - Building against .NET 6 (LTS) is still supported. When building from the tarball, .NET 6 will be used if .NET 7 is unavailable

### Changed
- Pinta now uses the standard GTK about dialog
- The Line / Curve tool no longer requires pressing Ctrl to start drawing a shape when the mouse is outside the canvas ([#1999997](https://bugs.launchpad.net/pinta/+bug/1999997))

### Fixed
- Fixed a bug where the default linear gradient was reflected rather than clamped
- The gradient tool now updates correctly when drawing transparent colors. Previously, old results were visible under the transparent color ([#1937942](https://bugs.launchpad.net/pinta/+bug/1937942))
- The history panel is now more readable when a dark theme is used (#207)
- Fixed an issue where the Cairo surface for live effect previews was not always disposed (#206)
- Fixed errors that could occur if a selection existed but had zero area (e.g. after inverting a full selection) ([#1754440](https://bugs.launchpad.net/pinta/+bug/1754440))
- Fixed an issue on Windows where the ruler's text did not render correctly (#212)
- Fixed a regression from Pinta 2.0 where the rulers did not draw a marker for the current mouse position (#214)
- Improved the zoom tool's rectangle zoom when working with smaller images
- Fixed a potential crash when opening / adding an image after actions in the layer list panel ([#1959598](https://bugs.launchpad.net/pinta/+bug/1959598))
- Fixed an issue where the "All Files" filter in the Open File dialog ignored files with no extension, and did not work in the macOS native file chooser ([#1958670](https://bugs.launchpad.net/pinta/+bug/1958670), [#1679570](https://bugs.launchpad.net/pinta/+bug/1679570))
- Fixed a crash in the text tool when pressing Ctrl+X without a selection (#219, [#1964398](https://bugs.launchpad.net/pinta/+bug/1964398))
- The application icon now appears correctly on KDE Wayland sessions [#1967687](https://bugs.launchpad.net/pinta/+bug/1967687)
- Fixed an issue where the selection could be invalid after undoing a rotation [#1975864](https://bugs.launchpad.net/pinta/+bug/1975864)
- Fixed a bug where the selected layer could be changed while updating the layer list widget [#1965101](https://bugs.launchpad.net/pinta/+bug/1965101)
- Fixed a bug in the shape tools where changing the shape type did not always take effect (#235, #238, [#1993332](https://bugs.launchpad.net/pinta/+bug/1993332), [#1635902](https://bugs.launchpad.net/pinta/+bug/1635902))
- Fixed an issue on macOS where toolbar drop down button menus items could not be selected properly
- Pinta now always saves files to the exact file name chosen by the native file dialog, without e.g. appending a default extension. This fixes issues where files saved through desktop portals could be lost ([#1958670](https://bugs.launchpad.net/pinta/+bug/1958670))
- Fixed a bug where an empty dash pattern resulted in nothing being drawn ([#1973706](https://bugs.launchpad.net/pinta/+bug/1973706))
- Fixed issues where certain dash patterns did not draw correctly ([#1959032](https://bugs.launchpad.net/pinta/+bug/1959032))
- Fixed a bug where pasting into a new image could occasionally zoom the new image to 1% ([#1959673](https://bugs.launchpad.net/pinta/+bug/1959673))

## [2.0.2](https://github.com/PintaProject/Pinta/compare/2.0.2...HEAD) - 2022/01/13

Thanks to the following contributors who worked on this release:
- @cameronwhite

### Changed
- Updated translations

### Fixed
- When the Windows installer is run in silent mode, fixed an issue where Pinta was automatically launched after installation
- Fixed a macOS packaging issue that caused copy/paste operations to fail ([#1957814](https://bugs.launchpad.net/pinta/+bug/1957814))

## [2.0.1](https://github.com/PintaProject/Pinta/releases/tag/2.0.1) - 2022/01/06

Thanks to the following contributors who worked on this release:
- @cameronwhite
- @jpobst

### Changed
- Some of the less frequently used items in the View menu (e.g. hiding the toolbar or status bar) are now grouped in their own submenu (#203)
- Updated translations

### Fixed
- Fixed a missing file (`installer/linux/install.proj`) from the release tarball that caused the `install` build step to fail
- Fixed a bug where opening a large number of tabs could cause the window's width to expand ([#1956182](https://bugs.launchpad.net/pinta/+bug/1956182))
- Fixed a bug that could cause a border to appear around the image when zoomed in far enough to require scrolling
- Fixed an issue where tool shortcuts could be affected by Num Lock being enabled ([#1093935](https://bugs.launchpad.net/pinta/+bug/1093935))
- Fixed an issue where the credits text in the About dialog was aligned incorrectly ([#1956168](https://bugs.launchpad.net/pinta/+bug/1956168))

## [2.0](https://github.com/PintaProject/Pinta/releases/tag/2.0) - 2021/12/31

Thanks to the following contributors who worked on this release:
- @cameronwhite
- @jpobst
- @darkdragon-001
- @thekolian1996
- @iangzh

### Added
- Ported to GTK3 and .NET 6
  - Many changes to the appearance of standard GTK widgets and dialogs (e.g. the color picker and file dialogs). GTK3 themes should also now be supported.
  - Improved support for high-DPI displays.
  - The platform-native file dialogs are now used ([#1909807](https://bugs.launchpad.net/pinta/+bug/1909807), [#1909664](https://bugs.launchpad.net/pinta/+bug/1909664)).
  - A GTK version of 3.24.21 or higher is recommended. Earlier versions have a bug with changing the file filter in the Save As dialog ([#1909807](https://bugs.launchpad.net/pinta/+bug/1909807)).
  - On macOS, the menu now appears in the global menu bar instead of the application window.
  - Changed the text tool to use the standard GTK font chooser widget ([#1311873](https://bugs.launchpad.net/pinta/+bug/1311873), [#1866653](https://bugs.launchpad.net/pinta/+bug/1866653), [#890589](https://bugs.launchpad.net/pinta/+bug/890589))
  - Changed several tools to use spin buttons rather than editable combo boxes for e.g. selecting brush sizes ([#1186516](https://bugs.launchpad.net/pinta/+bug/1186516)).
  - The Open Recent menu item was deprecated in GTK3 and has been removed, but similar functionality is available in the file dialog's Recent section.
  - Support for add-ins has been removed, but may return in a future release ([#1918039](https://bugs.launchpad.net/pinta/+bug/1918039)).
  - The Windows and macOS installers now bundle all necessary dependencies. Separately installing GTK and .NET / Mono is no longer required.

- Added a status bar widget containing the position / selection information, zoom, and the color palette (#154)
- Changed the tool palette to be a single column (#155)
- Added recently used colors to the color palette widget (#154)
- Tools now save their settings for the next time Pinta is opened (#178).
- The primary and secondary palette colors are now saved in the application settings (#171).
- The canvas can now be panned by clicking and dragging with the middle mouse button (#176, [#419](https://communiroo.com/PintaProject/pinta/suggestions/419), [#1883629](https://bugs.launchpad.net/pinta/+bug/1883629)).
- On macOS, keyboard shortcuts now use Command instead of Ctrl.
- The macOS installers are now signed and notarized.

### Changed
- The Paste Into New Image action no longer creates several unnecessary history items (#170).
- Performance improvements for the paint bucket and magic wand tools (#159).
- Performance improvements for the selection tools when interactively adjusting the selection.
- Removed the Images pad, which is obsolete now that tabs are used (#153).

### Fixed
- Fixed several Unicode-related issues in the text tool ([#1422445](https://bugs.launchpad.net/pinta/+bug/1422445)).
- Fixed issue on macOS where Pinta could launch in the wrong language ([#1900310](https://bugs.launchpad.net/pinta/+bug/1900310)).
- Improved the UX of the Close and Save As confirmation dialogs ([#1909576](https://bugs.launchpad.net/pinta/+bug/1909576), [#1909688](https://bugs.launchpad.net/pinta/+bug/1909688)).
- Fixed a bug where the Pan tool did not work if scrolling could only occur in the Y direction ([#1909910](https://bugs.launchpad.net/pinta/+bug/1909910)).
- Fixed issues where the zoom level was not maintained when resizing an image ([#1889673](https://bugs.launchpad.net/pinta/+bug/1889673)).
- Fixed an issue where opening a file URI from the command line did not work ([#1908806](https://bugs.launchpad.net/pinta/+bug/1908806)).
- Fixed an issue where hiding a layer could leave the selection still visible ([#1907987](https://bugs.launchpad.net/pinta/+bug/1907987)).
- Fixed issues with the text tool and certain input methods (#200, [#1350349](https://bugs.launchpad.net/pinta/+bug/1350349)).
- Fixed an issue where the text tool's font settings were incorrect after undo operations (#201, [#1910495](https://bugs.launchpad.net/pinta/+bug/1910495)).
- Fixed the Copy Merged action's behaviour for non-rectangular selections ([#1363388](https://bugs.launchpad.net/pinta/+bug/1363388)).

## [1.7.1](https://github.com/PintaProject/Pinta/releases/tag/1.7.1) - 2021/11/20

Thanks to the following contributors who worked on this release:
- @cameronwhite
- @jpobst
- @darkdragon-001
- @JamiKettunen
- @thekolian1996
- @tdgroot
- @nikita-yfh
- @pikachuiscool2
- @yaminb
- @dandv
- @City-busz
- @jefetienne

### Added
- The canvas can now be scrolled horizontally by holding Shift while using the mouse wheel (#141)
- The primary and secondary palette colors can now be swapped by pressing X (#147)
- Added a more user-friendly dialog when attempting to open an unsupported file format (#143, [#1856821](https://bugs.launchpad.net/pinta/+bug/1856821))
- Zooming in and out can now be done without pressing the Ctrl key (#150).
- Arrow keys can be used to move by a single pixel in the Move Selected Pixels and Move Selection tools ([#1906141](https://bugs.launchpad.net/pinta/+bug/1906141)).
- Shift can now be used to constrain to a uniform scale when scaling using the Move Selected Pixels tool (#138).
- The About dialog now allows easily copying the version information to the clipboard for use when reporting bugs ([#1924249](https://bugs.launchpad.net/pinta/+bug/1924249)).

### Changed
- Fixed inconsistent behavior when switching between tools that share the same shortcut, such as the selection tools (#144, [#1558767](https://bugs.launchpad.net/pinta/+bug/1558767))
- Improved error messages when the user does not have read or write permissions for a file ([#1715150](https://bugs.launchpad.net/pinta/+bug/1715150)).
- The appdata file is now installed to `/usr/share/metainfo` instead of the legacy path `/usr/share/appdata` (#186).
- Tooltips for tabs now show the full file path instead of only the file name (#187).

### Fixed
- Fixed a bug where Auto Crop could incorrectly remove an additional pixel on the bottom and right side of the image. ([#1191390](https://bugs.launchpad.net/pinta/+bug/1191390)).
- Fixed a bug where drawing a single pixel with the Pencil tool used black instead of the palette color ([#1897245](https://bugs.launchpad.net/pinta/+bug/1897245)).
- Fixed issues with the zoom controls when using a French locale ([#1464855](https://bugs.launchpad.net/pinta/+bug/1464855))
- Fixed invalid URLs in `pinta.appdata.xml` (#140, #145)
- Added missing release notes to `pinta.appdata.xml` (#142)
- Fixed a regression introduced in Pinta 1.7 that could produce blurred pixels when using the Move Selected Pixels tool ([#1904304](https://bugs.launchpad.net/pinta/+bug/1904304)).
- Fixed a bug where the Rotate / Zoom Layer dialog could leave the layer in a state where all future actions were also transformed ([#1905176](https://bugs.launchpad.net/pinta/+bug/1905176)).
- Fixed a bug where the document might not be marked as modified after certain undo / redo actions ([#1905165](https://bugs.launchpad.net/pinta/+bug/1905165)).
- Fixed a bug where the Move Selected Pixels tool did not handle transparent pixels correctly ([#1905706](https://bugs.launchpad.net/pinta/+bug/1905706)).
- Fixed a bug where deselecting via a single click in the select tool could cause bugs with undoing earlier history items ([#1905719](https://bugs.launchpad.net/pinta/+bug/1905719)).
- Fixed a bug where the initial corner of a rectangle shape could be cut off ([#1922470](https://bugs.launchpad.net/pinta/+bug/1922470)).
- Fixed a bug where the text tool was not correctly clipped against the selection ([#1910511](https://bugs.launchpad.net/pinta/+bug/1910511)).
- Improved handling of memory allocation failures for large images ([#776346](https://bugs.launchpad.net/pinta/+bug/776346)).
- Fixed a bug where the shape tools did not redraw after changes to the fill style until the cursor entered the canvas ([#1937921](https://bugs.launchpad.net/pinta/+bug/1937921)).
- Fixed a crash when opening an invalid palette file (#146, [#1890450](https://bugs.launchpad.net/pinta/+bug/1890450)).

## [1.7](https://github.com/PintaProject/Pinta/releases/tag/1.7) - 2020/08/04

Thanks to the following contributors who worked on this release:
- @cameronwhite
- @jpobst
- @don-mccomb
- @jeneira94
- @akaro2424
- @anadvu
- @miguelfazenda
- @skkestrel
- @codeprof
- @hasufell
- @Mailaender
- @averissimo
- @tdaffin
- @Shuunen
- @jkells
- @scx
- @albfan
- @rajter
- @dandv
- @jaburns
- @aivel

### Added
- A new [user guide](https://pinta-project.com/user-guide/) has been written for the Pinta website! Thanks to @jeneira94, @akaro2424, and @anadvu for their contributions!
- Added a tab view to switch between images. The tabs can also be docked side-by-side or pulled into new windows. (#94).
- The Rotate / Zoom dialog now supports zooming and panning ([#1252756](https://bugs.launchpad.net/pinta/+bug/1252756)).
- Added a Smooth Erase tool, which is enabled using the Type menu on the Erase tool's toolbar (#110).
- The Pencil tool can switch between different blend modes (#124, [#1688743](https://bugs.launchpad.net/pinta/+bug/1688743)).
- Added support for JASC PaintShop Pro palette files (#126).
- The transform tools can now rotate in fixed increments by holding Shift (#134).
- The Move Selected tool can now scale by holding Ctrl (#138).
- Dragging and dropping a URL (e.g. image from a web browser) to download and open the image is now supported (#80, [#644123](https://bugs.launchpad.net/pinta/+bug/644123)).
- Performance improvements when interacting with selections, particularly for large images ([#1428740](https://bugs.launchpad.net/pinta/+bug/1428740)).
- The Rectangle Select tool now shows different arrow cursors at each corner of the selection ([#1188143](https://bugs.launchpad.net/pinta/+bug/1188143)).
- Added an AppData file for integration with some Linux app stores (#121).

### Changed
- .NET 4.5 / Mono 4.0 are now required.
- Mono 6.x is strongly recommended for [Mac](https://www.mono-project.com/download/stable/#download-mac) and [Linux](https://bugs.launchpad.net/pinta/+bug/1877235) users.
- UI improvements to the New Image dialog (#99, [[1424547](https://bugs.launchpad.net/pinta/+bug/1424547)).
- The Rotate / Zoom dialog now rotates in-place instead of changing the layer's size.
- Cairo blend operations are now used instead of PDN's managed blend modes (#98, [#1248933](https://bugs.launchpad.net/pinta/+bug/1248933), [#1091910](https://bugs.launchpad.net/pinta/+bug/1091910)).
- The tool windows can now only be closed with the View -> Tool Windows menu, as it was easy to accidentally close them without knowing how to recover them ([#1428720](https://bugs.launchpad.net/pinta/+bug/1428720)).
- The shortcut for the Intersect selection mode is now Alt + Left Click instead of using Shift, which had caused conflicts with holding Shift to constrain the selection to a square ([#1426660](https://bugs.launchpad.net/pinta/+bug/1426660)).

### Fixed
- Fixed many issues where selection changes did not update correctly ([#1438022](https://bugs.launchpad.net/pinta/+bug/1438022), [#1188924](https://bugs.launchpad.net/pinta/+bug/1188924), [#1429830](https://bugs.launchpad.net/pinta/+bug/1429830), [#1098137](https://bugs.launchpad.net/pinta/+bug/1098137), #105).
- Fixed incorrect behaviour when using the Shift key to constrain to a square or circle in the Rectangle and Ellipse tools ([#1452607](https://bugs.launchpad.net/pinta/+bug/1452607)).
- The option to expand the canvas when pasting an image now only changes the canvas size in the dimension where the pasted image is larger ([#1883623](https://bugs.launchpad.net/pinta/+bug/1883623)).
- Fixed a bug where Auto Crop used the current layer instead of the entire image when deciding what to crop, and takes the selection into account ([#1434928](https://bugs.launchpad.net/pinta/+bug/1434928), [#1434906](https://bugs.launchpad.net/pinta/+bug/1434906)).
- Fixed potential crashes when switching tools without any open documents ([#1425612](https://bugs.launchpad.net/pinta/+bug/1425612)).
- Fixed a potential bug where the OK button in the New Image dialog could be incorrectly disabled ([#1430203](https://bugs.launchpad.net/pinta/+bug/1430203)).
- Fixed a crash when clicking on the Open Images pad after closing all images ([#1430789](https://bugs.launchpad.net/pinta/+bug/1430789)).
- Fixed a bug where the Levels dialog closed unexpectedly when clicking on one of the color checkboxes ([#1435045](https://bugs.launchpad.net/pinta/+bug/1435045)).
- The outline width settings on the Text Tool's toolbar now only show up if they are relevant to the stroke style being used ([#1426663](https://bugs.launchpad.net/pinta/+bug/1426663)).
- Fixed a potential crash creating gradients ([#1446217](https://bugs.launchpad.net/pinta/+bug/1446217)).
- Fixed issues where the selection handles disappeared after pressing Delete ([#1424629](https://bugs.launchpad.net/pinta/+bug/1424629)).
- Fixed several transparency-related issues with premultiplied alpha (#109, #113, #114, #117, #125).
- Corrected display problems in the Move Selected Tool and live previews for effects (#115).
- Add-ins can now load icons correctly (#116).
- Fixed strange behaviour when the width or height of a drawn rounded rectangle is 0 (#112).
- Fixed issues with the text tool on OSX ([#1425749](https://bugs.launchpad.net/pinta/+bug/1425749)).
- Fixed inconsistent labels in the UI ([#1579033](https://bugs.launchpad.net/pinta/+bug/1579033)).
- Fixed issues with the zoom tool under certain locales (#139, #133, [#1464855](https://bugs.launchpad.net/pinta/+bug/1464855)).
- Fixed issues when drawing on very zoomed-in images (#129, #133).
- Fixed issues where brushes could draw outside the selection ([#1775709](https://bugs.launchpad.net/pinta/+bug/1775709)).
- Fixed issues with the docking library ([#832395](https://bugs.launchpad.net/pinta/+bug/832395)).
- Fixed a bug where undoing a history item could set the background palette color to the foreground color ([#1888131](https://bugs.launchpad.net/pinta/+bug/1888131)).
- Fixed issues where the zoom level was not maintained when undoing a Crop to Selection ([#1888885](https://bugs.launchpad.net/pinta/+bug/1888885)).
- Fixed an error on newer Mono versions when opening URLs via the menu items under the Help menu ([#1888883](https://bugs.launchpad.net/pinta/+bug/1888883)).
- Fixed some occasional crashes on dragging and dropping or pasting into a new image ([#1838620](https://bugs.launchpad.net/pinta/+bug/1838620), [#1508777](https://bugs.launchpad.net/pinta/+bug/1508777)).
- Fixed issues where using the Rectangle Select tool after the Move Selection or Move Selected Pixels tools did not update correctly ([#1889647](https://bugs.launchpad.net/pinta/+bug/1889647), [#1473430](https://bugs.launchpad.net/pinta/+bug/1473430), [#1889774](https://bugs.launchpad.net/pinta/+bug/1889774)).
- Adjusted `Pinta.Install.proj` to simplify installing to a custom prefix ([#781836](https://bugs.launchpad.net/pinta/+bug/781836)).
