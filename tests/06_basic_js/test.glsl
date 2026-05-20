//#uniform vec3 ballPos
//#uniform vec3 ballColor

//#uniform float[4] rowBright

//#uniform vec3[4] pixels


// showing the interface between javascript and glsl code.

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv=(2.*fragCoord.xy-iResolution.xy)/iResolution.y;
    float m= 128.;
    
    
    int row= int((uv.y+1.)*2.);

    fragColor= vec4(vec3(rowBright[row]),1);

    float d= length(uv-ballPos.xy);

    if( d < 0.2 && d> 0.15 )
    {
        fragColor = vec4(ballColor,1);
    }


    if( uv.x > -0.3 && uv.x < 0.3 && uv.y < -0.3 && uv.y > -0.9 )
    {
        int xd= int((uv.x+0.3)*(2./.6));
        int yd= int((-uv.y-0.3)*(2./.6));


        fragColor= vec4(pixels[xd + yd*2],1);
    }
}