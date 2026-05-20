//#test norun
//#splash "splash.html"

// Showing usage of a custom splash screen when waiting for user input.

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv = (fragCoord / iResolution.xy);
    float m= 128.;
    
    // a pretty XOR background.
    fragColor = vec4(vec3(float(int(uv.x*m)^int(uv.y*m))/m), 1.0);
}