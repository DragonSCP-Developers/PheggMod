﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using PheggMod.API.Plugin;
using PheggMod.API.Events;
using UnityEngine;
using System;
using PheggMod.API.Commands;
using Mirror;
using Cryptography;
using RemoteAdmin;
using System.Text.RegularExpressions;
using GameCore;
using System.Threading;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json.Linq;

using PheggMod;
using MEC;
using System.Runtime.InteropServices;

namespace DiscordLab
{
	[Plugin.PluginDetails(
		author = "ThePheggHimself",
		name = "DiscordLab",
		description = "Basic logging bot for SCP: Secret Laboratory",
		version = "1.0"
	)]

	public class DiscordLab : Plugin
	{
		public static Bot bot;
		public override void initializePlugin()
		{
			bot = new Bot();

			this.AddEventHandlers(new Events());

			Info("DiscordLab loaded!!");
		}
	}

	public class Bot
	{
		private static Regex _rgx = new Regex("(.gg/)|(<@)|(http)|(www)");
		private static Regex _filterNames = new Regex("(\\*)|(_)|({)|(})|(@)|(<)|(>)|(\")");

		private Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		private IPAddress _ipAddress;
		private DateTime _lastSentMessage;
		internal static char[] validUnits = { 'm', 'h', 'd', 'w', 'M', 'y' };

		public enum messageType
		{
			MSG = 0,
			CMD = 1,
			PLIST = 2,
			SUPDATE = 3,
			KEEPALIVE = 4
		}

		private class msgMessage
		{
			public string Type = "msg";
			public string Message;
		}
		private class cmdMessage
		{
			public string Type = "cmdmsg";
			public string CommandMessage;
			public string ChannelID;
			public string StaffID;
		}
		private class plistMessage
		{
			public string Type = "plist";
			public string PlayerNames;
			public string ChannelID;
		}
		private class supdateMessage
		{
			public string Type = "supdate";
			public string CurrentPlayers = PlayerManager.players.Count() + "/" + ConfigFile.ServerConfig.GetInt("max_players", 20);
			internal DateTime LastUpdate = DateTime.Now;
		}
		private class keepaliveMessage
		{
			public string Type = "keepalive";
		}

		public class botmessage
		{
			//Types: plist, command

			public string Type;
			public string channel;
			public string Message = null;
			public string StaffID = null;
			public string Staff = null;
		}

		private supdateMessage _lastSupdateMessage = null;

		public Bot()
		{
			_ipAddress = IPAddress.Parse(ConfigFile.ServerConfig.GetString("dl_address", "127.0.0.1"));

			Timing.RunCoroutine(StatusUpdate());
			Timing.RunCoroutine(KeepAlive());
			new Thread(() =>
			{
				BotListener();
			}).Start();
		}

		private IEnumerator<float> StatusUpdate()
		{
			supdateMessage message = new supdateMessage();

			if (_lastSupdateMessage == null || _lastSupdateMessage.CurrentPlayers != message.CurrentPlayers || (DateTime.Now - _lastSupdateMessage.LastUpdate).Seconds > 30)
			{
				SendMessage(JsonConvert.SerializeObject(message));
				_lastSupdateMessage = message;
			}

			yield return Timing.WaitForSeconds(6f);

			Timing.RunCoroutine(StatusUpdate());
		}
		private IEnumerator<float> KeepAlive()
		{
			if ((DateTime.Now - _lastSentMessage).TotalMinutes > 30)
				NewMessage("RandomStringToKeepAlive", messageType.KEEPALIVE);

			yield return Timing.WaitForSeconds(1800f);

			Timing.RunCoroutine(KeepAlive());
		}

