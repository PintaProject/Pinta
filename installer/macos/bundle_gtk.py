#!/usr/bin/env python3

import argparse
import os
import pathlib
import re
import shutil
import subprocess
from stat import S_IREAD, S_IRGRP, S_IROTH, S_IWUSR

ROOT_LIB = "/usr/local/lib/libgtk-3.dylib"
ADWAITA_THEME = "/usr/local/share/icons/Adwaita/index.theme"
PIXBUF_LOADERS = "/usr/local/lib/gdk-pixbuf-2.0"
GLIB_SCHEMAS = "/usr/local/share/glib-2.0/schemas"
OTOOL_LIB_REGEX = re.compile("(/usr/local/.*\.dylib)") # Ignore system libraries.

def collect_libs(src_lib, lib_deps):
    """
    Use otool -L to collect the library dependencies.
    """
    cmd = ['otool', '-L', src_lib]
    output = subprocess.check_output(cmd).decode('utf-8')
    referenced_paths = re.findall(OTOOL_LIB_REGEX, output)
    real_lib_paths = set([os.path.realpath(lib) for lib in referenced_paths])

    lib_deps[src_lib] = referenced_paths

    for lib in real_lib_paths:
        if lib not in lib_deps:
            collect_libs(lib, lib_deps)

parser = argparse.ArgumentParser(description='Bundle the GTK libraries.')
parser.add_argument('--install_dir',
                    type=pathlib.Path,
                    required=True,
                    help='Directory to copy the executable to.')
parser.add_argument('--resource_dir',
                    type=pathlib.Path,
                    required=True,
                    help='Directory to copy extra resources to.')
args = parser.parse_args()

lib_deps = {}
collect_libs(os.path.realpath(ROOT_LIB), lib_deps)

for lib, deps in lib_deps.items():
    lib_copy = shutil.copy(lib, args.install_dir)
    # Make writable by user.
    os.chmod(lib_copy, S_IREAD | S_IRGRP | S_IROTH | S_IWUSR)

    # Run install_name_tool to fix up the absolute paths to the library
    # dependencies.
    for dep_path in deps:
        dep_lib = os.path.basename(os.path.realpath(dep_path))
        cmd = ['install_name_tool', '-change', dep_path, dep_lib, lib_copy]
        subprocess.check_output(cmd)

# Add the libgdk symlink that GtkSharp needs.
os.symlink("libgdk_pixbuf-2.0.0.dylib",
           os.path.join(args.install_dir, "libgdk_pixbuf-2.0.dylib"))

# Copy translations and icons.
gtk_root = os.path.join(os.path.dirname(os.path.realpath(ROOT_LIB)), "..")
shutil.copytree(os.path.join(gtk_root, 'share/locale'),
                os.path.join(args.resource_dir, 'share/locale'),
                dirs_exist_ok=True)
shutil.copytree(os.path.join(gtk_root, 'share/icons'),
                os.path.join(args.resource_dir, 'share/icons'),
                dirs_exist_ok=True)
adwaita_icons = os.path.join(os.path.dirname(os.path.realpath(ADWAITA_THEME)), "..")
shutil.copytree(adwaita_icons,
                os.path.join(args.resource_dir, 'share/icons'),
                dirs_exist_ok=True)

# Copy pixbuf loaders
shutil.copytree(PIXBUF_LOADERS,
                os.path.join(args.install_dir, 'gdk-pixbuf-2.0'),
                dirs_exist_ok=True)

# glib schemas
shutil.copytree(GLIB_SCHEMAS,
                os.path.join(args.resource_dir, 'share/glib-2.0/schemas'),
                dirs_exist_ok=True)
