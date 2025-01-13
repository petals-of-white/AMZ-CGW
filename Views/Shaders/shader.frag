#version 460 core

layout (location = 0) out vec4 FragColor;
layout (location = 1) out uvec4 RGBAColor;
layout (location = 2) out uvec4 MonochromeColor;
in vec3 TexCoord;

uniform usampler3D u_texture;
uniform float minPeak;
uniform float maxPeak;
uniform float windowWidth;
uniform float windowLevel;
uniform bool useLUT;

uniform usampler1D lutSampler;

float normalizeHistogram(float currentPixel, float minPeak, float maxPeak, float newMin, float newMax) {
	return newMin + (currentPixel - minPeak) / (maxPeak - minPeak) * (newMax-newMin);
}

float applyWindowLevel(float currentPixel, float ww, float wl, float newMin, float newMax) {
	float minVal = wl - 0.5 * ww;
	float maxVal = wl + 0.5 * ww;

	if (currentPixel <= minVal) { 
		return newMin;
	}
	else if (currentPixel > maxVal) { 
		return newMax; 
	} 
	else {
		return newMin + (currentPixel - wl + 0.5 * ww) * (newMax - newMin) / ww; 
	}

}



void main() {
	uint texValue = texture(u_texture, TexCoord).r;
	MonochromeColor = uvec4(texValue,texValue,texValue,texValue);
//	uint texValue = texValue.r;

	float newValue = normalizeHistogram(float(texValue), minPeak, maxPeak, 0.0, 255.0);
	RGBAColor = texelFetch(lutSampler,int(newValue),0);
	if (useLUT) {
		FragColor = vec4(RGBAColor.rgb/255.0, 1);
	}
	else {
		float normalized = normalizeHistogram(float(texValue), minPeak, maxPeak, 0.0, 1.0);
		FragColor = vec4(normalized,normalized,normalized, 1);

	}

}

