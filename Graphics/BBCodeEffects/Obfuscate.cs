using Godot;
using System;

namespace Blastula.Menus;

[Tool]
[GlobalClass]
public partial class Obfuscate : RichTextEffect
{
    [Export] public string bbcode = "obfuscate";

    private const string CHARS = "0123456789BCDHJLPQTVWXZ#%*+?><~@&^%$!GK";

    public override bool _ProcessCustomFX(CharFXTransform charFX)
    {
        TextServer ts = TextServerManager.GetPrimaryInterface();
        int a = charFX.RelativeIndex;
        a += 89347 + (int)(GetInstanceId() % 11191);
        for (int i = 0; i < 2 + (Mathf.RoundToInt(2 * Time.GetTicksMsec() / 1000.0) % 8); ++i)
        {
            a ^= a << 13; a ^= a >> 17; a ^= a << 5;
        }
        charFX.GlyphIndex = (uint)ts.FontGetGlyphIndex(charFX.Font, 1, CHARS[Mathf.Abs(a) % 37], 0);
        return false;
    }
}
