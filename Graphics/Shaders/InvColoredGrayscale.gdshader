shader_type canvas_item;
uniform lowp vec4 tint;

void vertex() {
	// Flip the texture vertically:
	// QuadMesh is upside-down!
	UV.y = 1.0 - UV.y;
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

