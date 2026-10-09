// Noise dissolve with a glowing edge. Progress 0 = whole, 1 = gone.
Shader "Sprite FX Free/Dissolve"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0, 1)) = 0
        _NoiseScale ("Noise Scale", Range(1, 64)) = 10
        _EdgeWidth ("Edge Width", Range(0, 0.3)) = 0.08
        _EdgeColor ("Edge Color", Color) = (1, 0.55, 0.1, 1)
        _EdgeIntensity ("Edge Intensity", Range(1, 4)) = 1.5
        _Direction ("Direction", Vector) = (0, 1, 0, 0)              // a straight sweep blended into the noise, y up: (0, 1) goes bottom to top
        _DirectionMix ("Direction Mix", Range(0, 1)) = 0
        [ToggleUI] _PixelSnap ("Pixel Snap", Float) = 1              // noise in whole texture pixels, for pixel art
        _Seed ("Seed", Float) = 0
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

            float4 _EdgeColor, _Direction;
            float _Progress, _NoiseScale, _EdgeWidth, _EdgeIntensity, _DirectionMix, _PixelSnap, _Seed;

            float4 frag(sfx_v2f i) : SV_Target
            {
                float4 tex = sfx_tex(i.uv);
                float2 uv = _PixelSnap > 0.5 ? (floor(i.uv / TEXTURE_PIXEL_SIZE) + 0.5) * TEXTURE_PIXEL_SIZE : i.uv;
                float n = sfx_fbm(uv * _NoiseScale + _Seed * 31.7);
                float2 dir = float2(_Direction.x, -_Direction.y);
                if (_DirectionMix > 0.0 && length(dir) > 0.0)
                    n = lerp(n, dot(uv - 0.5, normalize(dir)) + 0.5, _DirectionMix);
                float t = lerp(-_EdgeWidth, 1.0, _Progress);
                float gone = step(n, t);
                float edge = (1.0 - gone) * step(n, t + _EdgeWidth) * step(0.001, _Progress);
                float3 rgb = lerp(tex.rgb, sfx_col(_EdgeColor).rgb * _EdgeIntensity, edge);
                return sfx_out(float4(rgb, tex.a * (1.0 - gone)), i);
            }
            ENDCG
        }
    }
}
