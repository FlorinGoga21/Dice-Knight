// Shared by every Sprite FX Free shader, a port of the Godot pack's fx_common.gdshaderinc. The effects
// work as Godot's do: in the sprite's UV with y down from its top-left (sfx_v2f.uv), on gamma-space
// colours whatever the project's colour space, sizes in texture pixels. They read the sprite as one
// texture of its own (Mesh Type Full Rect), as Godot's read a Sprite2D's.

#include "UnitySprites.cginc"

float4 _MainTex_TexelSize;
float _PPU;                 // the sprite's pixels per unit, set by SpriteFX: for effects that move its corners
float _SfxHold, _SfxHeld;   // globals: with _SfxHold 1, the time is held at _SfxHeld (for still shots)

#define TIME (_SfxHold > 0.5 ? _SfxHeld : _Time.y)
#define TEXTURE_PIXEL_SIZE _MainTex_TexelSize.xy

struct sfx_v2f
{
    float4 pos : SV_POSITION;
    fixed4 color : COLOR;
    float2 uv : TEXCOORD0;      // y down, Godot's
    float4 screen : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

// The sprite's UV, y down.
float2 sfx_g(float2 uv)
{
    return float2(uv.x, 1.0 - uv.y);
}

// A vertex moved by `offset` (object units, before the sprite's flip), with UV `g` (y down).
sfx_v2f sfx_make(appdata_t v, float2 offset, float2 g)
{
    sfx_v2f o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.pos = UnityObjectToClipPos(UnityFlipSprite(v.vertex.xyz + float3(offset, 0.0), _Flip));
    o.color = v.color * _RendererColor;
    o.uv = g;
    o.screen = ComputeScreenPos(o.pos);
    return o;
}

sfx_v2f sfx_vert(appdata_t v)
{
    return sfx_make(v, float2(0.0, 0.0), sfx_g(v.texcoord));
}

// Exact sRGB, both ways.
float3 sfx_gamma(float3 c)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return c;
#else
    c = max(c, 0.0);
    return lerp(12.92 * c, 1.055 * pow(c, 1.0 / 2.4) - 0.055, step(0.0031308, c));
#endif
}

float3 sfx_linear(float3 c)
{
#ifdef UNITY_COLORSPACE_GAMMA
    return c;
#else
    c = max(c, 0.0);
    return lerp(c / 12.92, pow((c + 0.055) / 1.055, 2.4), step(0.04045, c));
#endif
}

// A colour property, in gamma space.
float4 sfx_col(float4 c)
{
    return float4(sfx_gamma(c.rgb), c.a);
}

// The sprite at a UV (y down), in gamma space.
float4 sfx_tex(float2 g)
{
    float4 c = tex2Dlod(_MainTex, float4(g.x, 1.0 - g.y, 0.0, 0.0));
    return float4(sfx_gamma(c.rgb), c.a);
}

float sfx_alpha(float2 g)
{
    return tex2Dlod(_MainTex, float4(g.x, 1.0 - g.y, 0.0, 0.0)).a;
}

// The effect's colour out, back in the project's colour space and tinted by the renderer's colour.
float4 sfx_out(float4 c, sfx_v2f i)
{
    return float4(sfx_linear(c.rgb), c.a) * i.color;
}

float sfx_mod(float x, float y)
{
    return x - y * floor(x / y);
}

float sfx_hash(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float sfx_noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(sfx_hash(i), sfx_hash(i + float2(1.0, 0.0)), u.x),
                lerp(sfx_hash(i + float2(0.0, 1.0)), sfx_hash(i + float2(1.0, 1.0)), u.x), u.y);
}

// 0..1, spread to cover the range.
float sfx_fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * sfx_noise(p);
        p = p * 2.03 + 17.0;
        a *= 0.5;
    }
    return saturate((v - 0.12) / 0.76);
}

float sfx_luma(float3 c)
{
    return dot(c, float3(0.299, 0.587, 0.114));
}

float3 sfx_hue_shift(float3 c, float turns)
{
    float a = turns * 6.2831853;
    float3 k = float3(0.57735, 0.57735, 0.57735);
    float ca = cos(a);
    return c * ca + cross(k, c) * sin(a) + k * dot(k, c) * (1.0 - ca);
}

// Ordered-dither thresholds in [0, 1).
float sfx_bayer2(float2 a)
{
    a = floor(a);
    return frac(dot(a, float2(0.5, a.y * 0.75)));
}

float sfx_bayer4(float2 a) { return sfx_bayer2(0.5 * a) * 0.25 + sfx_bayer2(a); }
float sfx_bayer8(float2 a) { return sfx_bayer4(0.5 * a) * 0.25 + sfx_bayer2(a); }

// 1 inside the 0..1 UV square, 0 outside: stops clamped edge pixels from smearing.
float sfx_in01(float2 uv)
{
    return step(0.0, uv.x) * step(uv.x, 1.0) * step(0.0, uv.y) * step(uv.y, 1.0);
}

// "a over b" for straight alpha.
float4 sfx_over(float4 a, float4 b)
{
    float o = a.a + b.a * (1.0 - a.a);
    float3 rgb = o > 0.0 ? (a.rgb * a.a + b.rgb * b.a * (1.0 - a.a)) / o : float3(0.0, 0.0, 0.0);
    return float4(rgb, o);
}

// A quad grown by `grow` texture pixels on every side, for an outline or glow round an unpadded sprite.
sfx_v2f sfx_grown(appdata_t v, float grow)
{
    float2 size = 1.0 / TEXTURE_PIXEL_SIZE;
    float2 offset = (v.texcoord * 2.0 - 1.0) * grow / _PPU;
    return sfx_make(v, offset, (sfx_g(v.texcoord) - 0.5) * (size + 2.0 * grow) / size + 0.5);
}
