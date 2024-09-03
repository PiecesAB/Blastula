shader_type canvas_item;
uniform vec2 offset_outer = vec2(-2000, 1200);
uniform vec2 outer_multiplier = vec2(1.0, 0.0);
uniform vec2 offset_inner = vec2(10000, 0);

void vertex() {
	vec2 center = (MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0)).xy + offset_inner;
	float real_multiplier = max(0, outer_multiplier.x + 0.001 * (center.x + center.y));
	real_multiplier = pow(real_multiplier, 4.0);
	VERTEX.x += 1.0 * sin(TIME * 3.0 + 0.01 * center.x + 0.02 * center.y) + offset_outer.x * real_multiplier;
	VERTEX.y += 1.0 * sin(TIME * 2.0 + 0.01 * center.y - 0.02 * center.x) + offset_outer.y * real_multiplier;
}
