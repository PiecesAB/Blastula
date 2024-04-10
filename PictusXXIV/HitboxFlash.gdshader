shader_type canvas_item;
render_mode blend_add;

void fragment() {
	COLOR.rgb = fract(COLOR.rgb - 3.0 * TIME);
}
