#!/bin/sh
set -e
set -x

MAC_APP_DIR=Pinta.app
MAC_APP_BIN_DIR="${MAC_APP_DIR}/Contents/MacOS/"

mkdir -p ${MAC_APP_DIR}/Contents/{MacOS,Resources}
dotnet publish ../../Pinta.sln -p:BuildTranslations=true --configuration Release -r osx-x64 -o ${MAC_APP_BIN_DIR}
cp Info.plist ${MAC_APP_DIR}/Contents
cp pinta.icns ${MAC_APP_DIR}/Contents/Resources
./bundle_gtk.py --install_dir ${MAC_APP_BIN_DIR}
touch ${MAC_APP_DIR}
zip -r9uq ${MAC_APP_DIR}.zip ${MAC_APP_DIR}
