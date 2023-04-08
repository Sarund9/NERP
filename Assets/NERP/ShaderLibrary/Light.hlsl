#ifndef NERP_LIGHT_INCLUDED
#define NERP_LIGHT_INCLUDED

#ifndef NERP_COMMON_INCLUDED
#error including NERP/Light requires including NERP/Common
#endif

#ifndef NERP_SHADOWS_INCLUDED
#error including NERP/Light requires including NERP/Shadows
#endif

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

CBUFFER_START(_NerpLight)
int _DirectionalLightCount;
float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};

DirectionalShadowData GetDirectionalShadowData(int lightIndex) {
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
	return data;
}

int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}

Light GetDirectionalLight(int index, Surface surfaceWS) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData shadowData = GetDirectionalShadowData(index);
	light.attenuation = GetDirectionalShadowAttenuation(shadowData, surfaceWS);
	return light;
}

#endif