		public void NewMessage(string message, messageType type = messageType.MSG, JObject jObj = null)
		{
			if (string.IsNullOrEmpty(message)) return;

			message.Replace("{", string.Empty).Replace("}", string.Empty);

			string json;

			if (type == messageType.MSG)
			{
				msgMessage msg = new msgMessage()
				{
					Message = _rgx.Replace(message, string.Empty)
				};

				json = JsonConvert.SerializeObject(msg);
			}
			else if (type == messageType.KEEPALIVE)
			{
				keepaliveMessage msg = new keepaliveMessage();

				json = JsonConvert.SerializeObject(msg);
			}
			else if (type == messageType.PLIST)
			{
				plistMessage msg = new plistMessage
				{
					ChannelID = jObj["channel"].ToString(),
					PlayerNames = message
				};

				json = JsonConvert.SerializeObject(msg);
			}
			else if (type == messageType.CMD)
			{
				cmdMessage msg = new cmdMessage
				{
					ChannelID = jObj["channel"].ToString(),
					StaffID = jObj["StaffID"].ToString(),
					CommandMessage = message
				};

				json = JsonConvert.SerializeObject(msg);
			}
			else if (type == messageType.SUPDATE)
			{
				supdateMessage msg = new supdateMessage();

				json = JsonConvert.SerializeObject(msg);
			}
			else
			{
				Plugin.Warn("Invalid messageType given!");
				return;
			}

			SendMessage(json);
		}
		private void SendMessage(string json)
		{
			if (!_socket.Connected)
				OpenConnection();

			if (!_socket.Connected) return;

			try
			{
				_socket.Send(Encoding.UTF8.GetBytes(json));
				_lastSentMessage = DateTime.Now;
			}
			catch (Exception e)
			{
				Plugin.Error(e.InnerException.Message + "\n" + e.InnerException.StackTrace);
			}
		}

		private void OpenConnection()
		{
			int port = ServerConsole.Port + 1000;

			if (port == 1000) return;
			try
			{
				_socket.Connect(_ipAddress, port);
				Plugin.Info("Bot connection established");
			}
			catch (Exception)
			{
				try
				{
					_socket.Disconnect(false);

					_socket.Connect(_ipAddress, port);
					Plugin.Info("Bot connection established");
				}
				catch (Exception e)
				{
					Plugin.Error($"{e.InnerException.Message}\n{e.InnerException.StackTrace}");
				}
			}
		}

		private void BotListener()
		{
			while (2 > 1)
			{
				if (_socket.Connected)
				{
					byte[] data = new byte[8192];
					int dataLength = _socket.Receive(data);

					string incomingData = Encoding.UTF8.GetString(data, 0, dataLength);
					List<string> messages = new List<string>(incomingData.Split('\n'));

					foreach (string message in messages)
					{
						if (!string.IsNullOrEmpty(message))
						{
							JObject jObj = JsonConvert.DeserializeObject<JObject>(message);

							if (jObj["Type"].ToString().ToLower() == "plist")
								NewMessage(Playerlist(), messageType.PLIST, jObj);
							else if (jObj["Type"].ToString().ToLower() == "cmd")
								NewMessage(HandleCommand(jObj), messageType.CMD, jObj);

						}
					}
				}
				Thread.Sleep(50);
			}
		}

		private string Playerlist()
		{
			if (PlayerManager.players.Count < 1)
			{
				if ((DateTime.Now - Events.RoundEnded).TotalSeconds < 45)
					return "*The round has recently restarted, so the playercount may not be accurate*\n**No online players**";
				else
					return "**No online players**";
			}

			List<string> players = new List<string>();
			foreach (GameObject go in PlayerManager.players)
				players.Add(_filterNames.Replace(go.GetComponent<NicknameSync>().MyNick, string.Empty));

			return $"**{PlayerManager.players.Count()}/{ConfigFile.ServerConfig.GetInt("max_players", 20)}**\n```\n{string.Join(", ", players)}```";
		}


		#region Commands

		private string HandleCommand(JObject jObj)
		{
			string[] command = jObj["Message"].ToString().Split(' ');

			switch (command[1].ToUpper())
			{
				case "BAN":
				case "RBAN":
				case "REMOTEBAN":
					return BanCommand(command, jObj);
				case "KICK":
				case "RKICK":
				case "REMOTEKICK":
					return KickCommand(command, jObj);
				case "UNBAN":
				case "RUNBAN":
				case "REMOTEUNBAN":
					return UnbanCommand(command, jObj);
				default:
					return "```diff\n- Invalid command```";
			}
		}

