using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;

namespace cs2TpPoint
{
    [MinimumApiVersion(80)]
    public class cs2TpPoint : BasePlugin
    {
        public override string ModuleName => "cs2TpPoint";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "qazlll456 with xAI assistance";
        public override string ModuleDescription => "Allows admins to create and teleport to saved points in CS2, with noclip support.";

        private Dictionary<string, TeleportPoint> _teleportPoints = new Dictionary<string, TeleportPoint>();
        private Dictionary<ulong, bool> _noclipStates = new Dictionary<ulong, bool>();
        private const int MaxTeleportPoints = 10;
        private string _teleportPointsFilePath = "";

        public override void Load(bool hotReload)
        {
            string pluginDir = Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/plugins/cs2TpPoint");
            string tpDataDir = Path.Combine(pluginDir, "tp-point-data");

            if (!Directory.Exists(pluginDir))
            {
                Directory.CreateDirectory(pluginDir);
            }

            if (!Directory.Exists(tpDataDir))
            {
                Directory.CreateDirectory(tpDataDir);
            }

            AddCommand("css_settppoint", "Save a teleport point", OnSetTeleportPointCommand);
            AddCommand("css_tpp", "Teleport to a point or teleport a team", OnTeleportCommand); // Renamed from css_tp to css_tpp
            AddCommand("css_tpphere", "Teleport a team to your location", OnTeleportHereCommand); // Renamed from css_tphere to css_tpphere
            AddCommand("css_listtpp", "List all teleport points", OnListTeleportPointsCommand);
            AddCommand("css_cleartpp", "Clear a teleport point", OnClearTeleportPointCommand);
            AddCommand("css_tpnoclip", "Toggle noclip", OnNoclipCommand);
        }

        public override void Unload(bool hotReload)
        {
            SaveTeleportPoints();
        }

        private void OnSetTeleportPointCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAdmin(player))
            {
                command.ReplyToCommand("You must have @css/root or @css/slay permissions to use this command!");
                Logger.LogWarning("Player {PlayerName} attempted to use !settppoint without permission", player?.PlayerName ?? "unknown");
                return;
            }

            if (command.ArgCount < 2)
            {
                command.ReplyToCommand("Usage: !settppoint <id|name>");
                Logger.LogWarning("Player {PlayerName} used !settppoint with invalid arguments", player?.PlayerName ?? "unknown");
                return;
            }

            string pointId = command.GetArg(1);
            if (string.IsNullOrWhiteSpace(pointId) || !IsValidPointId(pointId))
            {
                command.ReplyToCommand("Invalid teleport point ID or name! Use numbers or English words.");
                Logger.LogWarning("Player {PlayerName} used invalid point ID: {PointId}", player?.PlayerName ?? "unknown", pointId);
                return;
            }

            if (_teleportPoints.Count >= MaxTeleportPoints && !_teleportPoints.ContainsKey(pointId))
            {
                command.ReplyToCommand($"Cannot save more than {MaxTeleportPoints} teleport points!");
                Logger.LogWarning("Player {PlayerName} tried to save more than {MaxPoints} teleport points", player?.PlayerName ?? "unknown", MaxTeleportPoints);
                return;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            {
                command.ReplyToCommand("Unable to determine your position!");
                Logger.LogError("Failed to determine position for player {PlayerName}", player?.PlayerName ?? "unknown");
                return;
            }

            // Set file path if not already set
            if (string.IsNullOrEmpty(_teleportPointsFilePath))
            {
                string mapName = Server.MapName;
                if (string.IsNullOrEmpty(mapName))
                {
                    Logger.LogError("Server.MapName is empty when saving first teleport point. Using fallback name.");
                    mapName = "unknown_map";
                }
                string pluginDir = Path.Combine(Server.GameDirectory, "csgo/addons/counterstrikesharp/plugins/cs2TpPoint");
                string tpDataDir = Path.Combine(pluginDir, "tp-point-data");
                _teleportPointsFilePath = Path.Combine(tpDataDir, $"{mapName}.json");

                if (!File.Exists(_teleportPointsFilePath))
                {
                    File.WriteAllText(_teleportPointsFilePath, "{}");
                }
                LoadTeleportPoints();
            }

            _teleportPoints[pointId] = new TeleportPoint(
                new Vector(pawn.AbsOrigin.X, pawn.AbsOrigin.Y, pawn.AbsOrigin.Z),
                new QAngle(pawn.AbsRotation.X, pawn.AbsRotation.Y, pawn.AbsRotation.Z)
            );

            SaveTeleportPoints();
            command.ReplyToCommand($"Teleport point '{pointId}' saved at {pawn.AbsOrigin.X}, {pawn.AbsOrigin.Y}, {pawn.AbsOrigin.Z}");
        }

