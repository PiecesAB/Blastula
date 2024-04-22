shader_type canvas_item;
uniform lowp vec4 tint;
uniform highp float speed;

void vertex() {
	// Perform the spin
	float t = TIME * speed * PI / 180.0;
	VERTEX.xy = vec2(
		cos(t) * VERTEX.x - sin(t) * VERTEX.y, 
		sin(t) * VERTEX.x + cos(t) * VERTEX.y
	);
	// Flip the texture vertically:
	// QuadMesh is upside-down!
	UV.y = 1.0 - UV.y;
}

void fragment() {
	COLOR.rgba *= tint.rgba;
}