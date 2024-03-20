shader_type canvas_item;
uniform lowp vec4 tint;
uniform highp float frame_count = 4;
uniform highp float frame_duration = 0.25;
uniform bool sync = false;

void vertex() {
	// Flip the texture vertically:
	// QuadMesh is upside-down!
	UV.y = 1.0 - UV.y;
	// We expect a horizontal strip of frames of the same size.
	// This modifies the UV.x as to display only one such frame at a time.
	float timeOffset = float(!sync) * fract(sin(float(INSTANCE_ID) * 78.233) * 15929.0);
	float currFrame = mod(
		floor((TIME + timeOffset) / frame_duration), 
		frame_count
	);
	UV.x = (UV.x + currFrame) * (1.0 / frame_count);
}

void fragment() {
  vec4 texCol = texture(TEXTURE, UV);
  float texDark = 1.0 - texCol.b;
  float sat = max(max(abs(COLOR.r - COLOR.g), abs(COLOR.g - COLOR.b)), abs(COLOR.r - COLOR.b));
  float isGrayscale = 1.0 - step(0.03, sat);
  vec4 newColor = COLOR;
  newColor.rgb = mix(newColor.rgb, tint.rgb, texDark);
  newColor.a *= mix(1.0, tint.a, texDark);
  COLOR = mix(COLOR, newColor, isGrayscale);
}

