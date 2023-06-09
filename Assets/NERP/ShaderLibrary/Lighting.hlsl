#ifndef NERP_LIGHTING_INCLUDED
#define NERP_LIGHTING_INCLUDED

#ifndef NERP_SURFACE_INCLUDED
#error including NERP/Lighting requires including NERP/Surface
#endif
#ifndef NERP_LIGHT_INCLUDED
#error including NERP/Lighting requires including NERP/Light
#endif
#ifndef NERP_BRDF_INCLUDED
#error including NERP/Lighting requires including NERP/BRDF
#endif
#ifndef NERP_GI_INCLUDED
#error including NERP/Lighting requires including NERP/BRDF
#endif

// 
float3 IncomingLight(Surface surface, Light light) {
	return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

// Calculates light that hits a surface from a Light
float3 GetLighting(Surface surface, BRDF brdf, Light light) {
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

// Calculates light that hits a surface
float3 GetLighting(Surface surfaceWS, BRDF brdf, GI gi) {
	ShadowData shadowData = GetShadowData(surfaceWS);
	shadowData.shadowMask = gi.shadowMask;
	//return gi.shadowMask.shadows.rgb;

	float3 color = IndirectBRDF(surfaceWS, brdf, gi.diffuse, gi.specular);
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		Light light = GetDirectionalLight(i, surfaceWS, shadowData);
		color += GetLighting(surfaceWS, brdf, light);
	}
	return color;
}


#endif
