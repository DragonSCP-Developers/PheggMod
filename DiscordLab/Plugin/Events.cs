﻿using PheggMod.API;
using PheggMod.API.Events;
using PheggMod.API.Plugin;
using System;

namespace DiscordLab
{
    internal class Events : IEventHandlerWaitingForPlayers, IEventHandlerPlayerJoin, IEventHandlerPlayerLeave, IEventHandlerAdminQuery, IEventHandlerGlobalBan, IEventHandlerLczDecontaminate, IEventHandlerPlayerBan,
        IEventHandlerPlayerDeath, IEventHandlerPlayerEscape, IEventHandlerPlayerHurt, IEventHandlerPlayerKick, IEventHandlerPlayerSpawn, IEventHandlerPlayerThrowGrenade, IEventHandlerRespawn, IEventHandlerRoundEnd,
        IEventHandlerRoundStart, IEventHandlerWarheadCancel, IEventHandlerWarheadDetonate, IEventHandlerWarheadStart
    {
        internal static DateTime RoundEnded;
        private DateTime? roundStart = null;

        public void OnAdminQuery(AdminQueryEvent ev) => DiscordLab.bot.NewMessage($"```yaml\nAdmin: {ev.Admin.name}\nExecuted: {ev.Query.ToUpper()}```");
        public void OnGlobalBan(GlobalBanEvent ev) => DiscordLab.bot.NewMessage($"{ev.Player} was globally banned for cheating");
        public void OnLczDecontaminate(LczDecontaminateEvent ev) => DiscordLab.bot.NewMessage($"Light containment zone decontamination has begun!");
        public void OnPlayerBan(PlayerBanEvent ev) =>
            DiscordLab.bot.NewMessage($"**New Ban!**```autohotkey\nUser: {ev.Player}\nAdmin: {ev.Admin}\nDuration: {ev.Duration} {(ev.Duration > 1 ? "minutes" : "minute")}\nReason: {ev.Reason}```");
        public void OnPlayerEscape(PlayerEscapeEvent ev) => DiscordLab.bot.NewMessage($"{ev.Player.name} escaped the facility and became {ev.newRole}");
        public void OnPlayerJoin(PlayerJoinEvent ev) => DiscordLab.bot.NewMessage($"**{ev.Player.name} ({ev.Player.userId} from ||~~{ev.Player.ipAddress}~~||) has joined the server**");
        public void OnPlayerKick(PlayerKickEvent ev) => DiscordLab.bot.NewMessage($"**Player kicked!**```autohotkey\nUser: {ev.Player}\nAdmin: {(ev.Admin.ToString() == " ()" ? "Server console" : ev.Admin.ToString())}```");
        public void OnPlayerLeave(PlayerLeaveEvent ev) => DiscordLab.bot.NewMessage($"{ev.Player} disconnected from the server.");
        private string _lastSpawnMessage = "a";
        public void OnPlayerSpawn(PlayerSpawnEvent ev)
        {
            string newMsg = $"{ev.Player.name} spawned as {ev.Role}";
            if (_lastSpawnMessage == newMsg) return;

            DiscordLab.bot.NewMessage(newMsg);
            _lastSpawnMessage = newMsg;
        }
        public void OnRespawn(RespawnEvent ev) => DiscordLab.bot.NewMessage(ev.IsCI ? "**Attention all personnel: Chaos insurgency breach in progress**" : "**Mobile Task Force unit Epsilon 11 has entered the facility!**");
        public void OnRoundEnd(RoundEndEvent ev)
        {
            DiscordLab.bot.NewMessage($"**Round Ended**\n```Round Time: {new DateTime(TimeSpan.FromSeconds((DateTime.Now - (DateTime)roundStart).TotalSeconds).Ticks):HH:mm:ss}"
                + $"\nEscaped Class-D: {ev.Class_D.Escaped_ClassD}/{ev.Class_D.Starting_ClassD}"
                + $"\nRescued Scientists: {ev.Scientist.Escaped_Scientists}/{ev.Scientist.Starting_Scientists}"
                + $"\nTerminated SCPs: {ev.SCP.Terminated_SCPs}/{ev.SCP.Starting_SCPs}"
                + $"\nWarhead Status: {(AlphaWarheadController.Host.detonated == false ? "Not Detonated" : $"Detonated")}```");

            roundStart = null;
            Events.RoundEnded = DateTime.Now;
        }
        public void OnRoundStart(RoundStartEvent ev)
        {
            roundStart = DateTime.Now;
            DiscordLab.bot.NewMessage($"**A new round has begun**");
        }
        public void OnThrowGrenade(PlayerThrowGrenadeEvent ev) => DiscordLab.bot.NewMessage($"{ev.Player.name} threw {ev.Grenade}");
        public void OnWaitingForPlayers(WaitingForPlayersEvent ev) => DiscordLab.bot.NewMessage("Waiting for players...");
        public void OnWarheadCancel(WarheadCancelEvent ev) => DiscordLab.bot.NewMessage($"{ev.Disabler.name} canceled the warhead detonation");
        public void OnWarheadDetonate(WarheadDetonateEvent ev) => DiscordLab.bot.NewMessage("The alpha warhead has been detonated");
        public void OnWarheadStart(WarheadStartEvent ev) => DiscordLab.bot.NewMessage(
            ev.InitialStart ? $"**Alpha warhead detonation sequence engaged! The underground section of the facility will be detonated in T-minus {ev.TimeToDetonation} seconds!**" :
            $"**Alpha warhead detonation sequence resumed! {ev.TimeToDetonation} seconds to detonation!**");


        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            if (!RoundSummary.RoundInProgress() || ev.PlayerHurtCache.PlayerOriginalRole == RoleType.Spectator) return;

            PlayerHurtCache pHC = ev.PlayerHurtCache;

            if (pHC.PMDamageType == PMDamageType.WorldKill)
                DiscordLab.bot.NewMessage($"{ev.DamageType.name} -> {ev.Player.name} -> {Math.Round(ev.Damage)}");
            else if (ev.Attacker.userId == ev.Player.userId)
                DiscordLab.bot.NewMessage($"{ev.Player.name} self-harmed for {Math.Round(ev.Damage)} with {ev.DamageType.name}");
            else if (pHC.PMDamageType == PMDamageType.TeamKill)
                DiscordLab.bot.NewMessage($"**{pHC.AttackerOriginalRole} {ev.Attacker} attacked {pHC.PlayerOriginalRole} {ev.Player} for {Math.Round(ev.Damage)} with {ev.DamageType.name}**");
            else if (pHC.PMDamageType == PMDamageType.DisarmedKill)
                DiscordLab.bot.NewMessage($"__{pHC.AttackerOriginalRole} {ev.Attacker} attacked {pHC.PlayerOriginalRole} {ev.Player} for {Math.Round(ev.Damage)} with {ev.DamageType.name}__");
            else
                DiscordLab.bot.NewMessage($"{ev.Attacker.name} -> {ev.Player.name} -> {Math.Round(ev.Damage)} ({ev.DamageType.name})");
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            if (!RoundSummary.RoundInProgress() || ev.PlayerHurtCache.PlayerOriginalRole == RoleType.Spectator) return;

            PlayerHurtCache pHC = ev.PlayerHurtCache;

            if (pHC.PMDamageType == PMDamageType.WorldKill)
                DiscordLab.bot.NewMessage($"WORLD killed {ev.Player.name} using {ev.DamageType.name}");
            else if (ev.Attacker.userId == ev.Player.userId)
                DiscordLab.bot.NewMessage($"{ev.Player.name} committed suicide with {ev.DamageType.name}");
            else if (pHC.PMDamageType == PMDamageType.TeamKill)
                DiscordLab.bot.NewMessage($"**Teamkill** \n```autohotkey\nPlayer: {pHC.AttackerOriginalRole} {ev.Attacker}"
                                    + $"\nKilled: {pHC.PlayerOriginalRole} {ev.Player}\nUsing: {ev.DamageType.name}```");
            else if (pHC.PMDamageType == PMDamageType.DisarmedKill)
                DiscordLab.bot.NewMessage($"__Disarmed Kill__\n```autohotkey\nPlayer: {pHC.AttackerOriginalRole} {ev.Attacker}"
                                    + $"\nKilled: {pHC.PlayerOriginalRole} {ev.Player}\nUsing: {ev.DamageType.name}```");
            else if(pHC.PMDamageType == PMDamageType.AntiCheat)
                DiscordLab.bot.NewMessage($"ANTICHEAT killed {ev.Attacker} with code {pHC.HitInfo.Attacker}");

            else
                DiscordLab.bot.NewMessage($"{pHC.AttackerOriginalRole} {ev.Attacker.name} killed {pHC.PlayerOriginalRole} {ev.Player.name} with {ev.DamageType.name}");
        }
    }
}