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
    public override string ModuleVersion => "1.1.1";

    // false = стены из списка полностью невидимые, true = красные (для проверки)
    private const bool Debug = false;

    private const string Model = "models/props/de_dust/hr_dust/dust_crates/dust_crate_style_01_32x32x32.vmdl";

    // Стены по названию карты. Название смотреть через !wallinfo
    private readonly Dictionary<string, List<(Vector pos, QAngle ang)>> _maps =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["awp_lego_2"] = new()
        {
            (new Vector(1704, 244, 92), new QAngle(0, -85, 0)),
        },

        ["awp_lego_2_winter"] = new()
        {
            (new Vector(-88, 5, 311), new QAngle(0, -81, 0)),
        },
    };

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnServerPrecacheResources>(manifest =>
        {
            manifest.AddResource(Model);
        });

        RegisterEventHandler<EventRoundStart>((e, i) =>
        {
            AddTimer(1.0f, SpawnWalls);
            return HookResult.Continue;
        });
    }

    private void SpawnWalls()
    {
        var map = Server.MapName ?? "";

        if (!_maps.TryGetValue(map, out var walls))
        {
            Console.WriteLine($"[LadderWall] no walls for map '{map}', skip");
            return;
        }

        Console.WriteLine($"[LadderWall] map='{map}', walls={walls.Count}");

        foreach (var (pos, ang) in walls)
        {
            var p = Spawn(pos, ang);
            if (p == null)
            {
                Console.WriteLine("[LadderWall] spawn FAILED");
                continue;
            }

            if (Debug)
            {
                p.Render = Color.FromArgb(120, 255, 0, 0);
                Utilities.SetStateChanged(p, "CBaseModelEntity", "m_clrRender");
            }
        }
    }

    private static CBaseModelEntity? Spawn(Vector pos, QAngle ang)
    {
        var prop = Utilities.CreateEntityByName<CPhysicsPropOverride>("prop_physics_override");
        if (prop == null) return null;

        prop.SetModel(Model);
        prop.Teleport(pos, ang, new Vector(0, 0, 0));
        prop.DispatchSpawn();

        // заморозить: твёрдый, но не двигается
        prop.AcceptInput("DisableMotion");

        // полностью невидимый
        prop.Render = Color.FromArgb(0, 255, 255, 255);
        Utilities.SetStateChanged(prop, "CBaseModelEntity", "m_clrRender");

        return prop;
    }

    // Пробная стена (красная, для замера координат)
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

    [ConsoleCommand("css_wallinfo", "Инфо о карте и стенах")]
    [RequiresPermissions("@css/root")]
    public void OnInfo(CCSPlayerController? player, CommandInfo cmd)
    {
        var map = Server.MapName ?? "";
        var count = _maps.TryGetValue(map, out var w) ? w.Count : 0;
        player?.PrintToChat($" [wall] map='{map}' walls={count}");
    }

    [ConsoleCommand("css_wallspawn", "Заспавнить стены сейчас")]
    [RequiresPermissions("@css/root")]
    public void OnSpawnNow(CCSPlayerController? player, CommandInfo cmd)
    {
        SpawnWalls();
        player?.PrintToChat(" [wall] spawn called");
    }
}
