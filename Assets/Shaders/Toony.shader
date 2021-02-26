Shader "Toony"
{
    Properties
    {
        [MainTexture] _Albedo("Albedo", 2D) = "white" {}
        [Toggle(_Gradient)] _Gradient("Is Gradient", Float) = 1.0
        _GradientX("Gradient X", Range(0, 1)) = 0
        
        _SpecularColor("Specular", Color) = (0.7, 0.7, 0.7)
        _SpecularTilling("Specular Tilling", float) = 20
        _SpecularSmoothness("Specular Smoothness", Range(0.0, 1.0)) = 0.5
        
        _DirtColor("Dirt Color", Color) = (1, 1, 1)
        _DirtStrength("Dirt Strength", Range(0.0, 1.0)) = 0.5
        
        [Toggle(_RIM_HIGHLIGHT)] _RimHighlight("Rim Highlight", Float) = 0
        _RimColor("Rim Color", Color) = (1, 1, 1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.7
        
        [HideInInspector] _GradMinMax("Grad MinMax", Vector) = (0, 0, 0, 0)
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

            #pragma multi_compile _ _RIM_HIGHLIGHT
            #pragma multi_compile _ _Gradient
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
                float2 uv               : TEXCOORD0;
                float2 uvLM             : TEXCOORD1;
                float4 positionWSAndFog : TEXCOORD2;
                float3 normalWS         : TEXCOORD3;
                float4 shadowCoord      : TEXCOORD6;
                float4 positionCS       : SV_POSITION;
            };

            sampler2D _Albedo;
            float4 _Albedo_ST;
            sampler2D _LightRamp;
            sampler2D _SpecularRamp;
            float _GradientX;
            
            sampler2D _SpecularMask;
            float _SpecularTilling;
            float3 _SpecularColor;
            float _SpecularSmoothness;

            sampler2D _DirtMap;
            float4 _DirtMap_ST;
            float3 _DirtColor;
            float _DirtStrength;

            float3 _RimColor;
            float _RimAmount;
            
            float2 _GradMinMax;
            
            FragInput LitPassVertex(VertInput input)
            {
                VertexPositionInputs vertexInput       = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs   vertexNormalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                FragInput output;
                output.positionCS           = vertexInput.positionCS;
                output.uv                   = input.uv;
                output.uvLM                 = input.uvLM.xy;
                output.positionWSAndFog.xyz = vertexInput.positionWS;
                output.positionWSAndFog.w   = ComputeFogFactor(vertexInput.positionCS.z);
                output.normalWS             = vertexNormalInput.normalWS;
                output.shadowCoord          = TransformWorldToShadowCoord(vertexInput.positionWS);
                return output;
            }

            float2 CalculateAlbedoUV(FragInput Input)
            {
                float2 UV;
                #if _Gradient
                UV = float2(_GradientX, saturate((Input.positionWS.y - _GradMinMax.x) / (_GradMinMax.y - _GradMinMax.x)));
                #else
                UV = Input.uv;
                #endif
                return TRANSFORM_TEX(UV, _Albedo);
            }
            
            float4 ToonyLighting(Light light, float3 normalWS)
            {
                float NdotL = saturate(dot(normalWS, light.direction));
                return float4(light.color, saturate(NdotL * (light.distanceAttenuation * light.shadowAttenuation)));
            }

            float4 ToonySpecular(Light light, float3 normalWS, float3 viewDir, half smoothness)
            {
                float3 halfVec = SafeNormalize(float3(light.direction) + float3(viewDir));
                half   NdotH = saturate(dot(normalWS, halfVec));
                half   modifier = pow(NdotH, smoothness);
                return float4(light.color * _SpecularColor, modifier * (light.distanceAttenuation * light.shadowAttenuation));
            }

            float3 CalculateToonyLighting(FragInput input, float3 Albedo, float3 viewDirectionWS)
            {
                float  smoothness      = exp(_SpecularSmoothness * 10 + 1);     
                float3 bakedGI         = SampleLightmap(input.uvLM * unity_LightmapST.xy + unity_LightmapST.zw, input.normalWS);
                Light  mainLight       = GetMainLight(input.shadowCoord);
                
                MixRealtimeAndBakedGI(mainLight, input.normalWS, bakedGI, half4(0, 0, 0, 0));
           
                float4 lightingColor = ToonyLighting(mainLight, input.normalWS);
                float4 specularColor = ToonySpecular(mainLight, input.normalWS, viewDirectionWS, smoothness);
                
                #ifdef _ADDITIONAL_LIGHTS
                uint pixelLightCount = GetAdditionalLightsCount();
                for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
                {
                    Light light = GetAdditionalLight(lightIndex, input.positionWSAndFog.xyz);
                    lightingColor += ToonyLighting(light, input.normalWS);
                    specularColor += ToonySpecular(light, input.normalWS, viewDirectionWS, smoothness);
                }
                #else
                uint pixelLightCount = 0;
                #endif
                
                pixelLightCount   += 1;
                lightingColor.rgb *= tex2D(_LightRamp, float2(saturate(lightingColor.a / pixelLightCount), 0)).r;
                lightingColor.rgb += bakedGI;

                specularColor.rgb *= tex2D(_SpecularRamp, float2(saturate(specularColor.a / pixelLightCount), 0)).r;
                specularColor.rgb *= tex2D(_SpecularMask, input.uvLM * _SpecularTilling).r;

                return Albedo * lightingColor + specularColor;
            }

            float3 CalculateRim(FragInput input, float3 viewDirectionWS)
            {
                #if _RIM_HIGHLIGHT
                float4 rimDot = 1 - dot(viewDirectionWS, input.normalWS);
                float rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimDot);
                return rimIntensity * _RimColor;
                #else
                return 0;
                #endif
            }
            
            float4 LitPassFragment(FragInput input) : SV_Target
            {
                input.normalWS = SafeNormalize(input.normalWS);
                
                float3 viewDirectionWS = SafeNormalize(GetCameraPositionWS() - input.positionWSAndFog.xyz);
                float3 albedo = tex2D(_Albedo,  CalculateAlbedoUV(input));
                albedo += tex2D(_DirtMap, TRANSFORM_TEX(input.uv, _DirtMap)) * _DirtColor * _DirtStrength;
                albedo = CalculateToonyLighting(input, albedo, viewDirectionWS);
                albedo += CalculateRim(input, viewDirectionWS);
                albedo = MixFog(albedo, input.positionWSAndFog.w);
                
                return float4(saturate(albedo), 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/Meta"
    }
}