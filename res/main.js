const screenRatio = 1920/1080;

var loaderCanvas = document.createElement('canvas');
var curShader = null;

const vertexShaderText= 
"#version 300 es\n" +
"precision mediump float;\n" +
"in vec2 a_position;\n" +
"void main()\n" +
"{\n"  +
"    gl_Position = vec4(a_position, 0, 1);\n" +
"}\n";


const fragmentShaderTextPre =
`#version 300 es
precision mediump float;

out vec4 _outColor;
uniform vec3 iResolution;
uniform int iFrame;
uniform float iTime;

`;

const fragmentShaderTextPost= 
`
void main() {
 mainImage( _outColor, gl_FragCoord.xy );
}
`;


var shaderInitFailed = false;
var domLoaded = false;

//#insertdatapoint

//#insertsplashpoint

document.addEventListener("DOMContentLoaded", event => domLoaded = true );

class GLUniform {

    constructor(gl, prg, name, suffix) {
        this.gl = gl;
        this.prg = prg;
        this.name = name;
        this.suffix = suffix;
        this.location = this.gl.getUniformLocation(prg, name);
    }

    set(...values) {
        let method = "uniform" + this.suffix;
        let args = [this.location].concat(values);
        this.gl.useProgram(this.prg);
        this.gl[method].apply(this.gl, args);
    }
}

class GLProgram {
    constructor(gl, vertexShaderText, vertexShaderSourceMap, fragmentShaderText, fragmentShaderSourceMap) {
        this.gl = gl;
        this.prg = this.gl.createProgram();
        this.addShader(vertexShaderText, vertexShaderSourceMap, this.gl.VERTEX_SHADER);
        this.addShader(fragmentShaderText, fragmentShaderSourceMap, this.gl.FRAGMENT_SHADER);

        this.gl.linkProgram(this.prg);

        if (!this.gl.getProgramParameter(this.prg, this.gl.LINK_STATUS)) {
            const info = this.gl.getProgramInfoLog(this.prg);
            throw `Could not compile WebGL program. \n\n${info}`;
        }



        this.gl.useProgram(this.prg);

        this.time = new GLUniform(this.gl,this.prg, "iTime", "1f");
        this.frame = new GLUniform(this.gl, this.prg, "iFrame", "1i");
        this.resolution = new GLUniform(this.gl, this.prg, "iResolution", "3f");
        this.textureUniforms = [];
    }

    updateRes( width, height )
    {
        this.resolution.set(width, height, 1);
    }

    addShader(source, sourceMap, type) {
        let shader = this.gl.createShader(type);
        this.gl.shaderSource(shader, source);
        this.gl.compileShader(shader);
        let isCompiled = this.gl.getShaderParameter(shader, this.gl.COMPILE_STATUS);


        if (!isCompiled) {
            let errs = [];
            let er = this.gl.getShaderInfoLog(shader);

            let reg = /^ERROR: (\d+):(\d+): (.+)$/;

            for (let ln of er.split("\n")) {
                let m = reg.exec(ln);
                if (m != null) {
                    let lineNum = m[2];
                    let columnNum = m[1];
                    let errorMessage = m[3];

                    for (let sm of sourceMap) {
                        let smFile = sm[0];
                        let smStart = sm[1];
                        let smEnd = sm[2];
                        let smSrc = sm[3];

                        if (lineNum >= smStart && lineNum <= smEnd) {
                            errs.push(`Error: ${smFile}:${lineNum - smStart + smSrc}:${columnNum}: ${errorMessage}`);
                        }
                    }
                }
            }

            throw new Error(`Shader compile errors:\n${errs.join('\n')}`);
        }

        this.gl.attachShader(this.prg, shader);
    }

    

