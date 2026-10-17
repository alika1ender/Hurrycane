Shader "Hurrycane/PickupOutline"
{
    // Inverted-hull outline for items that can be picked up. Each vertex is pushed out from the item's centre (not along
    // its normal), so boxy props get an unbroken outline at the corners. Drawn by PickupHighlight with Graphics.RenderMesh.
    Properties
    {
        _Color ("Outline colour", Color) = (1, .82, .35, 1)
        _Width ("Outline width (m)", Float) = .012
        _Center ("Item centre (world)", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+20" }
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
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
            half4 Frag(Varyings i) : SV_Target { return _Color; }
            ENDHLSL
        }
    }
}
