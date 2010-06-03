Pinta - Simple Gtk# Paint Program
http://pinta-project.com/
Copyright (C) 2010 Jonathan Pobst <monkey AT jpobst DOT com>

Pinta is a Gtk# clone of Paint.Net 3.0: http://www.getpaint.net/.

Original Pinta code is licensed under the MIT License:
http://www.opensource.org/licenses/mit-license.php

Code from Paint.Net 3.36 is used under the MIT License and retains the
original headers on source files.

See license-pdn.txt for Paint.Net's original license.


Icons are from:

- Paint.Net 3.0 - http://www.getpaint.net/
Used under MIT License

- Silk icon set - http://www.famfamfam.com/lab/icons/silk/
Used under Creative Commons Attribution 3.0 License

- Fugue icon set - http://pinvoke.com/
Used under Creative Commons Attribution 3.0 License



Linux Build and Installation Instructions:

Building Pinta requires the follow software:
mono mono-xbuild automake autoconf libmono-cairo2.0-cil gtk-sharp2

Pinta only supports version 2.4 or higher of Mono. For Ubuntu, this means 10.04 or higher is required.

To build Pinta, run:
./autogen.sh
make
sudo make install

To use different installation directory than the default (/usr), run this instead:
./autogen.sh --prefix=<install directory>

To uninstall Pinta, run:
sudo make uninstall

To clean all files created during the build process, run:
make cleanall
*Note* This will require you to rerun autogen.sh in order to run more make commands.

For a list of more make commands, run:
make help
