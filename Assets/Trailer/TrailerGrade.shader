// Editor-only trailer colour grade (TrailerCompositor): exposure, contrast, saturation, warm highlights / teal shadows
// and a vignette, applied to each filmed frame before the titles go on top.
Shader "Hidden/TrailerGrade"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Exposure ("Exposure", Float) = 1
        _Contrast ("Contrast", Float) = 1.15
        _Saturation ("Saturation", Float) = 1.35
        _Split ("Split toning", Float) = 0.05
        _Vignette ("Vignette", Float) = 0.4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Exposure, _Contrast, _Saturation, _Split, _Vignette;

            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; float4 screen : TEXCOORD1; };

            v2f vert (appdata_full v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord.xy;
                o.color = v.color;
                o.screen = ComputeScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 tex = tex2D(_MainTex, i.uv) * i.color;
                float3 c = tex.rgb * _Exposure;
                // Work in gamma space so the curve looks the same as in a photo editor.
                float3 g = pow(saturate(c), 1.0 / 2.2);
                g = saturate((g - 0.5) * _Contrast + 0.5);
                float luma = dot(g, float3(0.2126, 0.7152, 0.0722));
                g = lerp(luma.xxx, g, _Saturation);
                g += _Split * (luma * float3(1.0, 0.45, -0.6) + (1 - luma) * float3(-0.5, 0.1, 0.6));
                float2 p = i.screen.xy / max(i.screen.w, 1e-5) - 0.5;
                g *= 1 - _Vignette * smoothstep(0.25, 0.85, length(p * float2(1.15, 1.0)));
                return float4(pow(saturate(g), 2.2), tex.a);
            }
            ENDCG
        }
    }
}
