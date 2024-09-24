shader_type canvas_item;
uniform sampler2D main_tex: repeat_enable;
uniform vec4 main_color;
uniform vec2 grid_dimensions = vec2(24, 24);

void vertex() {
	UV.xy *= grid_dimensions;
}

void fragment() {
	vec2 v = UV;
	v.y = (v.y - fract(v.y)) + (1.0 - fract(v.y));
	v.x += TIME * 0.4 * (1.0 - 2.0 * mod(floor(v.y), 2.0)) * (1.0 + -0.2 * floor(v.y));
	COLOR = texture(main_tex, v) * main_color;
}
