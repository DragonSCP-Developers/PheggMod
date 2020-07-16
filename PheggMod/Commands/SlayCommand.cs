﻿using CommandSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PheggMod.Commands
{
    public class SlayCommand : ICommand
    {
        public string Command => "slay";

        public string[] Aliases { get; } = { "kill" };

        public string Description => "Kills the targeted player(s)";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool canRun = CommandManager.CanRun(sender, PlayerPermissions.PlayersManagement, arguments, new[] { "player" }, out response, out List<ReferenceHub> hubs);
            if (!canRun)
                return false;

            CommandSender cmdSender = sender as CommandSender;

            foreach(ReferenceHub refhub in hubs)
                refhub.playerStats.HurtPlayer(new PlayerStats.HitInfo(9999f, cmdSender.Nickname, DamageTypes.Nuke, int.Parse(cmdSender.SenderId)), refhub.gameObject);

            response = $"Killed {hubs.Count} {(hubs.Count > 1 ? "players" : "player")}";

            return true;
        }
    }
}