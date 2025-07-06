// ShadowCast.fx - Pixel-perfect 2D shadow casting shader
// This shader samples a shadow mask texture to determine light occlusion

sampler2D ShadowMask : register(s0);
sampler2D LightTexture : register(s1);

float4 PixelShaderFunction(float2 texCoord : TEXCOORD0) : COLOR0
{
    // Sample the shadow mask (1.0 = occluded, 0.0 = clear)
    float shadow = tex2D(ShadowMask, texCoord).r;
    
    // Sample the light texture
    float4 light = tex2D(LightTexture, texCoord);
    
    // If shadowed, reduce light intensity
    // shadow = 1.0 means fully occluded, shadow = 0.0 means fully lit
    float4 finalColor = light * (1.0 - shadow);
    
    return finalColor;
}

technique ShadowCast
{
    pass P0
    {
        PixelShader = compile ps_4_0_level_9_1 PixelShaderFunction();
    }
} 