    addTexture(name, width, height, uint8Array)
    {
        var texture = this.gl.createTexture();
        this.gl.activeTexture(this.gl.TEXTURE0 + this.textureUniforms.length);
        this.gl.bindTexture(this.gl.TEXTURE_2D, texture);
        this.gl.texParameteri(this.gl.TEXTURE_2D, this.gl.TEXTURE_MAG_FILTER, this.gl.LINEAR);
        this.gl.texParameteri(this.gl.TEXTURE_2D, this.gl.TEXTURE_MIN_FILTER, this.gl.LINEAR);


        this.gl.texImage2D(
            this.gl.TEXTURE_2D,
            0,
            this.gl.RGBA,
            width, height,
            0,
            this.gl.RGBA, this.gl.UNSIGNED_BYTE,
            uint8Array
        );

        var nu = new GLUniform(this.gl, this.prg, name, "1i");
        nu.set(this.textureUniforms.length);
        this.textureUniforms.push(nu);
        // this.gl.generateMipmap(this.gl.TEXTURE_2D);
    }
    use() {
        this.gl.useProgram(this.prg);
    }
}

class GLRect {
    constructor( gl ) {
        this.gl= gl;
        this.verts = new Float32Array([-1, -1, 1, -1, -1, 1, 1, 1]);

        let buffer = this.gl.createBuffer();
        this.gl.bindBuffer(this.gl.ARRAY_BUFFER, buffer);
        this.gl.bufferData(this.gl.ARRAY_BUFFER, this.verts, this.gl.STATIC_DRAW);
    }

    render() {
        this.gl.drawArrays(this.gl.TRIANGLE_STRIP, 0, 4);
    }
}

function isBuiltIn(info) {
    const name = info.name;
    return name.startsWith("gl_") || name.startsWith("webgl_");
}


function glEnumToString(gl, value) {
    const keys = [];
    for (const key in gl) {
        if (gl[key] === value) {
            keys.push(key);
        }
    }
    return keys.length ? keys.join(' | ') : `0x${value.toString(16)}`;
}

function dumpGLProgram(gl, prg) {
    var output = '';
    var shaders = gl.getAttachedShaders(prg);

    output += `program
  #shaders: ${shaders.length}
`;

    {
        output += 'Uniforms:'
        const numUniforms = gl.getProgramParameter(prg, gl.ACTIVE_UNIFORMS);
        const indices = [...Array(numUniforms).keys()];
        const blockIndices = gl.getActiveUniforms(prg, indices, gl.UNIFORM_BLOCK_INDEX);
        const offsets = gl.getActiveUniforms(prg, indices, gl.UNIFORM_OFFSET);

        for (let ii = 0; ii < numUniforms; ++ii) {
            const uniformInfo = gl.getActiveUniform(prg, ii);
            if (isBuiltIn(uniformInfo)) {
                continue;
            }
            const { name, type, size } = uniformInfo;
            const blockIndex = blockIndices[ii];
            const offset = offsets[ii];

            output += `${name} ${glEnumToString(gl, type)}\n`;
        }

    }

    return output;
}


function dumpGL( gl ) {
    var output = 'dumping OpenGL\n';
    var prg = gl.getParameter(gl.CURRENT_PROGRAM);


    if (prg == null)
        output += 'No active program\n';
    else
        output += dumpGLProgram(gl, prg);

    console.log(output);
}

var offScreenWidth= 1920;
var offScreenHeight= 1080;

class ShaderScreen
{
    init()
    {
        this.texturesLoaded = 0;

        this.windowWidth= 0;
        this.windowHeight= 0;
        this.width= 0;
        this.height= 0;

        this.shaderCanvas = document.getElementById("maincanvas");

       this.gl = this.shaderCanvas.getContext("webgl2", { preserveDrawingBuffer: true });
        this.billboard = new GLRect(this.gl);

        this.shaderProgramError = null;

        dumpGL(this.gl);
    }

    Present( currentTime, currentFrame, program )
    {
        var ww = window.innerWidth;
        var wh = window.innerHeight;
        if (ww != this.windowWidth || wh != this.windowHeight) {
            this.windowWidth = ww;
            this.windowHeight = wh;
            this.shaderCanvas.setAttribute("width", this.windowWidth);
            this.shaderCanvas.setAttribute("height", this.windowHeight);
            program.updateRes(this.windowWidth, this.windowHeight);

            this.gl.viewport(0, 0, this.windowWidth, this.windowHeight);
        }

        let positionLocation = this.gl.getAttribLocation(program.prg, "a_position");
        this.gl.enableVertexAttribArray(positionLocation);
        this.gl.vertexAttribPointer(positionLocation, 2, this.gl.FLOAT, false, 0, 0);

        this.gl.enable(this.gl.BLEND);
        this.gl.blendFunc(this.gl.ONE, this.gl.ZERO);


        //var corr = 1.5;
        //var ww = this.shaderCanvas.width;
        //var wh = this.shaderCanvas.height;


        // shader.prg.use();
        this.gl.useProgram(program.prg);

        program.time.set(currentTime/1000.0);
        program.frame.set(currentFrame);
        

        this.billboard.render();
    }
}

