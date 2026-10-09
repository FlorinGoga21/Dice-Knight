// Hue rotation, static or cycling (rainbow), with saturation control.
Shader "Sprite FX Free/HueShift"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Shift ("Shift", Range(0, 1)) = 0
        _Speed ("Speed", Range(0, 4)) = 0                            // full hue turns per second; 0 holds
        _Saturation ("Saturation", Range(0, 2)) = 1
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex sfx_vert
            #pragma fragment frag
            #pragma target 3.0
            #include "SpriteCommon.cginc"

            float _Shift, _Speed, _Saturation;

            float4 frag(sfx_v2f i) : SV_Target
            {
                float4 tex = sfx_tex(i.uv);
                float3 rgb = sfx_hue_shift(tex.rgb, _Shift + TIME * _Speed);
                rgb = lerp(sfx_luma(rgb).xxx, rgb, _Saturation);
                return sfx_out(float4(saturate(rgb), tex.a), i);
            }
            ENDCG
        }
    }
}
