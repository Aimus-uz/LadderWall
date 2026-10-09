using System.Drawing;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

namespace LadderWall;

public class LadderWall : BasePlugin
{
    public override string ModuleName => "LadderWall";
    public override string ModuleVersion => "1.0.1";

    private const string MapName = "awp_lego";

    private const string Model = "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_01_32x32x32.vmdl";

    private readonly List<(Vector pos, QAngle ang)> _walls = new()
    {
        (new Vector(1707, 248, 62), new QAngle(0, -81, 0)),
    };

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundStart>((e, i) =>
        {
            AddTimer(1.0f, SpawnWalls);
            return HookResult.Continue;
        });

        if (hotReload) SpawnWalls();
    }

    private void SpawnWalls()
    {
        if (Server.MapName != MapName) return;
        foreach (var (pos, ang) in _walls) Spawn(pos, ang);
    }

    private static CDynamicProp? Spawn(Vector pos, QAngle ang)
    {
        var prop = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic_override");
        if (prop == null) return null;

        prop.SetModel(Model);
        prop.Teleport(pos, ang, new Vector(0, 0, 0));
        prop.DispatchSpawn();

        prop.Render = Color.FromArgb(0, 255, 255, 255);
        Utilities.SetStateChanged(prop, "CBaseModelEntity", "m_clrRender");

        prop.Collision.SolidType = SolidType_t.SOLID_VPHYSICS;
        Utilities.SetStateChanged(prop, "CCollisionProperty", "m_nSolidType");

        return prop;
    }

    [ConsoleCommand("css_wall", "Поставить пробную стену")]
    [RequiresPermissions("@css/root")]
    public void OnWall(CCSPlayerController? player, CommandInfo cmd)
    {
        var pawn = player?.PlayerPawn.Value;
        var o = pawn?.AbsOrigin;
        var a = pawn?.EyeAngles;
        if (player == null || o == null || a == null) return;

        var prop = Spawn(new Vector(o.X, o.Y, o.Z), new QAngle(0, a.Y, 0));
        if (prop != null)
        {
            prop.Render = Color.FromArgb(120, 255, 0, 0);
            Utilities.SetStateChanged(prop, "CBaseModelEntity", "m_clrRender");
        }

        player.PrintToChat($" [wall] X={o.X:F0} Y={o.Y:F0} Z={o.Z:F0} Yaw={a.Y:F0}");
    }
}
