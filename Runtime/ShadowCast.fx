// ShadowCast.fx - MonoGame HLSL effect for 2D pixel-perfect shadows

sampler2D ShadowMask : register(s0);

float2 LightPos;      // Light position in screen space (0-1)
float LightRadius;    // Light radius in screen space (0-1, relative to width)
float2 ScreenSize;    // Screen size in pixels

float4 PixelShaderFunction(float2 texCoord : TEXCOORD0) : COLOR0
{
    float2 pixelPos = texCoord * ScreenSize;
    float2 lightPosPx = LightPos * ScreenSize;
    float2 dir = pixelPos - lightPosPx;
    float dist = length(dir);

    if (dist > LightRadius * ScreenSize.x) // outside light
        return float4(0,0,0,1);

    float2 step = normalize(dir);
    float shadow = 0;
    for (float t = 0; t < dist; t += 2.0) // step by 2 pixels for speed
    {
        float2 samplePos = lightPosPx + step * t;
        float mask = tex2D(ShadowMask, samplePos / ScreenSize).r;
        if (mask > 0.5) // occluder hit
        {
            shadow = 1;
            break;
        }
    }

    // If shadowed, return black; else white (lit)
    return lerp(float4(1,1,1,1), float4(0,0,0,1), shadow);
}

technique Technique1
{
    pass Pass1
    {
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
} 