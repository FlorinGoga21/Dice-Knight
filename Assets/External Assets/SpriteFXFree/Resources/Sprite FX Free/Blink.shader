// Invincibility-frame blink.
Shader "Sprite FX Free/Blink"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [ToggleUI] _Active ("Active", Float) = 1
        _Rate ("Rate", Range(0, 30)) = 10                            // blinks per second
        _MinAlpha ("Min Alpha", Range(0, 1)) = 0.15
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

            float _Active, _Rate, _MinAlpha;

            float4 frag(sfx_v2f i) : SV_Target
            {
                float4 tex = sfx_tex(i.uv);
                float a = _Active > 0.5 ? lerp(_MinAlpha, 1.0, step(0.5, frac(TIME * _Rate))) : 1.0;
                return sfx_out(float4(tex.rgb, tex.a * a), i);
            }
            ENDCG
        }
    }
}
