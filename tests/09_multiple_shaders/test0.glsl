//#uniform vec2 bit

void mainImage(out vec4 fragColor, in vec2 fragCoord)
{
	vec2 uv = fragCoord / iResolution.xy;
	vec3 col= vec3(0);

	if( length(uv-bit) < 0.5 )
		col= vec3(1);

	fragColor= vec4(col,1);
}
