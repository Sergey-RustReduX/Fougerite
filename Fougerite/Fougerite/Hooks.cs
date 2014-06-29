﻿namespace Fougerite
{
    using Facepunch.Utility;
    using Fougerite.Events;
    using Rust;
    using RustPP;
    using RustPP.Commands;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using uLink;
    using UnityEngine;

    internal class Hooks
    {
        private static System.Collections.Generic.List<object> decayList = new System.Collections.Generic.List<object>();
        private static Hashtable talkerTimers = new Hashtable();

        public static event BlueprintUseHandlerDelagate OnBlueprintUse;

        public static event ChatHandlerDelegate OnChat;

        public static event CommandHandlerDelegate OnCommand;

        public static event ConsoleHandlerDelegate OnConsoleReceived;

        public static event DoorOpenHandlerDelegate OnDoorUse;

        public static event EntityDecayDelegate OnEntityDecay;

        public static event EntityDeployedDelegate OnEntityDeployed;

        public static event EntityHurtDelegate OnEntityHurt;

        public static event ItemsDatablocksLoaded OnItemsLoaded;

        public static event HurtHandlerDelegate OnNPCHurt;

        public static event KillHandlerDelegate OnNPCKilled;

        public static event ConnectionHandlerDelegate OnPlayerConnected;

        public static event DisconnectionHandlerDelegate OnPlayerDisconnected;

        public static event PlayerGatheringHandlerDelegate OnPlayerGathering;

        public static event HurtHandlerDelegate OnPlayerHurt;

        public static event KillHandlerDelegate OnPlayerKilled;

        public static event PlayerSpawnHandlerDelegate OnPlayerSpawned;

        public static event PlayerSpawnHandlerDelegate OnPlayerSpawning;

        public static event PluginInitHandlerDelegate OnPluginInit;

        public static event ServerInitDelegate OnServerInit;

        public static event ServerShutdownDelegate OnServerShutdown;

        public static event LootTablesLoaded OnTablesLoaded;

        public static void BlueprintUse(IBlueprintItem item, BlueprintDataBlock bdb)
        {
            Fougerite.Player player = Fougerite.Player.FindByPlayerClient(item.controllable.playerClient);
            if (player != null)
            {
                BPUseEvent ae = new BPUseEvent(bdb);
                if (OnBlueprintUse != null)
                {
                    OnBlueprintUse(player, ae);
                }
                if (!ae.Cancel)
                {
                    PlayerInventory internalInventory = player.Inventory.InternalInventory as PlayerInventory;
                    if (internalInventory.BindBlueprint(bdb))
                    {
                        int count = 1;
                        if (item.Consume(ref count))
                        {
                            internalInventory.RemoveItem(item.slot);
                        }
                        player.Notice("", "You can now craft: " + bdb.resultItem.name, 4f);
                    }
                    else
                    {
                        player.Notice("", "You already have this blueprint", 4f);
                    }
                }
            }
        }

        public static void ChatReceived(ref ConsoleSystem.Arg arg)
        {
            if (chat.enabled)
            {
                string item = Facepunch.Utility.String.QuoteSafe(arg.argUser.user.Displayname);
                string str = Facepunch.Utility.String.QuoteSafe(arg.GetString(0, "text"));
                TeleportToCommand command = ChatCommand.GetCommand("tpto") as TeleportToCommand;
                if (command.GetTPWaitList().Contains(arg.argUser.userID))
                {
                    int num;
                    if (int.TryParse(arg.GetString(0, "text").Trim(), out num))
                    {
                        command.PartialNameTP(ref arg, num);
                    }
                    else
                    {
                        Util.sayUser(arg.argUser.networkPlayer, "Invalid Choice!");
                        command.GetTPWaitList().Remove(arg.argUser.userID);
                    }
                }
                else if (Core.banWaitList.Contains(arg.argUser.userID))
                {
                    int num2;
                    if (int.TryParse(arg.GetString(0, "text").Trim(), out num2))
                    {
                        (ChatCommand.GetCommand("ban") as BanCommand).PartialNameBan(ref arg, num2);
                    }
                    else
                    {
                        Util.sayUser(arg.argUser.networkPlayer, "Invalid Choice!");
                        Core.banWaitList.Remove(arg.argUser.userID);
                    }
                }
                else if (Core.kickWaitList.Contains(arg.argUser.userID))
                {
                    int num3;
                    if (int.TryParse(arg.GetString(0, "text").Trim(), out num3))
                    {
                        (ChatCommand.GetCommand("kick") as KickCommand).PartialNameKick(ref arg, num3);
                    }
                    else
                    {
                        Util.sayUser(arg.argUser.networkPlayer, "Invalid Choice!");
                        Core.kickWaitList.Remove(arg.argUser.userID);
                    }
                }
                else if (((str != null) && (str.Length > 1)) && str.Substring(1, 1).Equals("/"))
                {
                    handleCommand(ref arg);
                    if (Core.IsEnabled())
                    {
                        Core.handleCommand(ref arg);
                    }
                }
                else
                {
                    ChatString text = new ChatString(str);
                    if (OnChat != null)
                    {
                        OnChat(Fougerite.Player.FindByPlayerClient(arg.argUser.playerClient), ref text);
                    }
                    str = Facepunch.Utility.String.QuoteSafe(text.NewText.Substring(1, text.NewText.Length - 2));
                    if (str != "")
                    {
                        if (Core.IsEnabled() && Core.muteList.Contains(arg.argUser.userID))
                        {
                            Util.sayUser(arg.argUser.networkPlayer, "You are muted.");
                        }
                        else
                        {
                            Fougerite.Data.GetData().chat_history.Add(str);
                            Fougerite.Data.GetData().chat_history_username.Add(item);
                            Logger.ChatLog(item, str);
                            ConsoleNetworker.Broadcast("chat.add " + item + " " + str);
                        }
                    }
                }
            }
        }

        public static bool CheckOwner(DeployableObject obj, Controllable controllable)
        {
            DoorEvent de = new DoorEvent(new Entity(obj));
            if (obj.ownerID == controllable.playerClient.userID)
            {
                de.Open = true;
            }
            if (!(obj is SleepingBag))
            {
                if (Core.IsEnabled() && !de.Open)
                {
                    ShareCommand command = ChatCommand.GetCommand("share") as ShareCommand;
                    ArrayList list = (ArrayList)command.GetSharedDoors()[obj.ownerID];
                    if (list == null)
                    {
                        de.Open = false;
                    }
                    else if (list.Contains(controllable.playerClient.userID))
                    {
                        de.Open = true;
                    }
                    else
                    {
                        de.Open = false;
                    }
                }
                if (OnDoorUse != null)
                {
                    OnDoorUse(Fougerite.Player.FindByPlayerClient(controllable.playerClient), de);
                }
            }
            return de.Open;
        }

        public static bool ConsoleReceived(ref ConsoleSystem.Arg a)
        {
            if (((a.argUser == null) && (a.Class == "fougeriteweb")) && (a.Function == "handshake"))
            {
                a.ReplyWith("All Good!");
                return true;
            }
            bool external = a.argUser == null;
            if (OnConsoleReceived != null)
            {
                OnConsoleReceived(ref a, external);
            }
            if ((a.Class.ToLower() == "fougerite") && (a.Function.ToLower() == "reload"))
            {
                if ((a.argUser != null) && a.argUser.admin)
                {
                    PluginEngine.GetPluginEngine().ReloadPlugins(Fougerite.Player.FindByPlayerClient(a.argUser.playerClient));
                    a.ReplyWith("Fougerite: Reloaded!");
                }
                else if (external)
                {
                    PluginEngine.GetPluginEngine().ReloadPlugins(null);
                    a.ReplyWith("Fougerite: Reloaded!");
                }
            }
            if ((a.Reply == null) || (a.Reply == ""))
            {
                a.ReplyWith("Fougerite: " + a.Class + "." + a.Function + " was executed!");
            }
            return true;
        }

        public static float EntityDecay(object entity, float dmg)
        {
            DecayEvent de = new DecayEvent(new Entity(entity), ref dmg);
            if (OnEntityDecay != null)
            {
                OnEntityDecay(de);
            }
            if (Core.IsEnabled() && (Core.config.GetSetting("Settings", "decay") == "false"))
            {
                de.DamageAmount = 0f;
            }
            if (decayList.Contains(entity))
            {
                decayList.Remove(entity);
            }
            decayList.Add(entity);
            return de.DamageAmount;
        }

        public static void EntityDeployed(object entity)
        {
            Entity e = new Entity(entity);
            Fougerite.Player creator = e.Creator;

            if (Core.IsEnabled() && Core.config.GetSetting("Settings", "rampfix") == "true")
                RampFix(e, creator);

            if (OnEntityDeployed != null)
                OnEntityDeployed(creator, e);
        }

        private static void RampFix(Entity e, Player creator) // by dretax14 (RampFix plugin)
        {
            if (e != null)
                if (e.Name == "WoodRamp" || e.Name == "MetalRamp")
                {
                    var name = e.Name;
                    foreach (Entity ent in World.GetWorld().Entities)
                    {
                        if (ent.Name == "WoodRamp" || ent.Name == "MetalRamp")
                        {
                            var one = Util.GetUtil().CreateVector(ent.X, ent.Y, ent.Z);
                            var two = Util.GetUtil().CreateVector(e.X, e.Y, e.Z);
                            var dist = Util.GetUtil().GetVectorsDistance(one, two);
                            if (e != ent && e.InstanceID != ent.InstanceID)
                                if (dist == 0)
                                {
                                    if (Core.config.GetSetting("Settings", "rampgiveback") == "true" && creator != null)
                                    {
                                        if (name == "WoodRamp")
                                            name = "Wood Ramp";
                                        else if (name == "MetalRamp")
                                            name = "Metal Ramp";

                                        // Make sure that the player is online
                                        creator.Inventory.AddItem(name, 1);
                                    }
                                    e.Destroy();
                                }
                        }
                    }
                }
        }

        public static void EntityHurt(object entity, ref DamageEvent e)
        {
            try
            {
                HurtEvent he = new HurtEvent(ref e, new Entity(entity));
                if (decayList.Contains(entity))
                {
                    he.IsDecay = true;
                }
                if (he.Entity.IsStructure() && !he.IsDecay)
                {
                    StructureComponent component = entity as StructureComponent;
                    if ((component.IsType(StructureComponent.StructureComponentType.Ceiling) || component.IsType(StructureComponent.StructureComponentType.Foundation)) || component.IsType(StructureComponent.StructureComponentType.Pillar))
                    {
                        he.DamageAmount = 0f;
                    }
                }
                TakeDamage takeDamage = he.Entity.GetTakeDamage();
                takeDamage.health += he.DamageAmount;
                if (Core.IsEnabled())
                {
                    InstaKOCommand command = ChatCommand.GetCommand("instako") as InstaKOCommand;
                    if (command.IsOn(e.attacker.client.userID))
                    {
                        if (!he.IsDecay)
                        {
                            he.Entity.Destroy();
                        }
                        else
                        {
                            decayList.Remove(entity);
                        }
                    }
                }
                if (OnEntityHurt != null)
                {
                    OnEntityHurt(he);
                }
                Zone3D zoned = Zone3D.GlobalContains(he.Entity);
                if ((zoned == null) || !zoned.Protected)
                {
                    if ((he.Entity.GetTakeDamage().health - he.DamageAmount) <= 0f)
                    {
                        he.Entity.Destroy();
                    }
                    else
                    {
                        TakeDamage damage2 = he.Entity.GetTakeDamage();
                        damage2.health -= he.DamageAmount;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static void handleCommand(ref ConsoleSystem.Arg arg)
        {
            string displayname = arg.argUser.user.Displayname;
            string[] strArray = arg.GetString(0, "text").Trim().Split(new char[] { ' ' });
            string text = strArray[0].Trim().Remove(0, 1);
            string[] args = new string[strArray.Length - 1];
            for (int i = 1; i < strArray.Length; i++)
            {
                args[i - 1] = strArray[i].Trim();
            }
            if (OnCommand != null)
            {
                OnCommand(Fougerite.Player.FindByPlayerClient(arg.argUser.playerClient), text, args);
            }
        }

        public static void hijack(string name)
        {
            if ((((name != "!Ng") && (name != ":rabbit_prefab_a")) && ((name != ";res_woodpile") && (name != ";res_ore_1"))) && ((((((((((((((name != ";res_ore_2") & (name != ";res_ore_3")) & (name != ":stag_prefab")) & (name != ":boar_prefab")) & (name != ":chicken_prefab")) & (name != ":bear_prefab")) & (name != ":wolf_prefab")) & (name != ":mutant_bear")) & (name != ":mutant_wolf")) & (name != "AmmoLootBox")) & (name != "MedicalLootBox")) & (name != "BoxLoot")) & (name != "WeaponLootBox")) & (name != "SupplyCrate")))
            {
                Logger.Log(name);
            }
        }

        public static ItemDataBlock[] ItemsLoaded(System.Collections.Generic.List<ItemDataBlock> items, Dictionary<string, int> stringDB, Dictionary<int, int> idDB)
        {
            ItemsBlocks blocks = new ItemsBlocks(items);
            if (OnItemsLoaded != null)
            {
                OnItemsLoaded(blocks);
            }
            int num = 0;
            foreach (ItemDataBlock block in blocks)
            {
                stringDB.Add(block.name, num);
                idDB.Add(block.uniqueID, num);
                num++;
            }
            Fougerite.Server.GetServer().Items = blocks;
            return blocks.ToArray();
        }

        public static void NPCHurt(ref DamageEvent e)
        {
            try
            {
                HurtEvent he = new HurtEvent(ref e);
                if ((he.Victim as NPC).Health > 0f)
                {
                    NPC victim = he.Victim as NPC;
                    victim.Health += he.DamageAmount;
                    if (OnNPCHurt != null)
                    {
                        OnNPCHurt(he);
                    }
                    if (((he.Victim as NPC).Health - he.DamageAmount) <= 0f)
                    {
                        (he.Victim as NPC).Kill();
                    }
                    else
                    {
                        NPC npc2 = he.Victim as NPC;
                        npc2.Health -= he.DamageAmount;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static void NPCKilled(ref DamageEvent e)
        {
            try
            {
                DeathEvent de = new DeathEvent(ref e);
                if (OnNPCKilled != null)
                {
                    OnNPCKilled(de);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static bool PlayerConnect(NetUser user)
        {
            Fougerite.Player item = new Fougerite.Player(user.playerClient);
            Fougerite.Server.GetServer().Players.Add(item);
            bool connected = user.connected;
            if (Core.IsEnabled())
            {
                connected = RustPP.Hooks.loginNotice(user);
            }
            if (OnPlayerConnected != null)
            {
                OnPlayerConnected(item);
            }
            item.Message("This server is powered by Fougerite v." + Bootstrap.Version + "!");
            return connected;
        }

        public static void PlayerDisconnect(NetUser user)
        {
            Fougerite.Player item = Fougerite.Player.FindByPlayerClient(user.playerClient);
            if (item != null)
            {
                Fougerite.Server.GetServer().Players.Remove(item);
            }
            if (Core.IsEnabled())
            {
                RustPP.Hooks.logoffNotice(user);
            }
            if (OnPlayerDisconnected != null)
            {
                OnPlayerDisconnected(item);
            }
        }

        public static void PlayerGather(Inventory rec, ResourceTarget rt, ResourceGivePair rg, ref int amount)
        {
            Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, rg, amount);
            if (OnPlayerGathering != null)
            {
                OnPlayerGathering(player, ge);
            }
            amount = ge.Quantity;
            if (!ge.Override)
            {
                amount = Mathf.Min(amount, rg.AmountLeft());
            }
            rg._resourceItemDatablock = ge.Item;
            rg.ResourceItemName = ge.Item;
        }

        public static void PlayerGatherWood(IMeleeWeaponItem rec, ResourceTarget rt, ref ItemDataBlock db, ref int amount, ref string name)
        {
            Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.inventory.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, db, amount);
            ge.Item = "Wood";
            if (OnPlayerGathering != null)
            {
                OnPlayerGathering(player, ge);
            }
            db = Fougerite.Server.GetServer().Items.Find(ge.Item);
            amount = ge.Quantity;
            name = ge.Item;
        }

        public static void PlayerHurt(ref DamageEvent e)
        {
            HurtEvent he = new HurtEvent(ref e);
            if (!(he.Attacker is NPC) && !(he.Victim is NPC))
            {
                Fougerite.Player attacker = he.Attacker as Fougerite.Player;
                Fougerite.Player victim = he.Victim as Fougerite.Player;
                Zone3D zoned = Zone3D.GlobalContains(attacker);
                if ((zoned != null) && !zoned.PVP)
                {
                    attacker.Message("You are in a PVP restricted area.");
                    he.DamageAmount = 0f;
                    e = he.DamageEvent;
                    return;
                }
                zoned = Zone3D.GlobalContains(victim);
                if ((zoned != null) && !zoned.PVP)
                {
                    attacker.Message(victim.Name + " is in a PVP restricted area.");
                    he.DamageAmount = 0f;
                    e = he.DamageEvent;
                    return;
                }
            }
            if (Core.IsEnabled() && RustPP.Hooks.IsFriend(ref e))
            {
                he.DamageAmount = 0f;
            }
            if (OnPlayerHurt != null)
            {
                OnPlayerHurt(he);
            }
            e = he.DamageEvent;
        }

        public static bool PlayerKilled(ref DamageEvent de)
        {
            try
            {
                DeathEvent event2 = new DeathEvent(ref de);
                if (Core.IsEnabled() && !(event2.Attacker is NPC))
                {
                    event2.DropItems = !RustPP.Hooks.KeepItem();
                    Fougerite.Player attacker = event2.Attacker as Fougerite.Player;
                    Fougerite.Player victim = event2.Victim as Fougerite.Player;
                    if ((attacker.Name != victim.Name) && (Fougerite.Server.GetServer().FindPlayer(attacker.Name) != null))
                    {
                        RustPP.Hooks.broadcastDeath(victim.Name, attacker.Name, event2.WeaponName);
                    }
                }
                if (OnPlayerKilled != null)
                {
                    OnPlayerKilled(event2);
                }
                return event2.DropItems;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                return true;
            }
        }

        public static void PlayerSpawned(PlayerClient pc, Vector3 pos, bool camp)
        {
            try
            {
                Fougerite.Player player = Fougerite.Player.FindByPlayerClient(pc);
                SpawnEvent se = new SpawnEvent(pos, camp);
                if ((OnPlayerSpawned != null) && (player != null))
                {
                    OnPlayerSpawned(player, se);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        public static Vector3 PlayerSpawning(PlayerClient pc, Vector3 pos, bool camp)
        {
            try
            {
                Fougerite.Player player = Fougerite.Player.FindByPlayerClient(pc);
                SpawnEvent se = new SpawnEvent(pos, camp);
                if ((OnPlayerSpawning != null) && (player != null))
                {
                    OnPlayerSpawning(player, se);
                }
                return new Vector3(se.X, se.Y, se.Z);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return Vector3.zero;
        }

        public static void PluginInit()
        {
            if (OnPluginInit != null)
            {
                OnPluginInit();
            }
        }

        public static void ResetHooks()
        {
            OnPluginInit = delegate
            {
            };
            OnChat = delegate(Fougerite.Player param0, ref ChatString param1)
            {
            };
            OnCommand = delegate(Fougerite.Player param0, string param1, string[] param2)
            {
            };
            OnPlayerConnected = delegate(Fougerite.Player param0)
            {
            };
            OnPlayerDisconnected = delegate(Fougerite.Player param0)
            {
            };
            OnNPCKilled = delegate(DeathEvent param0)
            {
            };
            OnNPCHurt = delegate(HurtEvent param0)
            {
            };
            OnPlayerKilled = delegate(DeathEvent param0)
            {
            };
            OnPlayerHurt = delegate(HurtEvent param0)
            {
            };
            OnPlayerSpawned = delegate(Fougerite.Player param0, SpawnEvent param1)
            {
            };
            OnPlayerSpawning = delegate(Fougerite.Player param0, SpawnEvent param1)
            {
            };
            OnPlayerGathering = delegate(Fougerite.Player param0, GatherEvent param1)
            {
            };
            OnEntityHurt = delegate(HurtEvent param0)
            {
            };
            OnEntityDecay = delegate(DecayEvent param0)
            {
            };
            OnEntityDeployed = delegate(Fougerite.Player param0, Entity param1)
            {
            };
            OnConsoleReceived = delegate(ref ConsoleSystem.Arg param0, bool param1)
            {
            };
            OnBlueprintUse = delegate(Fougerite.Player param0, BPUseEvent param1)
            {
            };
            OnDoorUse = delegate(Fougerite.Player param0, DoorEvent param1)
            {
            };
            OnTablesLoaded = delegate(Dictionary<string, LootSpawnList> param0)
            {
            };
            OnItemsLoaded = delegate(ItemsBlocks param0)
            {
            };
            OnServerInit = delegate
            {
            };
            OnServerShutdown = delegate
            {
            };
            foreach (Fougerite.Player player in Fougerite.Server.GetServer().Players)
            {
                player.FixInventoryRef();
            }
        }

        public static void ServerShutdown()
        {
            if (Core.IsEnabled())
            {
                Helper.CreateSaves();
            }
            if (OnServerShutdown != null)
            {
                OnServerShutdown();
            }
            DataStore.GetInstance().Save();
        }

        public static void ServerStarted()
        {
            DataStore.GetInstance().Load();
            if (OnServerInit != null)
            {
                OnServerInit();
            }
        }

        public static void ShowTalker(uLink.NetworkPlayer player, PlayerClient p)
        {
            if (Core.IsEnabled())
            {
                try
                {
                    if (Core.config.GetSetting("Settings", "voice_notifications") == "true")
                    {
                        if (talkerTimers.ContainsKey(p.userID))
                        {
                            if ((Environment.TickCount - ((int)talkerTimers[p.userID])) < int.Parse(Core.config.GetSetting("Settings", "voice_notification_delay")))
                            {
                                return;
                            }
                            talkerTimers[p.userID] = Environment.TickCount;
                        }
                        else
                        {
                            talkerTimers.Add(p.userID, Environment.TickCount);
                        }
                        Notice.Inventory(player, "☎ " + p.netUser.displayName);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
            }
        }

        public static Dictionary<string, LootSpawnList> TablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            if (OnTablesLoaded != null)
            {
                OnTablesLoaded(lists);
            }
            return lists;
        }

        public delegate void BlueprintUseHandlerDelagate(Fougerite.Player player, BPUseEvent ae);

        public delegate void ChatHandlerDelegate(Fougerite.Player player, ref ChatString text);

        public delegate void CommandHandlerDelegate(Fougerite.Player player, string text, string[] args);

        public delegate void ConnectionHandlerDelegate(Fougerite.Player player);

        public delegate void ConsoleHandlerDelegate(ref ConsoleSystem.Arg arg, bool external);

        public delegate void DisconnectionHandlerDelegate(Fougerite.Player player);

        public delegate void DoorOpenHandlerDelegate(Fougerite.Player p, DoorEvent de);

        public delegate void EntityDecayDelegate(DecayEvent de);

        public delegate void EntityDeployedDelegate(Fougerite.Player player, Entity e);

        public delegate void EntityHurtDelegate(HurtEvent he);

        public delegate void HurtHandlerDelegate(HurtEvent he);

        public delegate void ItemsDatablocksLoaded(ItemsBlocks items);

        public delegate void KillHandlerDelegate(DeathEvent de);

        public delegate void LootTablesLoaded(Dictionary<string, LootSpawnList> lists);

        public delegate void PlayerGatheringHandlerDelegate(Fougerite.Player player, GatherEvent ge);

        public delegate void PlayerSpawnHandlerDelegate(Fougerite.Player player, SpawnEvent se);

        public delegate void PluginInitHandlerDelegate();

        public delegate void ServerInitDelegate();

        public delegate void ServerShutdownDelegate();
    }
}