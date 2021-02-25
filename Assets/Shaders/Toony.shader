Shader "Toony"
{
    Properties
    {
        [MainTexture] _Albedo("Gradient", 2D) = "white" {}
        
        _LightRamp("Light Ramp", 2D) = "white" {}
        
        _SpecularColor("Specular", Color) = (0.2, 0.2, 0.2)
        _SpecularRamp("Specular Ramp", 2D) = "white" {}
        _SpecularMask("Specular Mask", 2D) = "white" {}
        _SpecularRotation("Specular Rotation", Range(0.0, 360)) = 70
        _SpecularTilling("Specular Tilling", Vector) = (0.5, 0.5, 0, 0)
        _SpecularSmoothness("Specular Smoothness", Range(0.0, 1.0)) = 0.5
        
        _DirtMap("Dirt Map", 2D) = "black" {}
        _DirtColor("Dirt Color", Color) = (1, 1, 1)
        _DirtStrength("Dirt Strength", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline" "IgnoreProjector" = "True"}

        Pass
        {
            Name "StandardLit"
            Tags{"LightMode" = "UniversalForward"}

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE

            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #pragma multi_compile_instancing

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"

            struct VertInput
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
                float2 uvLM         : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct FragInput
            {
                float2 uv                       : TEXCOORD0;
                float2 uvLM                     : TEXCOORD1;
                float4 positionWSAndFogFactor   : TEXCOORD2;
                half3  normalWS                 : TEXCOORD3;
                float4 shadowCoord              : TEXCOORD6;
                float4 positionCS               : SV_POSITION;
                float2 screenPos                : TEXCOORD7;
            };

            sampler2D _Albedo;
            sampler2D _LightRamp;
            sampler2D _SpecularRamp;
            
            sampler2D _SpecularMask;
            float _SpecularRotation;
            float2 _SpecularTilling;
            float3 _SpecularColor;
            float _SpecularSmoothness;

            sampler2D _DirtMap;
            float3 _DirtColor;
            float _DirtStrength;
            
            FragInput LitPassVertex(VertInput input)
            {
                FragInput output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                float fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

                output.uv = input.uv;
                output.uvLM = input.uvLM.xy * unity_LightmapST.xy + unity_LightmapST.zw;

                output.positionWSAndFogFactor = float4(vertexInput.positionWS, fogFactor);
                output.normalWS = vertexNormalInput.normalWS;
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                output.positionCS = vertexInput.positionCS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                return output;
            }

            float2 RotateDegrees(float2 UV, float2 Center, float Rotation)
            {
                Rotation = Rotation * (3.1415926f/180.0f);
                UV -= Center;
                float s = sin(Rotation);
                float c = cos(Rotation);
                float2x2 rMatrix = float2x2(c, -s, s, c);
                rMatrix *= 0.5;
                rMatrix += 0.5;
                rMatrix = rMatrix * 2 - 1;
                UV.xy = mul(UV.xy, rMatrix);
                UV += Center;
                return UV;
            }

            float2 TilingAndOffset(float2 UV, float2 Tiling, float2 Offset)
            {
                return UV * Tiling + Offset;
            }
            
            float4 LightingBanded(float3 lightColor, float3 lightDir, float3 normal)
            {
                float NdotL = saturate(dot(normal, lightDir));
                return float4(lightColor * NdotL, NdotL);
            }

            float4 SpecularBanded(float3 lightColor, float3 lightDir, float3 normal, float3 viewDir, float3 specular, half smoothness)
            {
                float3 halfVec = SafeNormalize(float3(lightDir) + float3(viewDir));
                half NdotH = saturate(dot(normal, halfVec));
                half modifier = pow(NdotH, smoothness);
                return float4(lightColor * specular, modifier);
            }
            
            float4 LitPassFragment(FragInput input) : SV_Target
            {
                float2 SpecularUV      = RotateDegrees(input.uv, float2(0.5, 0.5), _SpecularRotation);
                SpecularUV             = TilingAndOffset(SpecularUV, _SpecularTilling, float2(0, 0));
                float3 SpecularMaskCol = tex2D(_SpecularMask, SpecularUV);
                
                float3 diffuse = tex2D(_Albedo, input.uv);
                diffuse += tex2D(_DirtMap, input.uv) * _DirtColor * _DirtStrength;
                
                float3 positionWS = input.positionWSAndFogFactor.xyz;
                float3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - positionWS);
                float3 bakedGI  = SampleLightmap(input.uvLM, input.normalWS);
                Light mainLight = GetMainLight(input.shadowCoord);
                MixRealtimeAndBakedGI(mainLight, input.normalWS, bakedGI, half4(0, 0, 0, 0));

                float Smoothness = exp(_SpecularSmoothness * 10 + 1);
                float3 attenuatedLightColor = mainLight.color * (mainLight.distanceAttenuation * mainLight.shadowAttenuation);
                
                float4 lightingColor = LightingBanded(attenuatedLightColor, mainLight.direction, input.normalWS);
                float4 specularColor = SpecularBanded(attenuatedLightColor, mainLight.direction, input.normalWS, viewDirectionWS, _SpecularColor, Smoothness);

                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, positionWS);
                    float3 attenuatedLightColor = light.color * (light.distanceAttenuation * light.shadowAttenuation);
                    lightingColor += LightingBanded(attenuatedLightColor, light.direction, input.normalWS);
                    specularColor += SpecularBanded(attenuatedLightColor, light.direction, input.normalWS, viewDirectionWS, _SpecularColor, Smoothness);
                }

                lightingColor.rgb *= tex2D(_LightRamp, float2(saturate(lightingColor.a / (pixelLightCount + 1)), 0)).r;
                lightingColor.rgb += bakedGI;

                specularColor.rgb *= tex2D(_SpecularRamp, float2(saturate(specularColor.a / (pixelLightCount + 1)), 0)).r;
                specularColor.rgb *= SpecularMaskCol.r;

                return float4(saturate(lightingColor.rgb * diffuse + specularColor.rgb), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}