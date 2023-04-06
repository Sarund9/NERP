#ifndef NERP_SURFACE_INCLUDED
#define NERP_SURFACE_INCLUDED

#ifndef NERP_COMMON_INCLUDED
#error including NERP/Surface requires including NERP/Common
#endif

struct Surface {
	float3 normal;
	float3 viewDirection;
	float3 color;
	float alpha;
	float metallic;
	float smoothness;
};

#endif
