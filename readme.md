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

## Getting help/contributing:

- You can get technical help on the [Pinta Google Group](https://groups.google.com/group/pinta-project)
- You can report bugs on [Launchpad bug tracker](https://bugs.launchpad.net/pinta/+filebug)
- You can make suggestions at [Communiroo](https://communiroo.com/pintaproject/pinta/suggestions)
- You can help translate Pinta to your native language on [Launchpad translations](https://translations.launchpad.net/pinta)
- You can fork the project on [Github](https://github.com/PintaProject/Pinta)
- You can get help in #pinta on irc.gnome.org.
- For details on patching, take a look at `patch-guidelines.md` in the repo.


## Windows Build and Installation Instructions:

Be sure to install [Gtk# for Windows](https://xamarin.azureedge.net/GTKforWindows/Windows/gtk-sharp-2.12.45.msi) when building in Visual Studio.

## Linux Build and Installation Instructions:

Building Pinta requires the following software (instructions are for Ubuntu 20.04, but should be similar for other distributions):

`sudo apt install make automake autoconf mono-devel gtk-sharp2 intltool`

Pinta only supports version 2.8 or higher of Mono.

To build Pinta, run:

`./autogen.sh`

`make`

`sudo make install`

or if building from a tarball, run:

`./configure`

`make`

`sudo make install`

To use different installation directory than the default (/usr/local), run this instead:

`./autogen.sh --prefix=<install directory>`


To uninstall Pinta, run:

`sudo make uninstall`

To clean all files created during the build process, run:

`make cleanall`

**Note** This will require you to rerun `autogen.sh` in order to run more `make` commands.

For a list of more make commands, run:

`make help`
