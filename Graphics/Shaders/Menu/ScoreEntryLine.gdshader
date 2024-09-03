shader_type canvas_item;
uniform vec4 color_multiplier = vec4(1.0);
uniform sampler2D main_tex: repeat_enable;
varying vec4 MODULATE;

void vertex() {
	MODULATE = COLOR;
}

void fragment() {	
	COLOR = texture(main_tex, vec2(-5.0 * TIME + UV.x, UV.y));
	COLOR *= color_multiplier * MODULATE;
}
