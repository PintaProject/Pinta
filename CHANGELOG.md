# Change Log
All notable changes to this project (beginning with version 1.7) will be documented in this file.

## [Unreleased](https://github.com/PintaProject/Pinta/compare/1.7...HEAD)

Thanks to the following contributors who worked on this release:
- @cameronwhite
- @darkdragon-001
- @JamiKettunen
- @thekolian1996

### Added
- The canvas can now be scrolled horizontally by holding Shift while using the mouse wheel (#141)
- Added a more user-friendly dialog when attempting to open an unsupported file format (#143, [#1856821](https://bugs.launchpad.net/pinta/+bug/1856821))

### Changed

### Fixed
- Fixed invalid URLs in `pinta.appdata.xml` (#140)

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
