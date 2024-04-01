shader_type canvas_item;
global uniform float STAGE_TIME;
uniform highp float start_time = 0.0;
varying float curr_time;

void vertex() {
	float digit_start_time = start_time + 0.04 * float(INSTANCE_ID);
	curr_time = STAGE_TIME - digit_start_time + 0.25;
	// The line below can be used for testing the effect
	// curr_time = fract((TIME - digit_start_time) / 2.0) * 2.0;
	VERTEX.y += clamp(-3.0 * pow(4.0 * curr_time - 2.0, 5.0), -1000.0, 1000.0);
}

void fragment() {
	COLOR.a *= max(0.0, 1.0 - 3.0 * pow(abs(1.0 - 2.0 * curr_time), 3.0));
}
