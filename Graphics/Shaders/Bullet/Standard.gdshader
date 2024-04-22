shader_type canvas_item;
uniform lowp vec4 tint;

void vertex() {
	// Flip the texture vertically:
	// QuadMesh is upside-down!
	UV.y = 1.0 - UV.y;
}

void fragment() {
	COLOR.rgba *= tint.rgba;
}