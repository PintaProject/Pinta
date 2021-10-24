# Pinta - [Simple Gtk# Paint Program](http://pinta-project.com/)

[![Build Status](https://github.com/PintaProject/Pinta/workflows/Build/badge.svg)](https://github.com/PintaProject/Pinta/actions)

Copyright (C) 2010 Jonathan Pobst <monkey AT jpobst DOT com>

Pinta is a Gtk# clone of [Paint.Net 3.0](http://www.getpaint.net/)

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

## Building on Windows

Pinta can be built by opening `Pinta.sln` in [Visual Studio](https://visualstudio.microsoft.com/).
Ensure that .NET 5 is installed via the Visual Studio installer.

For building on the command line:
- [Install the .NET 5 SDK](https://dotnet.microsoft.com/).
- Build:
  - `dotnet build`
- Run:
  - `dotnet run --project Pinta`

## Building on macOS

- Install .NET 5 and GTK
  - `brew install dotnet-sdk gtk+3 adwaita-icon-theme gettext`
- Build:
  - `dotnet build`
- Run:
  - `dotnet run --project Pinta`

Alternatively, Pinta can be built by opening `Pinta.sln` in [Visual Studio for Mac](https://visualstudio.microsoft.com/vs/mac/).

## Building on Linux

- Install [.NET 5](https://dotnet.microsoft.com/) following the instructions for your Linux distribution.
- Install other dependencies (instructions are for Ubuntu 20.04, but should be similar for other distros):
  - `sudo apt install autotools-dev autoconf-archive gettext intltool libgtk-3-dev`
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
- You can make suggestions at [Communiroo](https://communiroo.com/pintaproject/pinta/suggestions)
- You can help translate Pinta to your native language on [Launchpad translations](https://translations.launchpad.net/pinta)
- You can fork the project on [Github](https://github.com/PintaProject/Pinta)
- You can get help in #pinta on irc.gnome.org.
- For details on patching, take a look at `patch-guidelines.md` in the repo.
