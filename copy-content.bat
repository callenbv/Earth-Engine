@echo off
echo Copying compiled content to Editor Assets directory...

REM Create the target directory if it doesn't exist
if not exist "Editor\bin\Assets\Rooms" mkdir "Editor\bin\Assets\Rooms"

REM Copy the compiled .xnb files
copy "Content\bin\Windows\Content\Rooms\Home.xnb" "Editor\bin\Assets\Rooms\Home.xnb"
copy "Content\bin\Windows\Content\ShadowCast.xnb" "Editor\bin\Assets\ShadowCast.xnb"

echo Content copied successfully!
pause 