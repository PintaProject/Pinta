#!/bin/sh
set -x

MAC_APP_DIR=Pinta.app
MAC_APP_BIN_DIR="${MAC_APP_DIR}/Contents/MacOS/"
BIN_DIR=../bin

mkdir -p ${MAC_APP_DIR}/Contents/{MacOS,Resources}
cp -r ${BIN_DIR}/ ./pinta ${MAC_APP_BIN_DIR}
chmod +x ${MAC_APP_BIN_DIR}/pinta
cp Info.plist ${MAC_APP_DIR}/Contents
cp pinta.icns ${MAC_APP_DIR}/Contents/Resources
touch ${MAC_APP_DIR}
zip -r9uq ${MAC_APP_DIR}.zip ${MAC_APP_DIR}
