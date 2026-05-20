//#include "common.glsl"

//#include "a.glsl"
//#include "d.glsl"


// shows the usage of multiple include commands.


void mainImage(out vec4 o, vec2 fc)
{
    vec2 r=iResolution.xy,u=(2.*fc.xy-r)/r.y;

    if( u.x >= 0. && u.y >= 0. )
    {
        o= topRight(u);
    }
    else if(u.x < 0. && u.y >= 0. )
    {
        o= topLeft(u);
    }
    else if(u.x < 0. && u.y < 0. )
    {
        o= bottomLeft(u);
    }
    else
        o= bottomRight(u);
}