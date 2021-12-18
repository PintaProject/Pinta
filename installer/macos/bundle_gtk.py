#!/usr/bin/env python3

import argparse
import os
import pathlib
import re
import shutil
import subprocess
from stat import S_IREAD, S_IRGRP, S_IROTH, S_IWUSR

PREFIX = "/usr/local"
ROOT_LIB = "/usr/local/lib/libgtk-3.dylib"
ADWAITA_THEME = "/usr/local/share/icons/Adwaita/index.theme"
PIXBUF_LOADERS = "lib/gdk-pixbuf-2.0"
IM_MODULES = "lib/gtk-3.0/3.0.0/immodules"
GLIB_SCHEMAS = "share/glib-2.0/schemas"

OTOOL_LIB_REGEX = re.compile("(/usr/local/.*\.dylib)") # Ignore system libraries.


def run_install_name_tool(lib, deps, lib_install_dir):
    # Make writable by user.
    os.chmod(lib, S_IREAD | S_IRGRP | S_IROTH | S_IWUSR)

    # Run install_name_tool to fix up the absolute paths to the library
    # dependencies.
    for dep_path in deps:
        dep_lib_name = os.path.basename(os.path.realpath(dep_path))
        dep_lib = os.path.relpath(os.path.join(lib_install_dir, dep_lib_name),
                                  os.path.dirname(lib))
        cmd = ['install_name_tool', '-change', dep_path, dep_lib, lib]
        subprocess.check_output(cmd)


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


def copy_resources(res_path):
    """
    Copy a folder from ${PREFIX}/${res_path} to Contents/Resources/${res_path}.
    """
    dest_folder = os.path.join(args.resource_dir, res_path)
    shutil.copytree(os.path.join(PREFIX, res_path),
                    dest_folder,
                    dirs_exist_ok=True)


def copy_plugins(res_path, lib_install_dir):
    """
    Copy a folder from ${PREFIX}/${res_path} to Contents/Resources/${res_path}.
    """

    copy_resources(res_path)

    # Update paths to the main GTK libs.
    lib_install_dir = os.path.join(args.resource_dir, 'lib')
    dest_folder = os.path.join(args.resource_dir, res_path)
    for root, dirs, files in os.walk(dest_folder):
        for lib in files:
            if not lib.endswith(".so"):
                continue

            lib_path = os.path.join(root, lib)
            lib_deps = {}
            collect_libs(lib_path, lib_deps)
            run_install_name_tool(lib_path, lib_deps[lib_path],
                                  lib_install_dir)


parser = argparse.ArgumentParser(description='Bundle the GTK libraries.')
parser.add_argument('--resource_dir',
                    type=pathlib.Path,
                    required=True,
                    help='Directory to copy extra resources to.')
args = parser.parse_args()

lib_deps = {}
collect_libs(os.path.realpath(ROOT_LIB), lib_deps)

lib_install_dir = os.path.join(args.resource_dir, 'lib')
os.makedirs(lib_install_dir)

for lib, deps in lib_deps.items():
    lib_copy = shutil.copy(lib, lib_install_dir)
    run_install_name_tool(lib_copy, deps, lib_install_dir)

# Add the libgdk symlink that GtkSharp needs.
os.symlink("libgdk_pixbuf-2.0.0.dylib",
           os.path.join(lib_install_dir, "libgdk_pixbuf-2.0.dylib"))

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

# TODO - update immodules.cache and loaders.cache
# TODO - set GDK_PIXBUF_MODULE_FILE and GTK_IM_MODULE_FILE at runtime
copy_plugins(PIXBUF_LOADERS, lib_install_dir)
copy_plugins(IM_MODULES, lib_install_dir)

copy_resources(GLIB_SCHEMAS)
