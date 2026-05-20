# DemoBuilder2000

A build system for converting shadertoy style shaders into js demos.

For example:

```glsl
void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = fragCoord / iResolution.xy;
    float m= 128.;
    fragColor = vec4(vec3(float(int(uv.x*m)^int(uv.y*m))/m), 1.0);
}
```

## Usage: 

```
db2k <.glsl/.js file>
```

Options:

-m: montor the input files and re-compile whenever they change

Sets up a http server on port 6969. Also adds a piece of code in the generated js that polls the http server and refreshes when the js changes.

-o=<.html file>: use a specific output file, instead of just out/index.html

### Javascript or GLSL files
If a GLSL file is specified, then the demo will consist of a single shader.
If a javascript file is specified then muliple shaders are able to be used.

## Javascript
The main Javascript file must define a function 'frameUpdate' which will be called before the shader is rendered for that frame. The js should then setup any uniforms that will affect the rendering of the frame.

### Shaders

Specify the shader using the //#shader command. Then have frameUpdate return the shader reference number that you want to use for that frame.

```js
//#shader "main.glsl" 0
//#shader "other.glsl" 1

function update(frame, ut) {
    return frame%2;
}
```

This example will toggle alternate between the two shaders.

## Testing

An automated test suite is provided in the `tests` directory.

### Running Tests

To run all tests and verify the output against established baselines:

```powershell
cd tests
powershell -ExecutionPolicy Bypass -File .\run_tests.ps1
```

The test runner automatically discovers all directories in `tests/` that contain a `test.glsl` or `test.js` file, builds them using `db2k`, and compares the generated `out/index.html` against an `expected.html` file using SHA256 hashing.

### Visual Regression Testing

You can also verify that the *rendered* output of the shader hasn't changed by using the `-Visual` flag:

```powershell
powershell -ExecutionPolicy Bypass -File .\run_tests.ps1 -Visual
```

This requires **Node.js** and **Playwright**. It will:
1. Open the compiled demo in a headless browser.
2. Force a deterministic frame/time (frame 0, time 1.0).
3. Compare the canvas pixels against an `expected.png` file.

### Managing Baselines

If you intentionally change the build output (e.g., by updating `main.js` or `DemoBuilder.cs`), you can update both the HTML and PNG baselines by running:

```powershell
powershell -ExecutionPolicy Bypass -File .\run_tests.ps1 -Update -Visual
```

### CI/CD

A GitHub Action is configured to run these tests on every push. It will fail if the build fails OR if the generated output does not match the committed `expected.html` files.

## GLSL Format

The glsl generally mimics how shadertoy.com does things. 

### Entry point

The glsl code is setup to call a function at startup as such:

```glsl
void mainImage( out vec4 fragColor, in vec2 fragCoord )
{
}
```

### Uniforms

These uniforms are predefined:

- iTime
   - The time in seconds since the start of playing the demo.
- iFrame
  -	The number of frames since the start of playing the demo.
- iResolution
  - The resolution of the canvas the shader is rendering to. The canvas will cover the entire area of the page.

### Commands

All commands start with '//#' and then the name of the command.
Parameters follow after.

### shader

JS only.

```
//#shader "effect.glsl" 0
```

Makes a shader out of a glsl file. The shader can be referenced with an number as the second parameter.


### include
GLSL only.

Other code can be included as such:

```glsl
//#include "other.glsl"
```

This will include the file in the code. Note that the included file can then include other files. 
Subsequent includes of the same file will be ignored.

### audio
GLSL or JS.

Sets up the audio for the demo.

```glsl
//#audio "music.mp3"
```

MP3s are the only audio format supported.
If specified, the startpause option will be enabled.


### splash
GLSL or JS.

Specifies a html div to show to the user when waiting for user input.
```glsl
//#splash "splash.html"
```
If not specified, a message saying 'click to start' will be shown.
If specified, the startpause option will be enabled.


### texture
GLSL only.

Sets up a texture sampler.
```glsl
//#texture "image.png" image
```
Replace 'image' with the name of the sampler.

A sampler name cannot be specified more than once.

### uniform
GLSL only.

Sets up a uniform that can be accessed by javascript.
```glsl
//#uniform <type> name
```

The uniform can be accessed in glsl and javascript with 'name'.

A name cannot be specified more than once.

#### Working with Uniforms

The recommended way of setting uniforms from javascript is to set them on every frame in the update() function.

floats:

The simplest form of uniforms.

```glsl
//#uniform float foobar
```

```javascript
foobar.set( 2.3 );
```

integers:

```glsl
//#uniform int foobar
```

```javascript
foobar.set( 2 );
```

arrays:
```glsl
//#uniform float[4] foobar
```

```javascript
foobar.set( new Float32Array([1,2,3,4]) );
```

Due to limitations of webgl2, the size of the array has to be specified in the glsl code.



### option
GLSL or JS.

Set the value of an option.

```glsl
//#option startpause true 
```

First parameter is the option that is being set, second is the value it has.

| Option | Valid Parameters | Default Value | Description
|---|---|---|---|
|startpause| true, false| false |Pauses the demo at the start and waits for the user to interact with the page. This is done because browsers may be setup to block audio until user interaction occurs.




## Notes

- Code is not size optimized so whilst the js code at 14kb is not overly bloated, it won't be suitable for doing 4ks.