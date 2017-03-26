attribute vec3 fuVertex;
attribute vec3 fuNormal;
attribute vec2 fuUV;
uniform mat4 FUSEE_MVP;
uniform mat4 FUSEE_MV;
uniform mat4 FUSEE_ITMV;
varying vec3 normal;
varying vec3 viewpos;
varying vec2 uv;

void main()
{
	normal = normalize(mat3(FUSEE_ITMV) * fuNormal);
	viewpos = (FUSEE_MV * vec4(fuVertex, 1.0)).xyz;
	uv = fuUV;
	gl_Position = FUSEE_MVP * vec4(fuVertex, 1.0);

}


