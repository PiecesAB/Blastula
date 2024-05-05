shader_type canvas_item;
uniform highp float spacing = 16.0;
uniform highp vec2 start_offset = vec2(228.0, 80.0);
uniform highp vec2 center = vec2(228.0, 220.0);
uniform mediump float use_rotation = 0.0;

void vertex() {
	float x = spacing * (float(INSTANCE_ID) + 0.5);
	float radius = length(center - start_offset);
	float t = (-x / radius) - (PI * 0.5);
	vec2 dir = vec2(cos(t), sin(t));
	vec2 rot = vec2(cos(t + PI * 0.5), sin(t + PI * 0.5));
	VERTEX.x -= x;
	vec2 vxRot = vec2(rot.x * VERTEX.x - rot.y * VERTEX.y, rot.y * VERTEX.x + rot.x * VERTEX.y);
	VERTEX.xy = mix(VERTEX.xy, vxRot, use_rotation);
	VERTEX.xy += center + dir * radius;
}

void fragment() {
	// Called for every pixel the material is visible on.
}

