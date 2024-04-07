shader_type canvas_item;
// The top-left of the inner region in world space.
// Anything outside is the rim of the bar.
uniform highp vec2 world_top_left = vec2(-9571.0, -194.0);
// The size of the inner region in world space.
// Anything outside is the rim of the bar.
uniform highp vec2 world_size = vec2(174.0, 39.0);
uniform lowp float filled_fraction = 0.5;
uniform sampler2D noise_sampler: repeat_enable;

varying highp vec4 pos_progress;

void vertex() {
	pos_progress = MODEL_MATRIX * vec4(VERTEX, 0.0, 1.0);
	pos_progress = vec4((pos_progress.xy - world_top_left) / world_size, 0.0, 1.0);
}

void fragment() {
	float is_inside = step(0.5, 1.0 - step(0.5, abs(0.5 - pos_progress.x)) - step(0.5, abs(0.5 - pos_progress.y)));
	float distortion = texture(noise_sampler, vec2(pos_progress.x - 0.25 * TIME, 0.25 * pos_progress.y)).r;
	float is_filled = is_inside * step(0.0, filled_fraction - pos_progress.x + 0.1 - 0.2 * distortion);
	COLOR.rgb = mix(vec3(1.0), vec3(0.0), is_filled);
}