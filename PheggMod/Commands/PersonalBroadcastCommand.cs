﻿using CommandSystem;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PheggMod.Commands
{
    public class PersonalBroadcastCommand : ICommand
    {
        public string Command => "pbc";

        public string[] Aliases { get; } = { "personalbroadcast", "privatebroadcast", "pbcmono", "personalbroadcastmono", "privatebroadcastmono" };

        public string Description => "Sends a private broadcast message to the specified player(s)";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            bool canRun = CommandManager.CanRun(sender, PlayerPermissions.Broadcasting, arguments, new[] { "player", "duration", "message" }, out response, out List<ReferenceHub> hubs);
            if (!canRun)
                return false;

            if (!ushort.TryParse(arguments.Array[2], out ushort duration) || duration < 1 || duration > 254)
            {
                response = "Invalid duration given";
                return false;
            }

			string message = $"<color=#FFA500><b>[Private]</b></color> <color=green>{string.Join(" ", arguments.Skip(2))}</color>";

			GameObject senderObject = PlayerManager.players.Where(p => p.GetComponent<NicknameSync>().MyNick == ((CommandSender)sender).Nickname).FirstOrDefault();
			if (senderObject != null)
				hubs.Add(senderObject.GetComponent<ReferenceHub>());

			foreach (ReferenceHub refhub in hubs)
            {
                GameObject go = refhub.gameObject;
                go.GetComponent<Broadcast>().TargetAddElement(go.GetComponent<NetworkConnection>(), message, duration, Broadcast.BroadcastFlags.Normal);
            }

            response = "Broadcast sent";
            return canRun;
        }
    }
}
