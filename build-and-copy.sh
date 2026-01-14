#!/bin/bash
# Build TypeScript i skopiuj do wwwroot
echo "Building TypeScript with Webpack..."
npm run build

if [ $? -eq 0 ]; then
    echo "Build successful, copying to wwwroot..."
    cp -v dist/bundle.js VirtualNanny/wwwroot/dist/
    cp -v dist/bundle.js.map VirtualNanny/wwwroot/dist/
    echo "Done! Files copied to VirtualNanny/wwwroot/dist/"
else
    echo "Build failed!"
    exit 1
fi
