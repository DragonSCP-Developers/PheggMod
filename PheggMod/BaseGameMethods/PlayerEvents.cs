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
using MEC;

using PheggMod.API.Events;

namespace PheggMod
{
    [MonoModPatch("global::PlayerStats")]
    class PMPlayerStats : PlayerStats
    {
        public extern bool orig_HurtPlayer(PlayerStats.HitInfo info, GameObject go);
        public new bool HurtPlayer(PlayerStats.HitInfo info, GameObject Player)
        {
            try
            {
                if (!Player.GetComponent<CharacterClassManager>().isLocalPlayer)
                {
                    PheggPlayer pPlayer = new PheggPlayer(Player);
                    PheggPlayer pAttacker = null;

                    if (info.GetPlayerObject() != null) { pAttacker = new PheggPlayer(info.GetPlayerObject()); }

                    PlayerStats Pstats = Player.GetComponent<PlayerStats>();
                    PluginManager.TriggerEvent<IEventHandlerPlayerHurt>(new PlayerHurtEvent(pPlayer, pAttacker, info.Amount, info.GetDamageType()));
                }
            }
            catch (Exception e) { Base.Error(e.Message); }

            orig_HurtPlayer(info, Player);

            return false;
        }
    }

    [MonoModPatch("global::CharacterClassManager")]
    class PMCharacterClassManager : CharacterClassManager
    {
        internal ServerRoles SrvRoles { get; private set; }

        //WaitingForPlayers
        public extern System.Collections.IEnumerator orig_Init();
        public System.Collections.IEnumerator Init()
        {
            base.StartCoroutine(orig_Init());

            if (isLocalPlayer && NetworkServer.active && ServerStatic.IsDedicated)
            {
                try
                {
                    PluginManager.TriggerEvent<IEventHandlerWaitingForPlayers>(new WaitingForPlayersEvent());
                }
                catch (Exception e) { Base.Error(e.Message); }
            }

            yield return 1f;
        }

        //RoundStartEvent
        public extern static bool orig_ForceRoundStart();
        public new static bool ForceRoundStart()
        {
            bool Bool = orig_ForceRoundStart();

            try
            {
                PluginManager.TriggerEvent<IEventHandlerRoundStart>(new RoundStartEvent());
            }
            catch (Exception e) { Base.Error(e.Message); }

            return Bool;
        }

        //PlayerSpawn / PlayerEscape
        public extern void orig_ApplyProperties(bool lite = false, bool escape = false);
        public new void ApplyProperties(bool lite = false, bool escape = false)
        {
            RoleType originalrole = this.CurClass;

            orig_ApplyProperties(lite, escape);

            if (isLocalPlayer || (int)this.CurClass == 2) return;

            try
            {
                if (!escape) PluginManager.TriggerEvent<IEventHandlerPlayerSpawn>(new PlayerSpawnEvent(new PheggPlayer(this.gameObject), this.CurClass, this.Classes.SafeGet(this.CurClass).team));
                else PluginManager.TriggerEvent<IEventHandlerPlayerEscape>(new PlayerEscapeEvent(new PheggPlayer(this.gameObject), originalrole, this.CurClass, this.Classes.SafeGet(this.CurClass).team));
            }
            catch (Exception e) { Base.Error(e.Message); }
        }

    }
}
