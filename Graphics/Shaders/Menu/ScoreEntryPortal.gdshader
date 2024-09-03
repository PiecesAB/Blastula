shader_type canvas_item;
uniform vec4 color_multiplier = vec4(1.0);
uniform sampler2D main_tex: repeat_enable;
varying vec4 MODULATE;

void vertex() {
	MODULATE = COLOR;
}

void fragment() {
	float d = distance(UV, vec2(0.5, 0.5));
	float a = (atan(UV.y - 0.5, UV.x - 0.5) + PI) / PI;
	float ramp = pow(clamp(3.0 * (0.5 - d), 0.0, 2.0), 0.7);
	COLOR = color_multiplier * ramp * texture(main_tex, vec2(2.0 * d - 2.0 * TIME, 0.5 * a));
	COLOR = mix(COLOR, vec4(0.0,0.0,0.0,1.0), 1.2 - 3.0 * d);
	COLOR *= MODULATE;
}
