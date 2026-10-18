Shader "Hurrycane/TaskGlow"
{
    // Marks what the current task points at (TaskGlow.cs), visible through walls.
    // Pass 1 (hidden parts): a soft additive silhouette drawn only where something is in front (ZTest Greater).
    // Pass 2 (visible parts): an inverted-hull outline pushed out from the item's centre, like PickupOutline.
    Properties
    {
        _Color ("Glow colour", Color) = (1, .72, .2, 1)
        _Width ("Outline width (m)", Float) = .02
        _Center ("Target centre (world)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+30" }

        Pass
        {
            Name "ThroughWalls"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back
            ZWrite Off
            ZTest Greater
            Blend SrcAlpha One
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; float rim : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _Center;
                float _Width;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                float3 normal = normalize(TransformObjectToWorldNormal(v.normalOS));
                float3 view = normalize(GetWorldSpaceViewDir(world));
                o.rim = 1 - saturate(abs(dot(normal, view)));
                // Grow the silhouette a little (more for small things) so a book or a bottle still reads through a wall.
                float3 away = world - _Center.xyz;
                float3 dir = dot(away, away) > 1e-8 ? normalize(away) : normal;
                o.positionCS = TransformWorldToHClip(world + dir * _Width * 3);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target
            {
                // Brighter at the silhouette edge, faint fill in the middle.
                float a = _Color.a * (.5 + .5 * i.rim * i.rim);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="UniversalForward" }
            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            CBUFFER_START(UnityPerMaterial)
                float4 _Color, _Center;
                float _Width;
            CBUFFER_END
            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(v.positionOS.xyz);
                float3 away = world - _Center.xyz;
                float3 dir = dot(away, away) > 1e-8 ? normalize(away) : TransformObjectToWorldNormal(v.normalOS);
                o.positionCS = TransformWorldToHClip(world + dir * _Width);
                return o;
            }
            half4 Frag(Varyings i) : SV_Target { return half4(_Color.rgb, saturate(_Color.a * 1.6)); }
            ENDHLSL
        }
    }
}
