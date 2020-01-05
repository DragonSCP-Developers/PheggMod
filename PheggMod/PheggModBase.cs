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
using MEC;
using System.Threading;

namespace PheggMod
{
    [MonoModPatch("global::ServerConsole")]
    public class Base : ServerConsole
    {
        public static string APILocation = "https://corruptionbot.xyz/DragonSCP/";

        private static string _serverName = string.Empty;

        public extern static void orig_ReloadServerName();
        public new void ReloadServerName()
        {
            orig_ReloadServerName();
            _serverName += "<color=#ffffff00><size=1>SMPheggMod</size></color>";
        }
        public extern void orig_Start();
        public void Start()
        {
            orig_Start();
            AddLog("[PHEGGMOD] THIS SERVER IS RUNNING PHEGGMOD");

            PluginManager.PluginPreLoad();

            BotWorker.OpenConnection();
            Timing.RunCoroutine(BotWorker.UpdatePlayerCount());

            new Thread(() => BotWorker.BotListener()).Start();
        }

        public static void Error(string m) => Base.AddLog(string.Format("[{0}] {1}LOGTYPE-8", "ERROR", m));
        public static void Warn(string m) => Base.AddLog(string.Format("[{0}] {1}", "WARN", m));
        public static void Info(string m) => Base.AddLog(string.Format("[{0}] {1}", "INFO", m));
    }
}
