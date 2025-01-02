#!/usr/bin/env python3

import argparse
import os
import pathlib
import re
import shutil
import subprocess
from stat import S_IREAD, S_IRGRP, S_IROTH, S_IWUSR

# Grab all dependencies of libadwaita / libgtk, plus pixbuf loader plugins.
GTK_LIB = "lib/libadwaita-1.0.dylib"
RSVG_LIB = "lib/librsvg-2.2.dylib"
TIFF_LIB = "lib/libtiff.6.dylib"
WEBP_DEMUX_LIB = "lib/libwebpdemux.2.dylib"
WEBP_MUX_LIB = "lib/libwebpmux.3.dylib"
ROOT_LIBS = [GTK_LIB, RSVG_LIB, TIFF_LIB, WEBP_DEMUX_LIB, WEBP_MUX_LIB]

ADWAITA_THEME = "share/icons/Adwaita/index.theme"
PIXBUF_LOADERS = "lib/gdk-pixbuf-2.0/2.10.0"
GLIB_SCHEMAS = "share/glib-2.0/schemas"


def run_install_name_tool(lib, deps, lib_install_dir):
    # Make writable by user.
    os.chmod(lib, S_IREAD | S_IRGRP | S_IROTH | S_IWUSR)

    # Run install_name_tool to fix up the absolute paths to the library
    # dependencies.
    for dep_path in deps:
        dep_path_basename = os.path.basename(dep_path)
        dep_lib_name = os.path.basename(os.path.realpath(dep_path))
        dep_lib = "@executable_path/../Resources/lib/" + dep_lib_name
        cmd = ['install_name_tool',
               '-change', dep_path, dep_lib,
               '-change', f"@rpath/{dep_path_basename}", dep_lib,  # For libraries like webp
               lib]
        subprocess.check_output(cmd)


def collect_libs(src_lib, lib_deps, otool_lib_regex, otool_rel_lib_regex):
    """
    Use otool -L to collect the library dependencies.
    """
    cmd = ['otool', '-L', src_lib]
    output = subprocess.check_output(cmd).decode('utf-8')
    referenced_paths = re.findall(otool_lib_regex, output)

    folder = os.path.dirname(src_lib)
    referenced_paths.extend([os.path.join(folder, lib)
                            for lib in re.findall(otool_rel_lib_regex, output)])

    real_lib_paths = set([os.path.realpath(lib) for lib in referenced_paths])

    lib_deps[src_lib] = referenced_paths

    for lib in real_lib_paths:
        if lib not in lib_deps:
            collect_libs(lib, lib_deps, otool_lib_regex, otool_rel_lib_regex)


def copy_resources(src_prefix, res_path):
    """
    Copy a folder from ${PREFIX}/${res_path} to Contents/Resources/${res_path}.
    """
    dest_folder = os.path.join(args.resource_dir, res_path)
    shutil.copytree(os.path.join(src_prefix, res_path),
                    dest_folder,
                    dirs_exist_ok=True)


def copy_plugins(src_prefix, res_path, lib_install_dir, otool_lib_regex, otool_rel_lib_regex):
    """
    Copy a folder of plugins from ${PREFIX}/${res_path} to
    Contents/Resources/${res_path} and update the library references.
    """

    copy_resources(src_prefix, res_path)

    # Update paths to the main GTK libs.
    lib_install_dir = os.path.join(args.resource_dir, 'lib')
    dest_folder = os.path.join(args.resource_dir, res_path)
    for root, dirs, files in os.walk(dest_folder):
        for lib in files:
            if not lib.endswith(".so"):
                continue

            lib_path = os.path.join(root, lib)
            lib_deps = {}
            collect_libs(lib_path, lib_deps, otool_lib_regex,
                         otool_rel_lib_regex)
            run_install_name_tool(lib_path, lib_deps[lib_path],
                                  lib_install_dir)


def install_plugin_cache(src_prefix, cache_path, resource_dir):
    """
    Copy a file such as immodules.cache, and update the library paths to be
    paths inside the .app bundle.
    """
    src_cache = os.path.join(src_prefix, cache_path)
    dest_cache = os.path.join(resource_dir, cache_path)

    with open(src_cache, 'r') as src_f:
        contents = src_f.read()
        contents = re.sub(r"/.*/(lib|share)/",
                          r"@executable_path/../Resources/\1/", contents)

        with open(dest_cache, 'w') as dest_f:
            dest_f.write(contents)


parser = argparse.ArgumentParser(description='Bundle the GTK libraries.')
parser.add_argument('--runtime', type=str, required=True,
                    help='The dotnet runtime id, e.g. osx-x64 or osx-arm64')
parser.add_argument('--resource_dir',
                    type=pathlib.Path,
                    required=True,
                    help='Directory to copy extra resources to.')
args = parser.parse_args()

src_prefix = ""
if args.runtime == "osx-x64":
    src_prefix = "/usr/local"
elif args.runtime == "osx-arm64":
    src_prefix = "/opt/homebrew"
else:
    raise RuntimeError("Invalid runtime id")

# Match against non-system libraries
otool_lib_regex = re.compile(fr"({src_prefix}/.*\.dylib)")
# Match against relative paths (webp and related libraries)
otool_rel_lib_regex = re.compile(r"@rpath/(lib.*\.dylib)")

lib_deps = {}
for root_lib in ROOT_LIBS:
    lib_path = os.path.realpath(os.path.join(src_prefix, root_lib))
    collect_libs(lib_path, lib_deps, otool_lib_regex, otool_rel_lib_regex)

lib_install_dir = os.path.join(args.resource_dir, 'lib')
os.makedirs(lib_install_dir)

for lib, deps in lib_deps.items():
    lib_copy = shutil.copy(lib, lib_install_dir)
    run_install_name_tool(lib_copy, deps, lib_install_dir)

# Add the libgdk symlink that GtkSharp needs.
os.symlink("libgdk_pixbuf-2.0.0.dylib",
           os.path.join(lib_install_dir, "libgdk_pixbuf-2.0.dylib"))

# Copy translations and icons.
gtk_root = os.path.join(os.path.dirname(
    os.path.realpath(os.path.join(src_prefix, GTK_LIB))), "..")
shutil.copytree(os.path.join(gtk_root, 'share/locale'),
                os.path.join(args.resource_dir, 'share/locale'),
                dirs_exist_ok=True)
# TODO - could probably trim the number of installed icons.
adwaita_icons = os.path.join(os.path.dirname(
    os.path.realpath(os.path.join(src_prefix, ADWAITA_THEME))), "..")
shutil.copytree(adwaita_icons,
                os.path.join(args.resource_dir, 'share/icons'),
                dirs_exist_ok=True)

copy_plugins(src_prefix, PIXBUF_LOADERS, lib_install_dir,
             otool_lib_regex, otool_rel_lib_regex)
install_plugin_cache(src_prefix, os.path.join(PIXBUF_LOADERS, "loaders.cache"),
                     args.resource_dir)

copy_resources(src_prefix, GLIB_SCHEMAS)
