#ifdef GL_ES
    precision highp float;
#endif
varying vec3 modelpos;
varying vec3 normal;
uniform vec3 albedo;

void main()
{
    float intensity = dots(normal, vec3(0, 0, -1));
    gl_FragColor = vec4(intensity * albedo, 1);
}