function initShader(sh)
{
    var text = fragmentShaderTextPre + sh._.text + fragmentShaderTextPost;

    var prg = new GLProgram(screen.gl, vertexShaderText, "", text, sh._.sourceMap);
    sh._.program = prg;

    for (var u of sh._.uniforms)
        sh[u.name] = new GLUniform(screen.gl, sh._.program.prg, u.name, u.glType);

    sh._.textures.forEach(t => {
        var name = t.name;
        var dataUri = "data:image/png;base64," + t.data;

        const img = new Image();
        img.onload = function () {
            const ctx = loaderCanvas.getContext('2d');
            loaderCanvas.width = img.width;
            loaderCanvas.height = img.height;

            // Draw the image onto the canvas
            ctx.drawImage(img, 0, 0, img.width, img.height);

            // Get the image data, which is a Uint8ClampedArray (RGBA bytes)
            const imageData = ctx.getImageData(0, 0, img.width, img.height);
            const byteArray = imageData.data;

            sh._.program.addTexture(name, img.width, img.height, byteArray);
        };
        img.src = dataUri;
    });

}


function initShaders()
{
    try {
        initShadersInternal();
    }
    catch (e)
    {
        console.log(e);
        return [false, e];
    }

    return [true,''];
}

function sleep() {
//    var now = new Date().getTime();
//    while (new Date().getTime() < now + 1000*4) { /* Do nothing */ };
}
function rsleep() {
    var now = new Date().getTime();
    while (new Date().getTime() < now + 1000*4) { /* Do nothing */ };
}

var screen = new ShaderScreen();
var startTime = 0;
var frame = 0;
var stage = 0;
var stageDesc = '';
var error = null;
var shaderInitFailed = false;


//#ifdef MONITOR
setInterval(() => {
    fetch("http://localhost:6969/updated")
        .then(r => {
            if (r.status == 205) {
                console.log('reload!!!!!');
                window.location.reload();
            }
        });
}, 1000);
//#endif

var snd = new Sound();

snd.load(audioData);




var stages = [
    ['finishing load', (timeMs) => {
        if( domLoaded )
            stage++
    }], 
    ['initialising screen', (timeMs) => { screen.init(); sleep(); stage++; }],
    ['initialising shaders', (timeMs) => {

        if (shaderInitFailed)
            return;

        var [res, errorText] = initShaders();

        if (!res)
        {
            document.getElementById('progresstext').innerText = errorText;
            shaderInitFailed = true;
            return;
        }

        sleep();
        stage++;
    }],
//#ifdef USERWAIT
    ['displaying splash screen', (timeMs) => {
        var pd= document.getElementById('progressdiv');
        pd.innerHTML = splashHtml;
        pd.onclick = () => {
            document.getElementById("progressdiv").remove(); 
            snd.play();
            stage++;
        }
        stage++;
    }],
    ['waiting for user input', (timeMs) => {
    }],
//#else
    ['removing progress bar', (timeMs) => {
        document.getElementById("progressdiv").remove(); 
        snd.play();
        stage++;
    }],
//#endif
    ['marking start time', (timeMs) =>
        {
            startTime = timeMs;
            stage++;
        }
    ],
    ['running intro', (timeMs) => {
        var ut = timeMs - startTime;

        update(frame, ut/1000.0);
        screen.Present(ut, frame, curShader._.program);

//#ifdef MONITOR
        if (frame == 10)
            fetch("http://localhost:6969/testfinished");
//#endif

        frame++;
    }]
];


function animate(timeMs) 
{
    var e = document.getElementById("progresslabel");
    var st = stages[stage];

    if (e != null)
    {
        var txt;
        if (st[0] != null)
            txt = st[0];

        e.innerText = txt;
    }

    st[1](timeMs);


    requestAnimationFrame(animate);
}
    
animate(0);