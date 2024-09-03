shader_type canvas_item;
uniform float power = 0.35;

void fragment() {
	COLOR.rgb *= (1.0 - power * UV.y);
}
