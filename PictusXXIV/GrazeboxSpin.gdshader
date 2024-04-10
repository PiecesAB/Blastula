shader_type canvas_item;
render_mode blend_add;
uniform lowp float opacity = 0.5;

void fragment() {
	mediump float uvArg = atan(UV.y - 0.5, UV.x - 0.5);
	float uvLen = distance(vec2(0.5, 0.5), UV.xy);
	vec2 uvA = uvLen * vec2(cos(uvArg + TIME), sin(uvArg + TIME)) + vec2(0.5, 0.5);
	vec2 uvB = uvLen * vec2(-cos(uvArg - TIME), sin(uvArg - TIME)) + vec2(0.5, 0.5);
	COLOR = opacity * (texture(TEXTURE, uvA) + texture(TEXTURE, uvB));
}
