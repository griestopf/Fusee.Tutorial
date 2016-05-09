#ifdef GL_ES
    precision highp float;
#endif
varying vec3 viewpos;
varying vec3 normal;
varying vec2 uv;
uniform vec3 albedo;
uniform float shininess;
uniform float specfactor;
uniform vec3 speccolor;
uniform vec3 ambientcolor;
uniform sampler2D texture;
uniform float texmix;

void main()
{
	vec3 nnormal = normalize(normal);
	
	// Diffuse
	vec3 lightdir = vec3(0, 0, -1);
    float intensityDiff = dot(nnormal, lightdir);
	vec3 resultingAlbedo = (1.0-texmix) * albedo + texmix * vec3(texture2D(texture, uv));

	// Specular
    float intensitySpec = 0.0;
	if (intensityDiff > 0.0)
	{
		vec3 viewdir = -viewpos;
		vec3 h = normalize(viewdir+lightdir);
		intensitySpec = specfactor * pow(max(0.0, dot(h, nnormal)), shininess);
	}

    gl_FragColor = vec4(ambientcolor + intensityDiff * resultingAlbedo + intensitySpec * speccolor, 1);
}
