Shader "Dice Knight/Character Palette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _PaletteTint ("Palette Tint", Color) = (0.9, 0.92, 1, 1)
        _Saturation ("Saturation", Range(0, 1)) = 0.65
        _OutlineColor ("Outline Color", Color) = (0.04, 0.045, 0.06, 1)
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip ("Flip", Vector) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
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
            #include "Assets/External Assets/SpriteFXFree/Resources/Sprite FX Free/SpriteCommon.cginc"

            float4 _PaletteTint, _OutlineColor, _FlashColor;
            float _Saturation, _FlashAmount;

            float4 frag(sfx_v2f i) : SV_Target
            {
                float4 sprite = sfx_tex(i.uv);
                float2 pixel = TEXTURE_PIXEL_SIZE;
                float neighbors = max(max(sfx_alpha(i.uv + float2(pixel.x, 0)), sfx_alpha(i.uv - float2(pixel.x, 0))),
                    max(sfx_alpha(i.uv + float2(0, pixel.y)), sfx_alpha(i.uv - float2(0, pixel.y))));
                float3 color = lerp(sfx_luma(sprite.rgb).xxx, sprite.rgb, _Saturation) * sfx_col(_PaletteTint).rgb;
                float4 styled = sfx_over(float4(color, sprite.a), float4(sfx_col(_OutlineColor).rgb, neighbors));
                styled.rgb = lerp(styled.rgb, sfx_col(_FlashColor).rgb, _FlashAmount);
                return sfx_out(styled, i);
            }
            ENDCG
        }
    }
}
