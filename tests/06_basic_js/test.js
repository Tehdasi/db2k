//#shader "test.glsl" start

var coloredSquares = new Float32Array([
    0, 0, 0,
    0, 1, 0,
    1, 0, 0,
    0, 0, 1,
]);


function update(frame, frameTimeSec)
{
    curShader = start;
    start.ballPos.set(Math.sin(frameTimeSec) * 0.2, 0, 0);
    start.ballColor.set(0.8, 0.6, 0.4);

    start.rowBright.set(new Float32Array([0.25, 0.5, 0.75, 1]));

    coloredSquares[0] = Math.sin(frameTimeSec) * 0.4 + 0.5;
    coloredSquares[1] = coloredSquares[0];
    coloredSquares[2] = coloredSquares[0];
    start.pixels.set(coloredSquares);
}