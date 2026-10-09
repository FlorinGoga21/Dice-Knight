// Hit flash: blends the sprite toward a flat colour.
Shader "Sprite FX Free/Flash"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
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

            float4 _FlashColor;
            float _FlashAmount;

            float4 frag(sfx_v2f i) : SV_Target
            {
                float4 tex = sfx_tex(i.uv);
                float4 fc = sfx_col(_FlashColor);
                return sfx_out(float4(lerp(tex.rgb, fc.rgb, _FlashAmount * fc.a), tex.a), i);
            }
            ENDCG
        }
    }
}
