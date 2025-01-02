#!/bin/sh
set -e

# Parse command line arguments
runtimeid=$1

if [ "$runtimeid" != "osx-x64" ] && [ "$runtimeid" != "osx-arm64" ]; then
    echo "Invalid runtime identifier (should be osx-x64 or osx-arm64)"
    echo "Usage: ./build_installer.sh runtimeid"
    exit 1
fi 

MAC_APP_DIR="$PWD/package/Pinta.app"
MAC_APP_BIN_DIR="${MAC_APP_DIR}/Contents/MacOS/"
MAC_APP_RESOURCE_DIR="${MAC_APP_DIR}/Contents/Resources/"
MAC_APP_SHARE_DIR="${MAC_APP_RESOURCE_DIR}/share"

run_codesign()
{
    file=$1
    echo ${file}
    codesign --deep --force --timestamp --options runtime --sign "Developer ID Application: Cameron White (D5G6C56TBH)" --entitlements entitlements.plist ${file}
}

mkdir -p ${MAC_APP_BIN_DIR} ${MAC_APP_RESOURCE_DIR} ${MAC_APP_SHARE_DIR}

dotnet publish ../../Pinta.sln -p:PublishDir=${MAC_APP_BIN_DIR} -p:BuildTranslations=true -c Release -r $runtimeid --self-contained true

# Remove stuff we don't need.
rm ${MAC_APP_BIN_DIR}/*.pdb

# Move resources files out of the MacOS folder (needed for code signing).
# TODO - this could be done in the .csproj publish rule instead?
mv ${MAC_APP_BIN_DIR}/locale ${MAC_APP_SHARE_DIR}/locale
mv ${MAC_APP_BIN_DIR}/icons ${MAC_APP_SHARE_DIR}/icons
cp hicolor.index.theme ${MAC_APP_SHARE_DIR}/icons/hicolor/index.theme

cp Info.plist ${MAC_APP_DIR}/Contents
cp pinta.icns ${MAC_APP_DIR}/Contents/Resources

# Install the GTK dependencies.
echo "Bundling GTK..."
./bundle_gtk.py --runtime $runtimeid --resource_dir ${MAC_APP_RESOURCE_DIR}
# Add the GTK lib dir to the library search path (for dlopen()), as an alternative to $DYLD_LIBRARY_PATH.
install_name_tool -add_rpath "@executable_path/../Resources/lib" ${MAC_APP_BIN_DIR}/Pinta

touch ${MAC_APP_DIR}

# Sign the GTK binaries.
echo "Signing..."
for lib in `find ${MAC_APP_RESOURCE_DIR} -name \*.dylib -or -name \*.so`
do
    run_codesign ${lib}
done

# Sign the main executable and .NET stuff.
run_codesign ${MAC_APP_DIR}

# Create and sign the .dmg image, and include a link to drag the app into /Applications
echo "Creating dmg..."
ln -s /Applications package/Applications
hdiutil create -quiet -srcFolder package -volname "Pinta Installer" -o Pinta.dmg
run_codesign Pinta.dmg

# Notarize
echo "Notarizing..."
xcrun notarytool submit --wait --apple-id=cameronwhite91@gmail.com --password ${MAC_DEV_PASSWORD} --team-id D5G6C56TBH Pinta.dmg

# Staple the result to the dmg
echo "Stapling..."
xcrun stapler staple Pinta.dmg
