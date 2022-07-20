#!/bin/sh

set -e
VERSION=${VERSION:-$(cat VERSION)}
echo Building $VERSION
echo


# Sets fg color to dim
function dim()
{
	echo -en '\e[2m'
}
# Reset fg color
function resetc()
{
	echo -en '\e[0m'
}

mkdir -p Export/bin Export/dl
cd Export

# Download butler
BUTLER_ZIP=dl/butler.zip

dim
if [ ! $(type -P butler) ]; then
	BUTLER=$(realpath -m ./bin/butler)
	if [ ! -f "$BUTLER"  ]; then
		curl -L -o $BUTLER_ZIP https://broth.itch.ovh/butler/linux-amd64/LATEST/archive/default
		unzip -o -d bin $BUTLER_ZIP
		chmod +x bin/butler
	else
		echo "Butler already downloaded"
	fi
else 
	echo "Butler installed using package manager"
	BUTLER=$(type -P butler)
fi
echo $BUTLER
resetc

echo "Butler version"
$BUTLER -V
echo


# Download Godot
GODOT_ZIP=dl/godot.zip
GODOT_VERSION=3.4.4
GODOT_DIR=Godot_v$GODOT_VERSION-stable_mono_linux_headless_64
GODOT_EXE=Godot_v$GODOT_VERSION-stable_mono_linux_headless.64

dim
if [ ! $(type -P godot-mono-headless) ]; then
	GODOT=$(realpath -m ./bin/$GODOT_DIR/$GODOT_EXE)
	if [ ! -f "$GODOT" ]; then
		curl -L -o $GODOT_ZIP https://downloads.tuxfamily.org/godotengine/$GODOT_VERSION/mono/$GODOT_DIR.zip
		unzip -o -d bin $GODOT_ZIP
		chmod +x bin/$GODOT_DIR/$GODOT_EXE
	else
		echo "Godot already downloaded"
	fi
else
	echo "Godot installed using package manager"
	GODOT=$(type -P godot-mono-headless)
fi
echo $GODOT
resetc

echo "Godot Version"
set +e
$GODOT --version
set -e
echo


# Export templates
GODOT_TEMPLATES_ZIP=./dl/export_templates.zip
GODOT_TEMPLATES_DIR=~/.local/share/godot/templates/$GODOT_VERSION.stable.mono

dim
if [ ! -d "$GODOT_TEMPLATES_DIR" ]; then
	curl -L -o $GODOT_TEMPLATES_ZIP https://downloads.tuxfamily.org/godotengine/$GODOT_VERSION/mono/Godot_v$GODOT_VERSION-stable_mono_export_templates.tpz
	mkdir -p $GODOT_TEMPLATES_DIR
	unzip -o -d $GODOT_TEMPLATES_DIR $GODOT_TEMPLATES_ZIP
	mv $GODOT_TEMPLATES_DIR/templates/* $GODOT_TEMPLATES_DIR
else
	echo "Export templates already downloaded"
fi
resetc


echo "Export templates version"
cat $GODOT_TEMPLATES_DIR/version.txt
echo


PROJECT=$(realpath -m ..)
echo "Exporting $PROJECT"
echo


# Prepare export presets
# Create export presets copy

# Change Windows .exe Version
sed -i "s/application\/file_version=\".*\"/application\/file_version=\"$VERSION\"/" $PROJECT/export_presets.cfg
sed -i "s/application\/product_version=\".*\"/application\/product_version=\"$VERSION\"/" $PROJECT/export_presets.cfg


# Export the game
GAME=GrandOreDeal
BUILD_PATH=$(realpath -m ./build)

dim
rm -rf $BUILD_PATH

# HTML
mkdir -p $BUILD_PATH/html
$GODOT --path "$PROJECT" --export HTML "$BUILD_PATH/html/$GAME.html"
# itch.io expects index.html
mv "$BUILD_PATH/html/$GAME.html" "$BUILD_PATH/html/index.html"

# Linux x86_64
mkdir -p $BUILD_PATH/linux64
$GODOT --path "$PROJECT" --export Linux64 "$BUILD_PATH/linux64/$GAME.x86_64"

# Windows
mkdir -p $BUILD_PATH/win64
$GODOT --path "$PROJECT" --export Windows64 "$BUILD_PATH/win64/$GAME.exe"

resetc


ITCH_USER=ryhon
ITCH_GAME=godot-lidar-demo

$BUTLER push $BUILD_PATH/html $ITCH_USER/$ITCH_GAME:html --userversion "$VERSION"
$BUTLER push $BUILD_PATH/linux64 $ITCH_USER/$ITCH_GAME:linux-64 --userversion "$VERSION"
$BUTLER push $BUILD_PATH/win64 $ITCH_USER/$ITCH_GAME:win-64 --userversion "$VERSION"