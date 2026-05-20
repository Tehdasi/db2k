//#shader "test0.glsl" one
//#shader "test1.glsl" two


function update(frame, ut) {

    if (Math.floor(frame / 100) % 2 == 0)
    {
        one.bit.set(0, Math.sin(ut));
        curShader = one;
    }
    else
        curShader = two;
}