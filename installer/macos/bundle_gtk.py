#!/usr/bin/env python3

import argparse
import os
import pathlib
import re
import shutil
import subprocess
from stat import S_IREAD, S_IRGRP, S_IROTH, S_IWUSR

PREFIX = "/usr/local"

# Grab all dependencies of libgtk, plus pixbuf loader plugins.
GTK_LIB = "/usr/local/lib/libgtk-3.dylib"
RSVG_LIB = "/usr/local/lib/librsvg-2.2.dylib"
TIFF_LIB = "/usr/local/lib/libtiff.6.dylib"
ROOT_LIBS = [GTK_LIB, RSVG_LIB, TIFF_LIB]

ADWAITA_THEME = "/usr/local/share/icons/Adwaita/index.theme"
PIXBUF_LOADERS = "lib/gdk-pixbuf-2.0/2.10.0"
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
        dep_lib = "@executable_path/../Resources/lib/" + dep_lib_name
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
    Copy a folder of plugins from ${PREFIX}/${res_path} to
    Contents/Resources/${res_path} and update the library references.
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


def install_plugin_cache(cache_path, resource_dir):
    """
    Copy a file such as immodules.cache, and update the library paths to be
    paths inside the .app bundle.
    """
    src_cache = os.path.join(PREFIX, cache_path)
    dest_cache = os.path.join(resource_dir, cache_path)

    with open(src_cache, 'r') as src_f:
        contents = src_f.read()
        contents = re.sub(r"/.*/(lib|share)/",
                          r"@executable_path/../Resources/\1/", contents)

        with open(dest_cache, 'w') as dest_f:
            dest_f.write(contents)


parser = argparse.ArgumentParser(description='Bundle the GTK libraries.')
parser.add_argument('--resource_dir',
                    type=pathlib.Path,
                    required=True,
                    help='Directory to copy extra resources to.')
args = parser.parse_args()

lib_deps = {}
for root_lib in ROOT_LIBS:
    collect_libs(os.path.realpath(root_lib), lib_deps)

lib_install_dir = os.path.join(args.resource_dir, 'lib')
os.makedirs(lib_install_dir)

for lib, deps in lib_deps.items():
    lib_copy = shutil.copy(lib, lib_install_dir)
    run_install_name_tool(lib_copy, deps, lib_install_dir)

# Add the libgdk symlink that GtkSharp needs.
os.symlink("libgdk_pixbuf-2.0.0.dylib",
           os.path.join(lib_install_dir, "libgdk_pixbuf-2.0.dylib"))

# Copy translations and icons.
gtk_root = os.path.join(os.path.dirname(os.path.realpath(GTK_LIB)), "..")
shutil.copytree(os.path.join(gtk_root, 'share/locale'),
                os.path.join(args.resource_dir, 'share/locale'),
                dirs_exist_ok=True)
# TODO - could probably trim the number of installed icons.
adwaita_icons = os.path.join(os.path.dirname(os.path.realpath(ADWAITA_THEME)), "..")
shutil.copytree(adwaita_icons,
                os.path.join(args.resource_dir, 'share/icons'),
                dirs_exist_ok=True)

copy_plugins(PIXBUF_LOADERS, lib_install_dir)
install_plugin_cache(os.path.join(PIXBUF_LOADERS, "loaders.cache"),
                     args.resource_dir)

copy_plugins(IM_MODULES, lib_install_dir)
install_plugin_cache(os.path.join(IM_MODULES, "../immodules.cache"),
                     args.resource_dir)

copy_resources(GLIB_SCHEMAS)
