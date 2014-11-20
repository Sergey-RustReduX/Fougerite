﻿namespace RustPP
{
    using Fougerite;
    using Fougerite.Events;
    using Rust;
    using RustPP.Commands;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Reflection;
    using System.Timers;

    public class RustPPModule : Fougerite.Module
    {
        public override string Name
        {
            get { return "RustPP"; }
        }
        public override string Author
        {
            get { return "xEnt22"; }
        }
        public override string Description
        {
            get { return "Rust++ Legacy Module"; }
        }
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public static string GetAbsoluteFilePath(string fileName)
        {
            return Path.Combine(ConfigsFolder, fileName);
        }

        public override uint Order
        {
            get { return uint.MinValue; }
        }

        public static string ConfigFile;
        public static string ConfigsFolder;
        public override void Initialize()
        {
            ConfigsFolder = ModuleFolder;
            ConfigFile = Path.Combine(ConfigsFolder, "Rust++.cfg");

            Core.Init();

            try
            {
                Core.config = new IniParser(ConfigFile);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }

            if (Core.config == null)
            {
                Logger.LogError("[RPP] Can't load config!");
                return;
            }
            string chatname = Core.config.GetSetting("Settings", "chatname");
            if (!string.IsNullOrEmpty(chatname))
            {
                Core.Name = Core.config.GetSetting("Settings", "chatname");
            }
            TimedEvents.startEvents();

            Fougerite.Hooks.OnDoorUse += DoorUse;
            Fougerite.Hooks.OnEntityHurt += EntityHurt;
            Fougerite.Hooks.OnPlayerConnected += PlayerConnect;
            Fougerite.Hooks.OnPlayerDisconnected += PlayerDisconnect;
            Fougerite.Hooks.OnPlayerHurt += PlayerHurt;
            Fougerite.Hooks.OnPlayerKilled += PlayerKilled;
            Fougerite.Hooks.OnServerShutdown += ServerShutdown;
            Fougerite.Hooks.OnShowTalker += ShowTalker;
            Fougerite.Hooks.OnChatRaw += ChatReceived;
            Fougerite.Hooks.OnChat += Chat;
        }

        public override void DeInitialize()
        {
            Logger.LogDebug("DeInitialize RPP");

            Fougerite.Hooks.OnDoorUse -= DoorUse;
            Fougerite.Hooks.OnEntityHurt -= EntityHurt;
            Fougerite.Hooks.OnPlayerConnected -= PlayerConnect;
            Fougerite.Hooks.OnPlayerDisconnected -= PlayerDisconnect;
            Fougerite.Hooks.OnPlayerHurt -= PlayerHurt;
            Fougerite.Hooks.OnPlayerKilled -= PlayerKilled;
            Fougerite.Hooks.OnServerShutdown -= ServerShutdown;
            Fougerite.Hooks.OnShowTalker -= ShowTalker;
            Fougerite.Hooks.OnChatRaw -= ChatReceived;
            Fougerite.Hooks.OnChat -= Chat;

            Logger.LogDebug("DeInitialized RPP");
        }

        void TimeEvent(object x, ElapsedEventArgs y)
        {
            TimedEvents.startEvents();
        }

        void ChatReceived(ref ConsoleSystem.Arg arg)
        {
            var command = ChatCommand.GetCommand("tpto") as TeleportToCommand;
            if (command.GetTPWaitList().Contains(arg.argUser.userID)) {
                command.PartialNameTP(ref arg, arg.GetInt(0));
                arg.ArgsStr = string.Empty;
            } else if (Core.banWaitList.Contains(arg.argUser.userID)) {
                (ChatCommand.GetCommand("ban") as BanCommand).PartialNameBan(ref arg, arg.GetInt(0));
                arg.ArgsStr = string.Empty;
            } else if (Core.kickWaitList.Contains(arg.argUser.userID)) {
                (ChatCommand.GetCommand("kick") as KickCommand).PartialNameKick(ref arg, arg.GetInt(0));  
                arg.ArgsStr = string.Empty;
            }

            if (Core.IsEnabled())
                Core.handleCommand(ref arg);
            
        }

        void Chat(Fougerite.Player p, ref ChatString text)
        {
            if (Core.IsEnabled() && Core.muteList.Contains(p.PlayerClient.netUser.userID)) // p.PlayerClient.userID
            {
                text.NewText = string.Empty;
                Util.sayUser(p.PlayerClient.netUser.networkPlayer, Core.Name, "You are muted.");
            }
        }

        void ShowTalker(uLink.NetworkPlayer player, PlayerClient p)
        {
            if (Core.IsEnabled())
            {
                try
                {
                    if (Core.config.GetSetting("Settings", "voice_notifications") == "true")
                    {
                        if (Fougerite.Hooks.talkerTimers.ContainsKey(p.userID))
                        {
                            if ((Environment.TickCount - ((int)Fougerite.Hooks.talkerTimers[p.userID])) < int.Parse(Core.config.GetSetting("Settings", "voice_notification_delay")))
                                return;
                            Fougerite.Hooks.talkerTimers[p.userID] = Environment.TickCount;
                        }
                        else
                            Fougerite.Hooks.talkerTimers.Add(p.userID, Environment.TickCount);
                        Notice.Inventory(player, "☎ " + p.netUser.displayName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        void ServerShutdown()
        {
            if (Core.IsEnabled())
                Helper.CreateSaves();
        }

        void PlayerKilled(DeathEvent event2)
        {
            event2.DropItems = !RustPP.Hooks.KeepItem();
            if (!(event2.Attacker is NPC)) // Not NPC
            {
                Fougerite.Player attacker = event2.Attacker as Fougerite.Player;
                Fougerite.Player victim = event2.Victim as Fougerite.Player;
                if ((attacker.Name != victim.Name) && (Fougerite.Server.GetServer().FindPlayer(attacker.Name) != null))
                    RustPP.Hooks.broadcastDeath(victim.Name, attacker.Name, event2.WeaponName);
            }
        }

        void DoorUse(Fougerite.Player p, DoorEvent de)
        {
            if (Core.IsEnabled() && !de.Open)
            {
                ShareCommand command = ChatCommand.GetCommand("share") as ShareCommand;
                ArrayList list = (ArrayList)command.GetSharedDoors()[de.Entity.OwnerID];
                if (list == null)
                    de.Open = false;
                else if (list.Contains(p.PlayerClient.userID))
                    de.Open = true;
                else
                    de.Open = false;
            }
        }

        void PlayerHurt(HurtEvent he) // Dirty Hack?
        {
            if (RustPP.Hooks.IsFriend(he.DamageEvent))
                he.DamageAmount = 0f;
        }

        void PlayerDisconnect(Fougerite.Player player)
        {
            if (Core.IsEnabled())
                RustPP.Hooks.logoffNotice(player.PlayerClient.netUser);
        }

        void PlayerConnect(Fougerite.Player player)
        {
            if (Core.IsEnabled())
                RustPP.Hooks.loginNotice(player.PlayerClient.netUser);
        }

        void EntityHurt(HurtEvent he)
        {
            if (Core.IsEnabled())
            {
                InstaKOCommand command = ChatCommand.GetCommand("instako") as InstaKOCommand;

                if (!(he.Attacker is Fougerite.Player))
                {
                    return;
                }

                if (command == null)
                    return;

                if (command.IsOn(((Fougerite.Player)he.Attacker).PlayerClient.userID))
                {
                    if (he.Entity != null)
                    {
                        try
                        {
                            if (!he.IsDecay)
                                he.Entity.Destroy();
                            else if (Fougerite.Hooks.decayList.Contains(he.Entity))
                                Fougerite.Hooks.decayList.Remove(he.Entity);
                        }
                        catch (Exception ex)
                        { Logger.LogDebug("EntityHurt EX: " + ex); }
                    }
                    else Logger.LogDebug("he.Entity is null!");
                }
            }
        }
    }
}
