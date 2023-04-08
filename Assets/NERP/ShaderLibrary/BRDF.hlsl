#ifndef NERP_BRDF_INCLUDED
#define NERP_BRDF_INCLUDED

#ifndef NERP_SURFACE_INCLUDED
#error including NERP/BRDF requires including NERP/Surface
#endif


struct BRDF {
	float3 diffuse;
	float3 specular;
	float roughness;
};

#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity(float metallic) {
	float range = 1.0 - MIN_REFLECTIVITY;
	return range - metallic * range;
}

// REVISE TUTORIAL REPOSITORY
// applyAlphaToDiffuse not used ?????
BRDF GetBRDF(Surface surface, bool applyAlphaToDiffuse = false) {
	BRDF brdf;
	float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);

	brdf.diffuse = surface.color * oneMinusReflectivity;
	brdf.diffuse *= surface.alpha;

	brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
	
	float perceptualRoughness =
		PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
	brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
	return brdf;
}

#endif
