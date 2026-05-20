//#uniform float[4] u_floats
//#uniform int[2] u_ints
//#uniform uint[3] u_uints
//#uniform vec3[2] u_vecs

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    float val = u_floats[0] + float(u_ints[0]) + float(u_uints[0]) + u_vecs[0].x;
    fragColor = vec4(val / 100.0, 0.5, 0.5, 1.0);
}
