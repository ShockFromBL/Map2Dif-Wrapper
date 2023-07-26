# Map2Dif-Wrapper

This is a wrapper for map2dif that adds automatic texture copying for level designers like TrenchBroom when using it for Torque games.

## Usage

1. Download the latest version from the Releases page or compile the source yourself.
2. Find a copy of `map2dif_plus.exe` and place it next to the `map2dif_wrapper.exe`
3. Open the `map2dif_wrapper.yaml` file and edit the `texturesPath` property to be the full path to the textures folder of your chosen level designer.
4. Drag and drop your `.map` file on top of `map2dif_wrapper.exe` instead of your map2dif executable.
5. Your `.dif` file should be created with the textures you used copied alongside it.

Theoretically, you can use all the parameters that your map2dif executable already supports. It's a wrapper and will pass all arguments to map2dif as-is.

## Caveats

This is mainly aimed with `map2dif_plus.exe` in mind, other map2dif executables will be picked up by the wrapper but your YMMV on whether it works or not.

If you use `auxiliary/NULL` as a texture in your maps, please note that unless you change it to `NULL` in your `.map` file, map2dif will not apply the texture correctly. In the map2dif source, it looks precisely for `NULL`.

The wrapper is not able to print out the dumb Fatal ISV ("In Shipping Version") errors that occur when map2dif encounters an "unrecoverable" situation, this is because they are actually debug breaks and the wrapper is not able to catch them because it's not a debugger. If you get a non-zero error code from map2dif and it doesn't seem clear why, try running your `.map` file without the wrapper and looking at the error it spits out in the console window.