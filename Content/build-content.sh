#!/bin/sh
set -e
echo "Building centralized content..."
dotnet tool restore
dotnet tool run mgcb Content.mgcb /rebuild
echo "Content build complete!" 