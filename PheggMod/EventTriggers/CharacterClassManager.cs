﻿#pragma warning disable CS0626 // orig_ method is marked external and has no attributes on it.
using System;
using MonoMod;
using UnityEngine;
using Mirror;
using PheggMod.API.Events;
using PheggMod.API;

namespace PheggMod.EventTriggers
{
	[MonoModPatch("global::CharacterClassManager")]
	class PMCharacterClassManager : CharacterClassManager
	{
		//WaitingForPlayers
		public extern System.Collections.IEnumerator orig_Init();
		public System.Collections.IEnumerator Init()
		{
			base.StartCoroutine(orig_Init());

			if (isLocalPlayer && NetworkServer.active && ServerStatic.IsDedicated)
			{
				try
				{
					Base.Debug("Triggering WaitingForPlayersEvent");
					PluginManager.TriggerEvent<IEventHandlerWaitingForPlayers>(new WaitingForPlayersEvent());
					PMAlphaWarheadController.nukeLock = false;
				}
				catch (Exception e)
				{
					Base.Error($"Error triggering WaitingForPlayersEvent: {e.InnerException}");
				}
			}

			yield return 1f;
		}

		//RoundStartEvent
		public extern static bool orig_ForceRoundStart();
		public new static bool ForceRoundStart()
		{
			bool Bool = orig_ForceRoundStart();

			Base.roundCount++;
			Base.roundStartTime = DateTime.Now;

			try
			{
				Base.Debug("Triggering RoundStartEvent");
				PluginManager.TriggerEvent<IEventHandlerRoundStart>(new RoundStartEvent());
			}
			catch (Exception e)
			{
				Base.Error($"Error triggering RoundStartEvent: {e.InnerException}");
			}

			if (FFDetector.FFDetector.DetectorEnabled)
				FFDetector.FFDetector.DoCheck = true;

			return Bool;
		}

		//PlayerSpawn / PlayerEscape
		public extern void orig_ApplyProperties(bool lite = false, bool escape = false);
		public void ApplyProperties(bool lite = false, bool escape = false)
		{
			try
			{
				RoleType originalrole = this.CurClass;
				orig_ApplyProperties(lite, escape);

				if (isLocalPlayer || (int)this.CurClass == 2) return;

				if (CurClass != RoleType.Spectator)
				{
					if (!escape)
						try
						{
							Base.Debug("Triggering PlayerSpawnEvent");
							PluginManager.TriggerEvent<IEventHandlerPlayerSpawn>(new PlayerSpawnEvent(new PheggPlayer(this.gameObject), this.CurClass, this.Classes.SafeGet(this.CurClass).team));
						}
						catch (Exception e)
						{
							Base.Error($"Error triggering PlayerSpawnEvent: {e.InnerException}");
						}
					else
						try
						{
							Base.Debug("Triggering PlayerEscapeEvent");
							PluginManager.TriggerEvent<IEventHandlerPlayerEscape>(new PlayerEscapeEvent(new PheggPlayer(this.gameObject), originalrole, this.CurClass, this.Classes.SafeGet(this.CurClass).team));
						}
						catch (Exception e)
						{
							Base.Error($"Error triggering PlayerEscapeEvent: {e.InnerException}");
						}
				}
			}
			catch (Exception e)
			{
				Base.Error(e.ToString());
			}
		}

		//[ServerCallback]
		//private void CmdStartRound()
		//{
		//    if (NetworkServer.active)
		//    {
		//        try
		//        {
		//            GameObject.Find("MeshDoor173").GetComponentInChildren<Door>().ForceCooldown(PMConfigFile.doorCooldown173);
		//            FindObjectOfType<ChopperAutostart>().SetState(b: false);
		//        }
		//        catch (Exception) { }
		//        NetworkRoundStarted = true;
		//    }
		//}
	}
}
