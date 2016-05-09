attribute vec3 fuVertex;
attribute vec3 fuNormal;
uniform mat4 FUSEE_MVP;
uniform mat4 FUSEE_MV;
uniform mat4 FUSEE_ITMV;
varying vec3 viewpos;
varying vec3 normal;

void main()
{
	normal = normalize(mat3(FUSEE_ITMV) * fuNormal);
	viewpos = (FUSEE_MV * vec4(fuVertex, 1.0)).xyz;
	gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);
}


