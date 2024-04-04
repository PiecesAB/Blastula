shader_type canvas_item;
uniform lowp vec4 tint = vec4(1.5, 1, 1, 1);
uniform mediump vec2 uv_center = vec2(0.5, 0.525);
// The center of the icon is assumed to be darker, and the rest is lighter.
// This shader fills in the center gradually.
uniform lowp float center_brightness_cutoff = 0.5;
uniform lowp float filled_fraction = 0.6;

void fragment() {
	float is_center = step(center_brightness_cutoff, 1.0 - COLOR.r);
	float pixel_filled = step((-filled_fraction + 0.5) * TAU, atan(-UV.x + uv_center.x, UV.y - uv_center.y));
	float filled_multiplier = mix(2.4, 0.5, 1.0 - pixel_filled);
	float unfilled_multiplier = mix(0.7, 1.0, pixel_filled);
	float multiplier = mix(unfilled_multiplier, filled_multiplier, is_center);
	multiplier *= mix(0.5 + 0.1 * sin(TIME * pixel_filled * 2.0), 1.0, step(0.999, filled_fraction));
	COLOR *= tint * multiplier;
}
