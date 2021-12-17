#!/bin/sh
set -e
set -x

MAC_APP_DIR=Pinta.app
MAC_APP_BIN_DIR="${MAC_APP_DIR}/Contents/MacOS/"
MAC_APP_RESOURCE_DIR="${MAC_APP_DIR}/Contents/Resources/"
MAC_APP_SHARE_DIR="${MAC_APP_RESOURCE_DIR}/share"

mkdir -p ${MAC_APP_BIN_DIR} ${MAC_APP_RESOURCE_DIR} ${MAC_APP_SHARE_DIR}

dotnet publish ../../Pinta.sln -p:BuildTranslations=true --configuration Release -r osx-x64 --self-contained true -o ${MAC_APP_BIN_DIR}

# Remove stuff we don't need.
rm ${MAC_APP_BIN_DIR}/*.pdb

# Move resources files out of the MacOS folder (needed for code signing).
# TODO - this could be done in the .csproj publish rule instead?
mv ${MAC_APP_BIN_DIR}/locale ${MAC_APP_SHARE_DIR}/locale
mv ${MAC_APP_BIN_DIR}/icons ${MAC_APP_SHARE_DIR}/icons

cp Info.plist ${MAC_APP_DIR}/Contents
cp pinta.icns ${MAC_APP_DIR}/Contents/Resources

./bundle_gtk.py --install_dir ${MAC_APP_BIN_DIR} --resource_dir ${MAC_APP_RESOURCE_DIR}

touch ${MAC_APP_DIR}

# Sign
#codesign --deep --force --timestamp --sign "Developer ID Application: Cameron White (D5G6C56TBH)" Pinta.app --options=runtime --no-strict --entitlements entitlements.plist

# Zip
zip -r9uq --symlinks ${MAC_APP_DIR}.zip ${MAC_APP_DIR}
