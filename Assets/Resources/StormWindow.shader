Shader "Hurrycane/StormWindow"
{
    Properties
    {
        _Storm ("Storm intensity", Range(0,1)) = 0
        _Aspect ("Window aspect", Float) = 1
        _Seed ("Window variation", Float) = 0
        _FinalStorm ("Final day disaster", Range(0,1)) = 0
        _WeatherTime ("Continuous weather clock", Float) = 0
        _Flash ("Lightning flash", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry+10" }
        Cull Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float _Storm, _Aspect, _Seed, _FinalStorm, _WeatherTime, _Flash;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS = TransformObjectToHClip(v.positionOS.xyz); o.uv = v.uv; return o;
            }
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7))) * 43758.5453); }
            float Noise(float2 p)
            {
                float2 i=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            half4 Frag(Varyings i) : SV_Target
            {
                float t=_WeatherTime, storm=smoothstep(.15,1,_Storm);
                float2 uv=i.uv, p=uv*float2(_Aspect,1);
                float finale=saturate(_FinalStorm);
                float gust=.5+.5*sin(t*1.7+sin(t*.63)*2);
                float speed=lerp(.65,.85,storm)*(1+finale*1.7);
                float2 wind=float2(t*speed+_Seed,t*.12);
                float cloud=Noise(p*3+wind)*.6+Noise(p*7+wind*1.7)*.28+Noise(p*17+wind*2)*.12;
                float3 sky=lerp(float3(.57,.64,.66),float3(.055,.087,.105),_Storm);
                sky*=.64+cloud*.68;
                sky+=pow(saturate(uv.y),2)*lerp(.05,.015,storm);
                sky*=lerp(1,.32,finale);
                // Multiple sparse, diagonal rain layers moving at different speeds.
                float rain=0;
                for(int layer=0;layer<3;layer++)
                {
                    float2 q=p*float2(30+layer*14,4+layer*3);
                    q.x+=q.y*lerp(.9,2.6,finale)+t*(8+layer*3)*(1+finale*1.5);
                    q.y+=t*(19+layer*8)*(1+finale*.65);
                    float2 cell=floor(q), f=frac(q);
                    float seed=Hash(cell+_Seed+layer);
                    rain+= (1-smoothstep(.01,lerp(.055,.105,finale),abs(f.x-.5))) * smoothstep(0,.15,f.y)
                        * (1-smoothstep(.65,.95,f.y))*step(lerp(.8,.44,finale),seed);
                }
                sky+=rain*lerp(.004,.12,storm);
                // Fast, broad curtains of wind-driven rain in the final disaster.
                float squall=Noise(float2(p.x*9+p.y*5-t*3.8,p.y*2+t*.7));
                sky+=finale*float3(.07,.09,.10)*smoothstep(.48,.82,squall)*(.4+gust*.6);
                // Windblown leaf/debris silhouettes: drift, tumble and cross at irregular intervals.
                float2 debris=p*float2(4,3)+float2(-t*lerp(.04,1.4,storm)*(1+finale*1.5),t*(.25+finale*.5));
                float2 cell=floor(debris), f=frac(debris)-.5;
                float h=Hash(cell+_Seed);
                float angle=t*(h*5+2)+h*31;
                float2 leaf=float2(f.x*cos(angle)-f.y*sin(angle),f.x*sin(angle)+f.y*cos(angle));
                float2 leafSize=float2(.12,.027)*(1+finale*.65);
                float silhouette=(1-smoothstep(.8,1,dot(leaf/leafSize,leaf/leafSize)))*step(lerp(.79,.48,finale),h);
                sky*=1-silhouette*storm*.85;
                // Broad gust shadows passing over the outside of the pane.
                sky*=1-storm*lerp(.22,.6,finale)*smoothstep(.42,.82,Noise(float2(p.x*2-t*(.8+finale*1.4),p.y*1.3+_Seed)));
                // Lightning: the whole sky lights up white-violet, clouds showing as darker shapes.
                sky=lerp(sky,float3(.86,.88,.95)*(.55+cloud*.6),saturate(_Flash));
                return half4(sky,1);
            }
            ENDHLSL
        }
    }
}
