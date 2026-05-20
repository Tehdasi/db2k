vec4 stripes(vec2 u, float xd, float yd)
{
	float v= sin((u.x*xd+u.y*yd)*10.0 - iTime)*.5 + .5;
	return vec4(v,v,v,1);
}

