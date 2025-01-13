#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec2 aTexCoord;
uniform mat4 u_transform_matrix;
out vec3 TexCoord;

void main() {
	gl_Position = vec4(aPos, 0, 1.0);
	vec4 fullCord = (u_transform_matrix * vec4(aTexCoord, 0, 1));
	TexCoord = fullCord.xyz/fullCord.w;
}