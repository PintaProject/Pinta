# Pinta - [Simple Gtk# Paint Program](http://pinta-project.com/)

[![Translation status](https://hosted.weblate.org/widget/pinta/pinta/287x66-grey.png)](https://hosted.weblate.org/engage/pinta/)
[![Build Status](https://github.com/PintaProject/Pinta/workflows/Build/badge.svg)](https://github.com/PintaProject/Pinta/actions)

Copyright (C) 2010 Jonathan Pobst <monkey AT jpobst DOT com>

Pinta is a GTK clone of [Paint.Net 3.0](http://www.getpaint.net/)

Original Pinta code is licensed under the MIT License:
See `license-mit.txt` for the MIT License

Code from Paint.Net 3.36 is used under the MIT License and retains the
original headers on source files.

See `license-pdn.txt` for Paint.Net's original license.


## Icons are from:

- [Paint.Net 3.0](http://www.getpaint.net/)
Used under [MIT License](http://www.opensource.org/licenses/mit-license.php)

- [Silk icon set](http://www.famfamfam.com/lab/icons/silk/)
Used under [Creative Commons Attribution 3.0 License](http://creativecommons.org/licenses/by/3.0/)

- [Fugue icon set](http://pinvoke.com/)
Used under [Creative Commons Attribution 3.0 License](http://creativecommons.org/licenses/by/3.0/)

- Pinta contributors, under the same license as the project itself

## Building on Windows

First, install the required GTK-related dependencies:
- Install MinGW64 via [MSYS2](https://www.msys2.org)
- From the MinGW64 terminal, run `pacman -S mingw-w64-x86_64-libadwaita mingw-w64-x86_64-webp-pixbuf-loader`.

Pinta can then be built by opening `Pinta.sln` in [Visual Studio](https://visualstudio.microsoft.com/).
Ensure that .NET 8 is installed via the Visual Studio installer.

For building on the command line:
- [Install the .NET 8 SDK](https://dotnet.microsoft.com/).
- Build:
  - `dotnet build`
- Run:
  - `dotnet run --project Pinta`

## Building on macOS

- Install .NET 8 and GTK4
  - `brew install dotnet-sdk libadwaita adwaita-icon-theme gettext webp-pixbuf-loader`
- Build:
  - `dotnet build`
- Run:
  - `dotnet run --project Pinta`

Alternatively, Pinta can be built by opening `Pinta.sln` in [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/).

## Building on Linux

- Install [.NET 8](https://dotnet.microsoft.com/) following the instructions for your Linux distribution.
- Install other dependencies (instructions are for Ubuntu 22.10, but should be similar for other distros):
  - `sudo apt install autotools-dev autoconf-archive gettext intltool libadwaita-1-dev`
  - Minimum library versions: `gtk` >= 4.12 and `libadwaita` >= 1.4
  - Optional dependencies: `webp-pixbuf-loader`
- Build (option 1, for development and testing):
  - `dotnet build`
  - `dotnet run --project Pinta`
- Build (option 2, for installation):
  - `./autogen.sh`
    - If building from a tarball, run `./configure` instead.
    - Add the `--prefix=<install directory>` argument to install to a directory other than `/usr/local`.
  - `make install`

## Getting help / contributing:

- You can get technical help on the [Pinta Google Group](https://groups.google.com/group/pinta-project)
- You can report bugs/issues on [Launchpad bug tracker](https://bugs.launchpad.net/pinta/+filebug)
- You can make suggestions on [Github](https://github.com/PintaProject/Pinta/discussions/categories/ideas)
- You can help [translate Pinta to your native language](https://hosted.weblate.org/engage/pinta/)
- You can fork the project on [Github](https://github.com/PintaProject/Pinta)
- You can get help in #pinta on irc.gnome.org.
- For details on patching, take a look at `patch-guidelines.md` in the repo.
