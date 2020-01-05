﻿#pragma warning disable CS0626 // orig_ method is marked external and has no attributes on it.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod;
using System.IO;
using System.Reflection;
using UnityEngine;
using Mirror;
using System.Net;
using GameCore;

namespace PheggMod
{
    [MonoModPatch("global::MTFRespawn")]
    class PMMTFRespawn : MTFRespawn
    {
        public extern void orig_RespawnDeadPlayers();
        public new void RespawnDeadPlayers()
        {
            orig_RespawnDeadPlayers();

            if (this.nextWaveIsCI)
            {
                BotWorker.NewMessage($"**Warning: Chaos insurgency detected!**");
                PlayerManager.localPlayer.GetComponent<MTFRespawn>().RpcPlayCustomAnnouncement("ATTENTION ALL PERSONNEL . CHAOS INSURGENCY BREACH IN PROGRESS", false, true);
            }

            else
                BotWorker.NewMessage($"**Mobile Task Force unit Epsilon 11 has entered the facility!**");
        }
    }
}
