#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif


/////////////
//Constants//
/////////////
static const int NumberOfVehicles = 2;


////////////////////
//Global variables//
////////////////////
float4x4 World;
float4x4 WorldViewProjection;

float LightPower;
float AmbientLightPower;
float LightAttenuation;
float SpecularExponent;

float3 CameraPosition;
float3 LightPosition;

float3 VehiclePositions[NumberOfVehicles];

Texture DiffuseTexture;
Texture NormalMapTexture;


//////////////////
//Sampler states//
//////////////////
sampler DiffuseTextureSampler = sampler_state
{
	texture = <DiffuseTexture>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};

sampler NormalMapTextureSampler = sampler_state
{
	texture = <NormalMapTexture>;
	minfilter = LINEAR;
	magfilter = LINEAR;
	mipfilter = LINEAR;
	AddressU = Wrap;
	AddressV = Wrap;
};


//////////////////
//I/O structures//
//////////////////
struct VertexShaderInput
{
	float4 Position           : POSITION0;
	float2 TextureCoordinates : TEXCOORD0;
	float3 Normal             : NORMAL0;
	float3 Tangent            : TANGENT0;
	float3 Binormal           : BINORMAL0;
};

struct VertexShaderOutput
{
	float4 Position           : POSITION0;
	float2 TextureCoordinates : TEXCOORD0;
	float3 Normal             : TEXCOORD1;
	float3 Tangent            : TEXCOORD2;
	float3 Binormal           : TEXCOORD3;
	float4 PositionWorld      : TEXCOORD4; // float4 to keep optimiser happy
};


///////////
//Shaders//
///////////
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	float3x3 world3x3 = (float3x3)World;

	output.Position = mul(input.Position, WorldViewProjection);
	output.TextureCoordinates = input.TextureCoordinates;
	output.Normal = normalize(mul(input.Normal, world3x3));
	output.Tangent = normalize(mul(input.Tangent, world3x3));
	output.Binormal = normalize(mul(input.Binormal, world3x3));
	output.PositionWorld = mul(input.Position, World);
	
	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float4 diffuseColour = tex2D(DiffuseTextureSampler, input.TextureCoordinates);

	float3 normalMapColour = 2.0f*tex2D(NormalMapTextureSampler, input.TextureCoordinates).rgb - 1.0f;
	float3 normal = normalize(normalMapColour.r*input.Tangent + normalMapColour.g*input.Binormal + normalMapColour.b*input.Normal);

	float3 lightVector = input.PositionWorld.xyz - LightPosition;
	float3 lightVectorNormalised = normalize(lightVector);
	float lightAttenuationMultiplier = saturate(1.0f - length(lightVector)/LightAttenuation);
	float diffuseLightingFactor = saturate(dot(-lightVectorNormalised, normal))*lightAttenuationMultiplier*LightPower;

	float3 cameraVector = normalize(CameraPosition - input.PositionWorld.xyz);
	float3 reflectionVector = normalize(reflect(lightVector, normal));
	float specularLightingFactor = pow(saturate(dot(reflectionVector, cameraVector)), SpecularExponent)*lightAttenuationMultiplier;

	float shadowFactor = 0.0f;
	for (int i = 0; i < NumberOfVehicles; i++)
	{
		//float shadowValue = 1.0f - saturate((length(input.PositionWorld.xyz - VehiclePositions[i]) - 1.0f)/(3.0f - 1.0f));
		float shadowValue = pow(saturate(dot(normalize(VehiclePositions[i] - LightPosition), lightVectorNormalised)), 128.0f);
		shadowFactor = shadowValue + shadowFactor*(1.0f - shadowValue);
	}
	float shadowMultiplier = 1.0f - shadowFactor;
	diffuseLightingFactor *= shadowMultiplier;
	specularLightingFactor *= shadowMultiplier;

	float4 finalColour = float4(diffuseColour.rgb*(AmbientLightPower + diffuseLightingFactor + specularLightingFactor), diffuseColour.a);

	return finalColour;
}

technique ArenaTechnique
{
	pass Pass1
	{
		VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
		PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
	}
};