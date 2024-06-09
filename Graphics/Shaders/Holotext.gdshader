shader_type canvas_item;
uniform sampler2D wobble_sampler: repeat_enable;
uniform float wobble_intensity = 4.0;
uniform sampler2D fade_sampler: repeat_enable;
uniform float fade_progress = 1.0;
uniform sampler2D texture_sampler: repeat_enable;

void vertex() {
	VERTEX.xy += wobble_intensity * texture(wobble_sampler, vec2(5.0 * float(INSTANCE_ID), 3.0 * TIME) / 96.0).xy;
}

void fragment() {
	float s = texture(fade_sampler, VERTEX / 360.0).x;
	if (s > fade_progress) { discard; }
	COLOR *= 0.875 + 0.5 * texture(texture_sampler, VERTEX / 90.0).x;
}
