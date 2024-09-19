#if OPENGL
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

float3 CameraPosition;
float3 HorizonColour;
float3 ZenithColour;


//////////////////
//I/O structures//
//////////////////
struct VertexShaderInput
{
	float4 Position	: POSITION0;
};

struct VertexShaderOutput
{
	float4 Position      : POSITION0;
	float4 PositionWorld : TEXCOORD0; // float4 to keep optimiser happy
};


///////////
//Shaders//
///////////
VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
	VertexShaderOutput output;

	output.Position = mul(input.Position, WorldViewProjection);
	output.PositionWorld = mul(input.Position, World);

	return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
	float3 skyColour = lerp(HorizonColour, ZenithColour, saturate(dot(normalize(input.PositionWorld.xyz - CameraPosition), float3(0.0f, 1.0f, 0.0f))));

	float4 finalColour = float4(skyColour, 1.0f);

	return finalColour;
}

technique SkyboxTechnique
{
	pass Pass1
	{
		VertexShader = compile VS_SHADERMODEL VertexShaderFunction();
		PixelShader = compile PS_SHADERMODEL PixelShaderFunction();
	}
}