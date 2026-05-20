//#option startpause false

// A basic spinning cube.
// Note that the starting pause is disabled due to there being no audio in this demo.

void mainImage(out vec4 F, in vec2 C)
{
    vec2 r=iResolution.xy,u=(2.*C.xy-r)/r.y;
    vec3 w = vec3(0,0,-3.),p;
    float l=.0,t=iTime+.3,s=sin(t),c=cos(t);

    for (int i=0;i<40;i++)
    {
        p=w+normalize(vec3(u,1))*l;
        mat2 m=mat2(c,s,-s,c);
        p.xy*=m;
        p.zy*=m;
        p=abs(p)-1.;
        l+=length(max(p, 0.0)) + min(max(p.x, max(p.y, p.z)), 0.0);
    }

    F = vec4(vec3(1.2 - l * .4), 1);
}