		private string BanCommand(string[] arg, JObject jObject)
		{
			if (arg.Count() < 5) return $"```{arg[1]} [UserID/Ip] [Duration] [Reason]```";

			bool validUID = arg[2].Contains('@');
			bool validIP = IPAddress.TryParse(arg[2], out IPAddress ip);

			//BanDetails details;

			if (!validIP && !validUID)
				return $"```diff\n- Invalid UserID or IP given```";

			var chars = arg[3].ToString().Where(Char.IsLetter).ToArray();

			if (chars.Length < 1)
				return "```diff\n- Invalid duration```";

			char unit = arg[3].ToString().Where(Char.IsLetter).ToArray()[0];

			if (!int.TryParse(new string(arg[3].Where(Char.IsDigit).ToArray()), out int amount) || !validUnits.Contains(unit) || amount < 1)
				return "```diff\n- Invalid duration```";

			TimeSpan duration = GetBanDuration(unit, amount);
			string reason = string.Join(" ", arg.Skip(4));


			int index;
			if (validUID)
				index = PlayerManager.players.FindIndex(p => p.GetComponent<CharacterClassManager>().UserId == arg[2]);
			else
				index = PlayerManager.players.FindIndex(p => p.GetComponent<CharacterClassManager>().connectionToClient.address == arg[2]);

			if (index > -1)
			{
				PheggPlayer player = new PheggPlayer(PlayerManager.players[index]);

				player.Ban(duration.Minutes, reason, jObject["Staff"].ToString(), true);
				try
				{
					player.refHub.networkIdentity.connectionToClient.Disconnect();
				}
				catch (Exception) { }
			}
			else
			{
				BanHandler.IssueBan(new BanDetails
				{
					OriginalName = "Offline player",
					Id = arg[2],
					Issuer = jObject["Staff"].ToString(),
					IssuanceTime = DateTime.UtcNow.Ticks,
					Expires = DateTime.UtcNow.Add(duration).Ticks,
					Reason = reason
				}, (validUID ? BanHandler.BanType.UserId : BanHandler.BanType.IP));
			}

			return $"`{arg[2]}` was banned for {arg[3]} with reason {reason}!";
		}
		private TimeSpan GetBanDuration(char unit, int amount)
		{
			switch (unit)
			{
				default:
					return new TimeSpan(0, 0, amount, 0);
				case 'h':
					return new TimeSpan(0, amount, 0, 0);
				case 'd':
					return new TimeSpan(amount, 0, 0, 0);
				case 'w':
					return new TimeSpan(7 * amount, 0, 0, 0);
				case 'M':
					return new TimeSpan(30 * amount, 0, 0, 0);
				case 'y':
					return new TimeSpan(365 * amount, 0, 0, 0);
			}
		}

		private string KickCommand(string[] arg, JObject jObject)
		{
			if (arg.Count() < 4) return $"```{arg[1]} [UserID] [Reason]```";
			else if (!arg[2].Contains('@')) return "Invalid UserID given";

			string reason = string.Join(" ", arg.Skip(3));

			GameObject go = PlayerManager.players.Where(p => p.GetComponent<CharacterClassManager>().UserId == arg[2]).FirstOrDefault();


			if (go == null || go.Equals(default(GameObject)))
			{
				return $"Unable to find user `{arg[2]}` on the server!";
			}
			else
			{
				PheggPlayer player = new PheggPlayer(go);
				player.Kick(reason);

				return $"`{player.ToString()}` was kicked with reason {reason}!";
			}
		}

		private string UnbanCommand(string[] arg, JObject jObject)
		{
			if (arg.Count() < 3) return $"```{arg[1]} [UserID/Ip]```";

			bool validUID = arg[2].Contains('@');
			bool validIP = IPAddress.TryParse(arg[2], out IPAddress ip);

			BanDetails details;

			if (!validIP && !validUID)
				return $"```diff\n- Invalid UserID or IP given```";

			if (validUID)
				details = BanHandler.QueryBan(arg[2], null).Key;
			else
				details = BanHandler.QueryBan(null, arg[2]).Value;

			if (details == null)
				return $"No ban found for `{arg[2]}`.\nMake sure you have typed it correctly, and that it has the @domain prefix if it's a UserID";

			BanHandler.RemoveBan(arg[2], (validUID ? BanHandler.BanType.UserId : BanHandler.BanType.IP));

			return $"`{arg[2]}` has been unbanned.";
		}

		#endregion
	}
}
