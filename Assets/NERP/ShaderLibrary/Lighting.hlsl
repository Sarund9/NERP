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

float SpecularStrength(Surface surface, BRDF brdf, Light light) {
	float3 h = SafeNormalize(light.direction + surface.viewDirection);
	float nh2 = Square(saturate(dot(surface.normal, h)));
	float lh2 = Square(saturate(dot(light.direction, h)));
	float r2 = Square(brdf.roughness);
	float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
	float normalization = brdf.roughness * 4.0 + 2.0;
	return r2 / (d2 * max(0.1, lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light) {
	return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

// 
float3 IncomingLight(Surface surface, Light light) {
	return saturate(dot(surface.normal, light.direction)) * light.color;
}

// Calculates light that hits a surface from a Light
float3 GetLighting(Surface surface, BRDF brdf, Light light) {
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

// Calculates light that hits a surface
float3 GetLighting(Surface surface, BRDF brdf) {
	float3 color = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		color += GetLighting(surface, brdf, GetDirectionalLight(i));
	}
	return color;
}


#endif
