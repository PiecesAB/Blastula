shader_type canvas_item;
global uniform float STAGE_TIME;
uniform highp float screen_top = -448;
uniform highp float graphic_half_height = 16;
// We expect a graphic where the upper half is an upward arrow for when the collectible is off the top of the screen.
// The bottom half is the normal graphic for the collectible.

void vertex() {
	// Flip the texture vertically:
	// QuadMesh is upside-down!
	UV.y = 1.0 - UV.y;
	// Stretch the UV to make only the normal graphic visible.
	UV.y = 0.5 * UV.y + 0.5;
	// If the graphic is sufficiently higher than screen_top, clamp to screen_top and appear as an arrow.
	// This assumes the graphic is being used as part of a multimesh and will therefore always be drawn.
	// Normally, when the sprite goes off-screen, it is "correctly" culled despite our vertex movement.
	vec4 world_pos = MODEL_MATRIX * vec4(0.0, 0.0, 0.0, 1.0);
	float displacement = max(0.0, (screen_top - graphic_half_height) - world_pos.y);
	UV.y -= 0.5 * step(0.01, displacement);
	displacement += graphic_half_height * step(0.01, displacement);
	VERTEX.y += (inverse(MODEL_MATRIX) * vec4(0.0, displacement, 0.0, 0.0)).y;
}

void fragment() {
	COLOR.a *= 0.9 + 0.1 * sin(STAGE_TIME * 10.0);
}