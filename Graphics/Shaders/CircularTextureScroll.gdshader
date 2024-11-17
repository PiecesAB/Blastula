shader_type canvas_item;
uniform highp float radius = 20.0;
uniform highp float period = 100.0;
uniform highp vec4 color_mul = vec4(1.0, 1.0, 1.0, 1.0);
uniform sampler2D pattern: repeat_enable;

void vertex() {
	
}

void fragment() {
	float a = 2.0 * PI * TIME / period;
	vec2 p = radius * vec2(cos(a), sin(a));
	COLOR = color_mul * texture(pattern, UV + p);
}