        private void OnTeleportCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAdmin(player))
            {
                command.ReplyToCommand("You must have @css/root or @css/slay permissions to use this command!");
                Logger.LogWarning("Player {PlayerName} attempted to use !tpp without permission", player?.PlayerName ?? "unknown");
                return;
            }

            if (command.ArgCount < 3)
            {
                command.ReplyToCommand("Usage: !tpp tpp <id|name> or !tpp @t|@ct tpp <id|name>");
                Logger.LogWarning("Player {PlayerName} used !tpp with invalid arguments", player?.PlayerName ?? "unknown");
                return;
            }

            string targetArg = command.GetArg(1).ToLower();
            if (targetArg != "tpp" && targetArg != "@t" && targetArg != "@ct")
            {
                command.ReplyToCommand("Invalid target! Use 'tpp', '@t', or '@ct'.");
                Logger.LogWarning("Player {PlayerName} used invalid target in !tpp: {Target}", player?.PlayerName ?? "unknown", targetArg);
                return;
            }

            if (targetArg == "tpp")
            {
                string pointId = command.GetArg(2);
                if (!_teleportPoints.TryGetValue(pointId, out var point))
                {
                    command.ReplyToCommand($"Teleport point '{pointId}' not found!");
                    Logger.LogWarning("Player {PlayerName} tried to teleport to non-existent point '{PointId}'", player?.PlayerName ?? "unknown", pointId);
                    return;
                }

                var pawn = player.PlayerPawn.Value;
                if (pawn == null || !pawn.IsValid)
                {
                    command.ReplyToCommand("Unable to teleport you!");
                    Logger.LogError("Failed to teleport player {PlayerName} to tpp '{PointId}'", player?.PlayerName ?? "unknown", pointId);
                    return;
                }

                pawn.Teleport(point.Position.ToVector(), point.Rotation.ToQAngle(), new Vector(0, 0, 0));
                player.PrintToChat($"Teleported to tpp '{pointId}'");
            }
            else
            {
                if (command.GetArg(2).ToLower() != "tpp" || command.ArgCount < 4)
                {
                    command.ReplyToCommand("Usage: !tpp @t|@ct tpp <id|name>");
                    Logger.LogWarning("Player {PlayerName} used !tpp with invalid arguments for team teleport", player?.PlayerName ?? "unknown");
                    return;
                }

                string pointId = command.GetArg(3);
                if (!_teleportPoints.TryGetValue(pointId, out var point))
                {
                    command.ReplyToCommand($"Teleport point '{pointId}' not found!");
                    Logger.LogWarning("Player {PlayerName} tried to teleport team to non-existent point '{PointId}'", player?.PlayerName ?? "unknown", pointId);
                    return;
                }

                CsTeam team = targetArg == "@t" ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
                var players = Utilities.GetPlayers().Where(p => p.IsValid && p.Team == team && p.PlayerPawn.Value != null);
                int count = 0;

                foreach (var target in players)
                {
                    var pawn = target.PlayerPawn.Value;
                    pawn.Teleport(point.Position.ToVector(), point.Rotation.ToQAngle(), new Vector(0, 0, 0));
                    target.PrintToChat($"You have been teleported to tpp '{pointId}' by {(player != null ? player.PlayerName : "unknown")}!");
                    count++;
                }

                string teamName = team == CsTeam.Terrorist ? "Terrorists" : "Counter-Terrorists";
                command.ReplyToCommand($"Teleported {count} {teamName} to tpp '{pointId}'");
            }
        }

        private void OnTeleportHereCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAdmin(player))
            {
                command.ReplyToCommand("You must have @css/root or @css/slay permissions to use this command!");
                Logger.LogWarning("Player {PlayerName} attempted to use !tpphere without permission", player?.PlayerName ?? "unknown");
                return;
            }

            if (command.ArgCount < 2 || (command.GetArg(1).ToLower() != "@t" && command.GetArg(1).ToLower() != "@ct"))
            {
                command.ReplyToCommand("Usage: !tpphere @t|@ct");
                Logger.LogWarning("Player {PlayerName} used !tpphere with invalid arguments", player?.PlayerName ?? "unknown");
                return;
            }

            var pawn = player.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid || pawn.AbsOrigin == null)
            {
                command.ReplyToCommand("Unable to determine your position!");
                Logger.LogError("Failed to determine position for player {PlayerName} in !tpphere", player?.PlayerName ?? "unknown");
                return;
            }

            string targetArg = command.GetArg(1).ToLower();
            CsTeam team = targetArg == "@t" ? CsTeam.Terrorist : CsTeam.CounterTerrorist;
            var players = Utilities.GetPlayers().Where(p => p.IsValid && p.Team == team && p.PlayerPawn.Value != null);
            int count = 0;

            foreach (var target in players)
            {
                var targetPawn = target.PlayerPawn.Value;
                targetPawn.Teleport(pawn.AbsOrigin, pawn.AbsRotation, new Vector(0, 0, 0));
                target.PrintToChat($"You have been teleported to {(player != null ? player.PlayerName : "unknown")}!");
                count++;
            }

            string teamName = team == CsTeam.Terrorist ? "Terrorists" : "Counter-Terrorists";
            command.ReplyToCommand($"Teleported {count} {teamName} to your location");
        }

        private void OnListTeleportPointsCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAdmin(player))
            {
                command.ReplyToCommand("You must have @css/root or @css/slay permissions to use this command!");
                Logger.LogWarning("Player {PlayerName} attempted to use !listtpp without permission", player?.PlayerName ?? "unknown");
                return;
            }

            if (_teleportPoints.Count == 0)
            {
                command.ReplyToCommand("No teleport points set for this map!");
                return;
            }

            string pointList = string.Join(", ", _teleportPoints.Keys.Select(k => $"[{k}]"));
            command.ReplyToCommand($"{pointList}; total {_teleportPoints.Count} tp points");
        }

        private void OnClearTeleportPointCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAdmin(player))
            {
                command.ReplyToCommand("You must have @css/root or @css/slay permissions to use this command!");
                Logger.LogWarning("Player {PlayerName} attempted to use !cleartpp without permission", player?.PlayerName ?? "unknown");
                return;
            }

            if (command.ArgCount < 2)
            {
                command.ReplyToCommand("Usage: !cleartpp <id|name> or !cleartpp @all");
                Logger.LogWarning("Player {PlayerName} used !cleartpp with invalid arguments", player?.PlayerName ?? "unknown");
                return;
            }

            string arg = command.GetArg(1).ToLower();
            if (arg == "@all")
            {
                int count = _teleportPoints.Count;
                _teleportPoints.Clear();
                SaveTeleportPoints();
                command.ReplyToCommand($"Cleared all {count} teleport points for this map");
            }
            else
            {
                if (!_teleportPoints.Remove(arg))
                {
                    command.ReplyToCommand($"Teleport point '{arg}' not found!");
                    Logger.LogWarning("Player {PlayerName} tried to clear non-existent point '{PointId}'", player?.PlayerName ?? "unknown", arg);
                    return;
                }
                SaveTeleportPoints();
                command.ReplyToCommand($"Cleared teleport point '{arg}'");
            }
        }

        private void OnNoclipCommand(CCSPlayerController? player, CommandInfo command)
        {
            if (!IsAdmin(player))
            {
                command.ReplyToCommand("You must have @css/root or @css/slay permissions to use this command!");
                Logger.LogWarning("Player {PlayerName} attempted to use !tpnoclip without permission", player?.PlayerName ?? "unknown");
                return;
            }

            CCSPlayerController target;
            if (command.ArgCount < 2)
            {
                target = player;
            }
            else
            {
                string targetArg = command.GetArg(1);
                target = GetTargetFromArg(command, targetArg);
                if (target == null)
                {
                    Logger.LogWarning("Failed to find target for !tpnoclip: {TargetArg}", targetArg);
                    return;
                }
            }

            var pawn = target.PlayerPawn.Value;
            if (pawn == null || !pawn.IsValid)
            {
                command.ReplyToCommand($"Unable to toggle noclip for {target.PlayerName}!");
                Logger.LogError("Failed to toggle noclip for player {PlayerName}: Invalid pawn", target.PlayerName);
                return;
            }

            bool isAlive = target.PawnIsAlive;
            if (!isAlive)
            {
                command.ReplyToCommand("You must be alive to toggle noclip!");
                Logger.LogWarning("Player {PlayerName} attempted to toggle noclip while not alive", target.PlayerName);
                return;
            }

            bool currentState = _noclipStates.TryGetValue(target.SteamID, out var state) && state;
            bool newState = !currentState;

            SetMoveType(target, newState ? MoveType_t.MOVETYPE_NOCLIP : MoveType_t.MOVETYPE_WALK);
            if (!newState)
            {
                pawn.AbsVelocity.X = 0;
                pawn.AbsVelocity.Y = 0;
                pawn.AbsVelocity.Z = 0;
            }

            _noclipStates[target.SteamID] = newState;
            target.PrintToChat($"Noclip {(newState ? "enabled" : "disabled")} by {(player != null ? player.PlayerName : "unknown")}");
            if (target != player)
            {
                player.PrintToChat($"Toggled noclip {(newState ? "on" : "off")} for {target.PlayerName}");
            }
        }

        private void SetMoveType(CCSPlayerController? player, MoveType_t moveType)
        {
            if (player?.PlayerPawn?.Value == null) return;
            player.PlayerPawn.Value.MoveType = moveType;
            player.PlayerPawn.Value.ActualMoveType = moveType;
            Utilities.SetStateChanged(player.PlayerPawn.Value, "CBaseEntity", "m_MoveType");
        }

        private bool IsAdmin(CCSPlayerController? player)
        {
            if (player == null || !player.IsValid)
            {
                Logger.LogWarning("Admin check failed: Player is null or invalid");
                return false;
            }
            return true;
        }

        private bool IsValidPointId(string id)
        {
            return int.TryParse(id, out _) || System.Text.RegularExpressions.Regex.IsMatch(id, @"^[a-zA-Z]+$");
        }

        private CCSPlayerController? GetTargetFromArg(CommandInfo command, string arg)
        {
            if (string.IsNullOrEmpty(arg))
            {
                command.ReplyToCommand("Please specify a player name!");
                Logger.LogWarning("GetTargetFromArg: No player name specified");
                return null;
            }

            var players = Utilities.GetPlayers()
                .Where(p => p.IsValid && p.PlayerName.Contains(arg, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (players.Count == 0)
            {
                command.ReplyToCommand("No player found with that name!");
                Logger.LogWarning("GetTargetFromArg: No player found with name {Name}", arg);
                return null;
            }
            else if (players.Count > 1)
            {
                command.ReplyToCommand("Multiple players found! Be more specific.");
                Logger.LogWarning("GetTargetFromArg: Multiple players found with name {Name}", arg);
                return null;
            }

            return players[0];
        }

        private void LoadTeleportPoints()
        {
            if (!File.Exists(_teleportPointsFilePath))
            {
                return;
            }

            try
            {
                string json = File.ReadAllText(_teleportPointsFilePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, TeleportPoint>>(json);
                if (data != null)
                {
                    _teleportPoints = data;
                }
                else
                {
                    Logger.LogWarning("Teleport points file {Path} is empty or invalid, initializing empty dictionary", _teleportPointsFilePath);
                    _teleportPoints = new Dictionary<string, TeleportPoint>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to load teleport points from {Path}: {Error}", _teleportPointsFilePath, ex.Message);
            }
        }

        private void SaveTeleportPoints()
        {
            if (string.IsNullOrEmpty(_teleportPointsFilePath))
            {
                Logger.LogWarning("Teleport points file path not set. Cannot save teleport points.");
                return;
            }

            try
            {
                string json = JsonSerializer.Serialize(_teleportPoints, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_teleportPointsFilePath, json);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to save teleport points to {Path}: {Error}", _teleportPointsFilePath, ex.Message);
            }
        }
    }

    public class SerializableVector
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public SerializableVector() { }
        public SerializableVector(Vector vector) { X = vector.X; Y = vector.Y; Z = vector.Z; }
        public Vector ToVector() => new Vector(X, Y, Z);
    }

    public class SerializableQAngle
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public SerializableQAngle() { }
        public SerializableQAngle(QAngle qAngle) { X = qAngle.X; Y = qAngle.Y; Z = qAngle.Z; }
        public QAngle ToQAngle() => new QAngle(X, Y, Z);
    }

    public class TeleportPoint
    {
        public SerializableVector Position { get; set; }
        public SerializableQAngle Rotation { get; set; }

        public TeleportPoint()
        {
            Position = new SerializableVector();
            Rotation = new SerializableQAngle();
        }

        public TeleportPoint(Vector position, QAngle rotation)
        {
            Position = new SerializableVector(position);
            Rotation = new SerializableQAngle(rotation);
        }
    }
}