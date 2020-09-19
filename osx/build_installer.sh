#!/bin/sh
set -x

msbuild ../Pinta.Install.proj -target:CompileTranslations
dotnet publish ../Pinta.sln -r osx-x64

MAC_APP_DIR=Pinta.app
MAC_APP_BIN_DIR="${MAC_APP_DIR}/Contents/MacOS/"
BIN_DIR=../bin/osx-x64/publish

mkdir -p ${MAC_APP_DIR}/Contents/{MacOS,Resources}
cp -r ${BIN_DIR}/ ../bin/locale ${MAC_APP_BIN_DIR}
cp Info.plist ${MAC_APP_DIR}/Contents
cp pinta.icns ${MAC_APP_DIR}/Contents/Resources
touch ${MAC_APP_DIR}
zip -r9uq ${MAC_APP_DIR}.zip ${MAC_APP_DIR}
