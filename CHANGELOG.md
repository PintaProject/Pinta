# Change Log
All notable changes to this project (beginning with version 1.7) will be documented in this file.

## [Unreleased](https://github.com/PintaProject/Pinta/compare/1.7...HEAD)

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

### Added
- Ported to GTK3 and .NET 5
  - Many changes to the appearance of standard GTK widgets and dialogs (e.g. the color picker and file dialogs). GTK3 themes should also now be supported.
  - Improved support for high-DPI displays.
  - The platform-native file dialogs are now used ([#1909807](https://bugs.launchpad.net/pinta/+bug/1909807), [#1909664](https://bugs.launchpad.net/pinta/+bug/1909664)).
  - The Open Recent menu item was deprecated in GTK3 and has been removed, but similar functionality is available in the file dialog's Recent section.
  - On macOS, the menu now appears in the global menu bar instead of the application window.
  - On macOS, keyboard shortcuts now use Command instead of Ctrl.
  - Removed the Images pad, which is obsolete now that tabs are used (#153).
  - Added recently used colors to the color palette widget (#154)
  - Added a status bar widget containing the position / selection information, zoom, and the color palette (#154)
  - Changed the tool palette to be a single column (#155)
  - Changed the text tool to use the standard GTK font chooser widget ([#1311873](https://bugs.launchpad.net/pinta/+bug/1311873), [#1866653](https://bugs.launchpad.net/pinta/+bug/1866653), [#890589](https://bugs.launchpad.net/pinta/+bug/890589))
  - Changed several tools to use spin buttons rather than editable combo boxes for e.g. selecting brush sizes ([#1186516](https://bugs.launchpad.net/pinta/+bug/1186516)).
- The canvas can now be scrolled horizontally by holding Shift while using the mouse wheel (#141)
- The canvas can now be panned by clicking and dragging with the middle mouse button (#176, [#419](https://communiroo.com/PintaProject/pinta/suggestions/419)).
- The primary and secondary palette colors can now be swapped by pressing X (#147)
- Added a more user-friendly dialog when attempting to open an unsupported file format (#143, [#1856821](https://bugs.launchpad.net/pinta/+bug/1856821))
- Zooming in and out can now be done without pressing the Ctrl key (#150).
- Arrow keys can be used to move by a single pixel in the Move Selected Pixels and Move Selection tools ([#1906141](https://bugs.launchpad.net/pinta/+bug/1906141)).
- The primary and secondary palette colors are now saved in the application settings (#171).
- Shift can now be used to constrain to a uniform scale when scaling using the Move Selected Pixels tool (#138).
- Tools now save their settings for the next time Pinta is opened (#178).
- The About dialog now allows easily copying the version information to the clipboard for use when reporting bugs ([#1924249](https://bugs.launchpad.net/pinta/+bug/1924249)).

### Changed
- Fixed inconsistent behavior when switching between tools that share the same shortcut, such as the selection tools (#144, [#1558767](https://bugs.launchpad.net/pinta/+bug/1558767))
- The Paste Into New Image action no longer creates several unnecessary history items (#170).
- Performance improvements for the paint bucket and magic wand tools (#159).
- Performance improvements for the selection tools when interactively adjusting the selection.
- The appdata file is now installed to `/usr/share/metainfo` instead of the legacy path `/usr/share/appdata` (#186).
- Improved error messages when the user does not have read or write permissions for a file ([#1715150](https://bugs.launchpad.net/pinta/+bug/1715150)).

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
- Fixed several Unicode-related issues in the text tool ([#1422445](https://bugs.launchpad.net/pinta/+bug/1422445)).
- Fixed issue on macOS where Pinta could launch in the wrong language ([#1900310](https://bugs.launchpad.net/pinta/+bug/1900310)).
- Improved the UX of the Close and Save As confirmation dialogs ([#1909576](https://bugs.launchpad.net/pinta/+bug/1909576), [#1909688](https://bugs.launchpad.net/pinta/+bug/1909688)).
- Fixed a bug where the Pan tool did not work if scrolling could only occur in the Y direction ([#1909910](https://bugs.launchpad.net/pinta/+bug/1909910)).
- Fixed issues where the zoom level was not maintained when resizing an image ([#1889673](https://bugs.launchpad.net/pinta/+bug/1889673)).
- Fixed a bug where the initial corner of a rectangle shape could be cut off ([#1922470](https://bugs.launchpad.net/pinta/+bug/1922470)).
- Fixed a bug where the text tool was not correctly clipped against the selection ([#1910511](https://bugs.launchpad.net/pinta/+bug/1910511)).
- Improved handling of memory allocation failures for large images ([#776346](https://bugs.launchpad.net/pinta/+bug/776346)).
- Fixed an issue where opening a file URI from the command line did not work ([#1908806](https://bugs.launchpad.net/pinta/+bug/1908806)).
- Fixed an issue where hiding a layer could leave the selection still visible ([#1907987](https://bugs.launchpad.net/pinta/+bug/1907987)).
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
