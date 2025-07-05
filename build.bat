@echo off
echo Building Game Engine...

echo Building Engine.Core...
dotnet build Engine\Engine.Core\Engine.Core.csproj

echo Building GameRuntime...
dotnet build Runtime\GameRuntime.csproj

echo Building Editor...
dotnet build Editor\Editor.csproj
dotnet run --project Editor

echo Build complete!