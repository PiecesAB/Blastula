shader_type canvas_item;
global uniform float STAGE_TIME;
uniform highp float character_delay = 0.04;
uniform highp float start_time = 0.0;
uniform highp float time_extension = 0.0;
uniform highp vec2 undulation = vec2(0.0, 1.0);
varying float curr_time;

void vertex() {
	float digit_start_time = start_time + character_delay * float(INSTANCE_ID);
	curr_time = STAGE_TIME - digit_start_time + 0.25;
	// The line below can be used for testing the effect
	//curr_time = fract((TIME - digit_start_time) / 4.0) * 4.0;
	curr_time = curr_time - max(0, min(curr_time - 0.5, time_extension));
	VERTEX.x += undulation.x * cos((digit_start_time + TIME) * undulation.y);
	VERTEX.y += undulation.x * sin((digit_start_time + TIME) * undulation.y);
	VERTEX.y += clamp(-3.0 * pow(4.0 * curr_time - 2.0, 5.0), -1000.0, 1000.0);
}

void fragment() {
	COLOR.a *= max(0.0, 1.0 - 3.0 * pow(abs(1.0 - 2.0 * curr_time), 3.0));
}
