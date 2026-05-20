//#test norun
//#audio "..\res\audio_oneshot.mp3"


// Showing the sync of audio to visuals.
//
// The audio file runs for 2.008s and has a drum hit that happens at .7s.
// Since the audio loops and we are also looping the visuals the flash should be 
// in sync. 
// Well, in theory. It will prolly slowly go out of sync. 

const float maxTime= 2.008;


void stick( out float v, float t, float mag)
{
    float tm= mod(iTime, maxTime);
    float tt= (tm-t)*10.;
    v= max( v, (exp(-tt)*(sign(tt)*0.5+0.5))*mag );
}

float tick()
{
    float v= 0.;
    stick(v, .7, 1.);
    return v;
}


void mainImage(out vec4 o, vec2 fc)
{
    o= vec4(tick(),0,0,1);
}