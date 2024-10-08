﻿#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif


////////////////////
//Global variables//
////////////////////
float4x4 World;
float4x4 WorldViewProjection;

float LightPower;
float AmbientLightPower;
float LightAttenuation;

float3 LightPosition;
float3 HighlightColour;


//////////////////
//I/O structures//
//////////////////
struct VertexShaderInput
{
	float4 Position	: POSITION0;
	float3 Normal   : NORMAL0;
};

struct VertexShaderOutput
{
	float4 Position      : POSITION0;
	float3 Normal        : TEXCOORD0;
	float4 PositionWorld : TEXCOORD1; // float4 to keep optimiser happy
};


///////////
//Shaders//
///////////
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	output.Position = mul(input.Position, WorldViewProjection);
	output.Normal = normalize(mul(input.Normal, (float3x3)World));
	output.PositionWorld = mul(input.Position, World);

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float3 lightVector = input.PositionWorld.xyz - LightPosition;
	float lightAttenuationMultiplier = saturate(1.0f - length(lightVector)/LightAttenuation);
	float diffuseLightingFactor = saturate(dot(-normalize(lightVector), input.Normal))*lightAttenuationMultiplier*LightPower;

	float4 finalColour = float4(HighlightColour*(AmbientLightPower + diffuseLightingFactor), 1.0f);

	return finalColour;
}

technique VehicleTechnique
{
	pass Pass1
	{
		VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
		PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
	}
}