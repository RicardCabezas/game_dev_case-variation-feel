Shader "Game/Enemies/Bee Hit Blink"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (1, 1, 1, 1)
		_HitColor("Hit Color", Color) = (1, 1, 1, 1)
		_HitFlash("Hit Flash", Range(0, 1)) = 0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

		Pass
		{
			Name "ForwardLit"
			Tags { "LightMode" = "UniversalForward" }

			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment
			#pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

			CBUFFER_START(UnityPerMaterial)
			half4 _BaseColor;
			half4 _HitColor;
			half _HitFlash;
			CBUFFER_END

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
			};

			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				half3 normalWS : TEXCOORD0;
			};

			Varyings Vertex(Attributes input)
			{
				Varyings output;
				output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
				output.normalWS = TransformObjectToWorldNormal(input.normalOS);
				return output;
			}

			half4 Fragment(Varyings input) : SV_Target
			{
				half3 normalWS = normalize(input.normalWS);
				Light mainLight = GetMainLight();
				half diffuse = saturate(dot(normalWS, mainLight.direction));
				half3 litColor = _BaseColor.rgb * (SampleSH(normalWS) + mainLight.color * diffuse);
				return half4(lerp(litColor, _HitColor.rgb, _HitFlash), _BaseColor.a);
			}
			ENDHLSL
		}

		UsePass "Universal Render Pipeline/Lit/ShadowCaster"
		UsePass "Universal Render Pipeline/Lit/DepthOnly"
	}
}
