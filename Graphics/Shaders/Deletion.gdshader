shader_type canvas_item;
render_mode blend_add;
global uniform float STAGE_TIME;
uniform highp float frame_count = 16;
uniform highp float frame_duration = 0.0333;

void vertex() {
	// Expectations:
	// INSTANCE_CUSTOM.x is a scale multiplier.
	// INSTANCE_CUSTOM.y is a start time for the animation.
	
	// Expand the effect by scale multiplier
	VERTEX *= INSTANCE_CUSTOM.x;
	
	// Select a frame
	float frame = floor((STAGE_TIME - INSTANCE_CUSTOM.y) / frame_duration);
	frame = clamp(frame, 0, frame_count - 1.0);
	UV.x = (UV.x + frame) * (1.0 / frame_count);
	
	// Flip the texture vertically:
	// QuadMesh is upside-down!
	UV.y = 1.0 - UV.y;
}

void fragment() {
}

