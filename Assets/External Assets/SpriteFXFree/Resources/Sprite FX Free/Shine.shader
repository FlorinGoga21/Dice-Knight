// A band of light sweeping across: pickups, UI, rare items.
Shader "Sprite FX Free/Shine"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ShineColor ("Shine Color", Color) = (1, 1, 1, 0.85)
        _Width ("Width", Range(0, 1)) = 0.18
        _Angle ("Angle", Range(-80, 80)) = 25
        _Speed ("Speed", Range(0, 5)) = 1.2
        _Interval ("Interval", Range(0, 10)) = 1.2                   // seconds between sweeps
        _Progress ("Progress", Range(-1, 1)) = -1                    // 0 to 1 drives the band by hand; below 0 it runs on time
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

            float4 _ShineColor;
            float _Width, _Angle, _Speed, _Interval, _Progress;

            float4 frag(sfx_v2f i) : SV_Target
            {
                float4 tex = sfx_tex(i.uv);
                float k = tan(radians(_Angle));
                float s = i.uv.x + (i.uv.y - 0.5) * k;
                float t = _Progress >= 0.0 ? _Progress : sfx_mod(TIME * _Speed, 1.0 + _Interval * _Speed);
                float c = lerp(-_Width - abs(k) * 0.5, 1.0 + _Width + abs(k) * 0.5, t);
                float band = (1.0 - smoothstep(0.0, _Width * 0.5, abs(s - c))) * step(t, 1.0);
                float4 sc = sfx_col(_ShineColor);
                return sfx_out(float4(tex.rgb + sc.rgb * sc.a * band, tex.a), i);
            }
            ENDCG
        }
    }
}
