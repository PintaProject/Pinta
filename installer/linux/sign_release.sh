#!/bin/bash

# Check if a filename was provided
if [ -z "$1" ]; then
    echo "Usage: $0 <filename>"
    exit 1
fi

TARGET_FILE="$1"

echo "Signing file $TARGET_FILE ..."
gpg --digest-algo SHA512 --armor --detach-sign $TARGET_FILE

echo "Verifying signature..."
if gpg --verify "$TARGET_FILE.asc" "$TARGET_FILE"; then
    echo "Verification successful!"
else
    echo "Verification failed!"
    exit 1
fi

echo "Creating SHA512 checksum..."
sha512sum "$TARGET_FILE" > "$TARGET_FILE.sha512"

echo "--- Done! Created $TARGET_FILE.asc and $TARGET_FILE.sha512 ---"
