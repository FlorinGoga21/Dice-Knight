// Pixel-perfect outline, around the sprite or on its own edge pixels.
Shader "Sprite FX Free/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        [IntRange] _Width ("Width", Range(0, 16)) = 1
        [ToggleUI] _Diagonals ("Diagonals", Float) = 1
        [ToggleUI] _Inside ("Inside", Float) = 0                     // on the sprite's own edge pixels instead of around it
        [ToggleUI] _OutlineOnly ("Outline Only", Float) = 0
        [IntRange] _Grow ("Grow", Range(0, 16)) = 0                  // grows the quad, in texture pixels, for unpadded sprites
        _PulseSpeed ("Pulse Speed", Range(0, 20)) = 0
        _PulseMinAlpha ("Pulse Minimum Alpha", Range(0, 1)) = 0.35
        _PulseMaxAlpha ("Pulse Maximum Alpha", Range(0, 1)) = 1
        _PPU ("Pixels Per Unit", Float) = 100                        // the sprite's; SpriteFX sets it
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
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "SpriteCommon.cginc"

            float4 _OutlineColor;
            float _Width, _Diagonals, _Inside, _OutlineOnly, _Grow;
            float _PulseSpeed, _PulseMinAlpha, _PulseMaxAlpha;

            #define ALPHA_AT(p) (sfx_alpha(p) * sfx_in01(p))

            sfx_v2f vert(appdata_t v)
            {
                return sfx_grown(v, _Grow);
            }

            float4 frag(sfx_v2f i) : SV_Target
            {
                float4 tex = sfx_tex(i.uv) * sfx_in01(i.uv);
                float nearMax = 0.0;
                float nearMin = 1.0;
                [loop] for (int k = 1; k <= 16; k++)
                {
                    if (k > _Width)
                        break;
                    float2 px = TEXTURE_PIXEL_SIZE * k;
                    float4 a = float4(ALPHA_AT(i.uv + float2(px.x, 0.0)), ALPHA_AT(i.uv - float2(px.x, 0.0)),
                                      ALPHA_AT(i.uv + float2(0.0, px.y)), ALPHA_AT(i.uv - float2(0.0, px.y)));
                    nearMax = max(nearMax, max(max(a.x, a.y), max(a.z, a.w)));
                    nearMin = min(nearMin, min(min(a.x, a.y), min(a.z, a.w)));
                    if (_Diagonals > 0.5)
                    {
                        float4 d = float4(ALPHA_AT(i.uv + px), ALPHA_AT(i.uv - px),
                                          ALPHA_AT(i.uv + float2(px.x, -px.y)), ALPHA_AT(i.uv + float2(-px.x, px.y)));
                        nearMax = max(nearMax, max(max(d.x, d.y), max(d.z, d.w)));
                        nearMin = min(nearMin, min(min(d.x, d.y), min(d.z, d.w)));
                    }
                }
                float4 oc = sfx_col(_OutlineColor);
                if (_PulseSpeed > 0.0)
                {
                    float pulse = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    oc.a *= lerp(_PulseMinAlpha, _PulseMaxAlpha, pulse);
                }
                float4 result;
                if (_Inside > 0.5)
                {
                    float edge = step(0.5, tex.a) * step(nearMin, 0.5);
                    result = float4(lerp(tex.rgb, oc.rgb, edge * oc.a), tex.a);
                }
                else if (_OutlineOnly > 0.5)
                    result = float4(oc.rgb, saturate(nearMax - tex.a) * oc.a);
                else
                    result = sfx_over(tex, float4(oc.rgb, nearMax * oc.a));
                return sfx_out(result, i);
            }
            ENDCG
        }
    }
}
