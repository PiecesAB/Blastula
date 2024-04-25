#if TOOLS
using Blastula;
using Blastula.Graphics;
using Blastula.Schedules;
using Blastula.VirtualVariables;
using Godot;

[Tool]
public partial class BlastulaPlugin : EditorPlugin
{
    public static BlastulaPlugin main;
    public static GodotObject selection;

    public override string _GetPluginName()
    {
        return "Blastula";
    }

    public override bool _Handles(GodotObject obj)
    {
        return obj is Blastula.Wind.StandardWindSource;
    }

    public override void _Edit(GodotObject obj)
    {
        base._Edit(obj);
        selection = obj;
    }

    public override void _EnterTree()
    {
        main = this;
        // Set game environment conditions
        ProjectSettings.SetSetting("application/run/main_scene", "res://addons/Blastula/Loader.tscn");
        ProjectSettings.SetSetting("application/run/max_fps", Persistent.SIMULATED_FPS);
        // Set to recommended resolution and auto-scale
        ProjectSettings.SetSetting("display/window/size/viewport_width", 1280);
        ProjectSettings.SetSetting("display/window/size/viewport_height", 960);
        ProjectSettings.SetSetting("display/window/stretch/mode", "canvas_items");
        ProjectSettings.SetSetting("display/window/stretch/aspect", "keep_height");
        // Editor-only shader globals existence, so shader compilation doesn't complain.
        // In game, it is created using BulletRendererManager.
        RenderingServer.GlobalShaderParameterAdd(BulletRendererManager.STAGE_TIME_NAME, RenderingServer.GlobalShaderParameterType.Float, 0);
    }

    public override void _ExitTree()
    {
        RenderingServer.GlobalShaderParameterRemove(BulletRendererManager.STAGE_TIME_NAME);
    }
}
#endif
