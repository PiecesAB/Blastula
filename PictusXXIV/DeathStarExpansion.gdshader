shader_type canvas_item;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform float spike_factor = 1.0;
uniform float frequency = 8.0;
uniform float intensity = 1.0;

void fragment() {
	vec3 c = textureLod(screen_texture, SCREEN_UV, 0.0).rgb;
	vec2 from_center = UV - vec2(0.5, 0.5);
	float a = atan(from_center.y, from_center.x);
	float tri = (asin(sin(frequency * a + 0.5 * PI)) + 0.5 * PI) / PI;
	float dist = distance(UV, vec2(0.5, 0.5));
	float max_dist = mix(0.5 / (1.0 + spike_factor), 0.5, tri);
	float intensity_2 = intensity * mix(1.0, 2.0, dist / max_dist);
	COLOR.rgb = mix(c.rgb, vec3(1.0) - c.rgb, intensity_2 * step(dist, max_dist));
}
