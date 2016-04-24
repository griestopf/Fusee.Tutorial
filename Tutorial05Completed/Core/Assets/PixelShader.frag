#ifdef GL_ES
    precision highp float;
#endif
varying vec3 viewpos;
varying vec3 normal;
uniform vec3 albedo;
uniform float shininess;
uniform float specfactor;

void main()
{
	vec3 nnormal = normalize(normal);
	
	// Diffuse
	vec3 lightdir = vec3(0, 0, -1);
    float intensityDiff = dot(nnormal, lightdir);

	// Specular
    float intensitySpec = 0.0;
	if (intensityDiff > 0.0)
	{
		vec3 viewdir = -viewpos;
		vec3 h = normalize(viewdir+lightdir);
		intensitySpec = pow(max(0.0, dot(h, nnormal)), shininess);
	}

    gl_FragColor = vec4(intensityDiff * albedo + vec3(intensitySpec), 1);
}
