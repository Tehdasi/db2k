//#texture "..\res\duck.png" duck
//#texture "..\res\snowflake.png" snowflake

// showing off lots of textures

vec3 hash31(float p)
{
   vec3 p3 = fract(vec3(p) * vec3(.1031, .1030, .0973));
   p3 += dot(p3, p3.yzx+33.33);
   return fract((p3.xxy+p3.yzz)*p3.zyx); 
}


void mainImage(out vec4 fragColor, in vec2 fragCoord) 
{
    vec2 uv = fragCoord / iResolution.xy;
    vec2 iuv= uv*vec2(1,-1);



    fragColor = mix(texture(duck,iuv,1.), texture(snowflake,iuv,1.), sin(iTime)*0.5 + 0.5 );
}