shader_type canvas_item;
uniform lowp vec4 top_color = vec4(1.0);
uniform lowp vec4 bottom_color = vec4(0.0, 0.0, 0.0, 1.0);
// (amplitude, time frequency, space frequency)
uniform highp vec3 glisten_factor = vec3(0.2, 4.0, 0.2);
varying vec4 world_pos;

void vertex() {
	world_pos = MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0);
}

void fragment() {
	float lumin = COLOR.g;
	lumin += glisten_factor.x * sin(world_pos.y * glisten_factor.z - glisten_factor.y * TIME);
	float alpha = COLOR.a;
	COLOR = mix(bottom_color, top_color, lumin);
	COLOR.a = alpha;
}