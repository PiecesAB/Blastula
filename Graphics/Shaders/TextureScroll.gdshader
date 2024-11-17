shader_type canvas_item;
uniform highp vec2 speed = vec2(0.2, 0.2);
uniform highp float period = 100.0;
uniform sampler2D pattern: repeat_enable;

void vertex() {
	
}

void fragment() {
	COLOR = texture(pattern, UV + mod(TIME, period) * speed);
}
