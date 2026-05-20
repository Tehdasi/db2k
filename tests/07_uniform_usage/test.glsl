//#uniform int vvv


void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
    vec2 uv=(2.*fragCoord.xy-iResolution.xy)/iResolution.y;



    fragColor= vec4(0.1,0.3,0.5,1);



    if( uv.x > 1. || uv.x < -1. ||
        uv.y > 0.5 || uv.y < -0.5 || 
        
        mod( uv.x*4.0, 1. ) < 0.1 ||
        mod( uv.y*4.0, 1. ) < 0.1
        )
    {
        fragColor= vec4(0.05,0.15,0.25,1);
    }
    else
    {
         int fx= int((uv.x+1.)*4.0);
         int fy = 3 - int((uv.y+0.5)*4.0);


        if( ((vvv >> (fx + fy*8)) & 1)==1 )
        {
            fragColor= vec4(0.8,0.9,1,1);
        }
    }

}