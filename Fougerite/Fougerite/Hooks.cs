﻿using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Timers;
using Facepunch.Clocks.Counters;
using Google.ProtocolBuffers.Serialization;
using RustProto;
using RustProto.Helpers;

namespace Fougerite
{
    using uLink;
    using Fougerite.Events;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Text.RegularExpressions;

    public class Hooks
    {
        public static System.Collections.Generic.List<object> decayList = new System.Collections.Generic.List<object>();
        public static Hashtable talkerTimers = new Hashtable();

        public static event BlueprintUseHandlerDelegate OnBlueprintUse;
        public static event ChatHandlerDelegate OnChat;
        public static event ChatRawHandlerDelegate OnChatRaw;
        public static event CommandHandlerDelegate OnCommand;
        public static event CommandRawHandlerDelegate OnCommandRaw;
        public static event ConsoleHandlerDelegate OnConsoleReceived;
        public static event DoorOpenHandlerDelegate OnDoorUse;
        public static event EntityDecayDelegate OnEntityDecay;
        [System.Obsolete("Use OnEntityDeployedWithPlacer", false)]
        public static event EntityDeployedDelegate OnEntityDeployed;
        public static event EntityDeployedWithPlacerDelegate OnEntityDeployedWithPlacer;
        public static event EntityHurtDelegate OnEntityHurt;
        public static event EntityDestroyedDelegate OnEntityDestroyed;
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
        public static event TeleportDelegate OnPlayerTeleport;
        public static event ServerInitDelegate OnServerInit;
        public static event ServerShutdownDelegate OnServerShutdown;
        public static event ShowTalkerDelegate OnShowTalker;
        public static event LootTablesLoaded OnTablesLoaded;
        public static event ModulesLoadedDelegate OnModulesLoaded;
        public static event RecieveNetworkDelegate OnRecieveNetwork;
        public static event CraftingDelegate OnCrafting;
        public static event ResourceSpawnDelegate OnResourceSpawned;
        public static event ItemRemovedDelegate OnItemRemoved;
        public static event ItemAddedDelegate OnItemAdded;
        public static event AirdropDelegate OnAirdropCalled;
        //public static event AirdropCrateDroppedDelegate OnAirdropCrateDropped;
        public static event SteamDenyDelegate OnSteamDeny;
        public static event PlayerApprovalDelegate OnPlayerApproval;
        public static event PlayerMoveDelegate OnPlayerMove;
        public static event ResearchDelegate OnResearch;
        public static event ServerSavedDelegate OnServerSaved;
        public static event ItemPickupDelegate OnItemPickup;
        public static event FallDamageDelegate OnFallDamage;
        public static event LootEnterDelegate OnLootUse;
        public static event ShootEventDelegate OnShoot;
        public static event ShotgunShootEventDelegate OnShotgunShoot;
        public static event BowShootEventDelegate OnBowShoot;
        public static event GrenadeThrowEventDelegate OnGrenadeThrow;
        public static event BanEventDelegate OnPlayerBan;
        public static event RepairBenchEventDelegate OnRepairBench;
        public static event ItemMoveEventDelegate OnItemMove;
        public static bool IsShuttingDown = false;

        public static readonly List<ulong> uLinkDCCache = new List<ulong>(); 

        public static void BlueprintUse(IBlueprintItem item, BlueprintDataBlock bdb)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            //Fougerite.Player player = Fougerite.Player.FindByPlayerClient(item.controllable.playerClient);
            Fougerite.Player player = Fougerite.Server.Cache[item.controllable.playerClient.userID];
            if (player != null)
            {
                BPUseEvent ae = new BPUseEvent(bdb, item);
                if (OnBlueprintUse != null)
                {
                    try
                    {
                        OnBlueprintUse(player, ae);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("BluePrintUseEvent Error: " + ex.ToString());
                    }
                }
                if (!ae.Cancel)
                {
                    PlayerInventory internalInventory = player.Inventory.InternalInventory as PlayerInventory;
                    if (internalInventory != null && internalInventory.BindBlueprint(bdb))
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
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("BluePrintEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void ChatReceived(ref ConsoleSystem.Arg arg)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            if (!chat.enabled) { return; }

            if (string.IsNullOrEmpty(arg.ArgsStr)) { return; }

            var quotedName = Facepunch.Utility.String.QuoteSafe(arg.argUser.displayName);
            var quotedMessage = Facepunch.Utility.String.QuoteSafe(arg.GetString(0));
            if (quotedMessage.Trim('"').StartsWith("/"))
            {
                Logger.LogDebug("[CHAT-CMD] " + quotedName + " executed " + quotedMessage);
            }

            if (OnChatRaw != null)
            {
                try
                {
                    OnChatRaw(ref arg);
                }
                catch (Exception ex)
                {
                    Logger.LogError("ChatRawEvent Error: " + ex.ToString());
                }
            }

            if (string.IsNullOrEmpty(arg.ArgsStr)) { return; }

            if (quotedMessage.Trim('"').StartsWith("/"))
            {
                string[] args = Facepunch.Utility.String.SplitQuotesStrings(quotedMessage.Trim('"'));
                var command = args[0].TrimStart('/');
                Fougerite.Player player = Fougerite.Server.Cache[arg.argUser.playerClient.userID];
                if (command == "fougerite")
                {
                    player.Message("[color #00FFFF]This Server is running Fougerite V[color yellow]" + Bootstrap.Version);
                    player.Message("[color green]Fougerite Team: www.fougerite.com");
                    player.Message("[color #0C86AE]Pluton Team: www.pluton-team.org");
                }
                var cargs = new string[args.Length - 1];
                Array.Copy(args, 1, cargs, 0, cargs.Length);
                if (OnCommand != null)
                {
                    if (player.CommandCancelList.Contains(command))
                    {
                        player.Message("You cannot execute " + command + " at the moment!");
                        return;
                    }
                    try
                    {
                        OnCommand(player, command, cargs);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("CommandEvent Error: " + ex.ToString());
                    }
                }

            }
            else
            {
                Logger.ChatLog(quotedName, quotedMessage);
                var chatstr = new ChatString(quotedMessage);
                try
                {
                    if (OnChat != null)
                    {
                        OnChat(Fougerite.Server.Cache[arg.argUser.playerClient.userID], ref chatstr);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("ChatEvent Error: " + ex.ToString());
                }
                if (string.IsNullOrEmpty(chatstr.NewText) || chatstr.NewText.Length == 0) { return; }

                string newchat = Facepunch.Utility.String.QuoteSafe(chatstr.NewText.Substring(1, chatstr.NewText.Length - 2)).Replace("\\\"", "" + '\u0022');

                if (string.IsNullOrEmpty(newchat) || newchat.Length == 0) { return; }
                string s = Regex.Replace(newchat, @"\[/?color\b.*?\]", string.Empty);
                if (s.Length <= 100)
                {
                    Fougerite.Data.GetData().chat_history.Add(newchat);
                    Fougerite.Data.GetData().chat_history_username.Add(quotedName);
                    ConsoleNetworker.Broadcast("chat.add " + quotedName + " " + newchat);
                    return;
                }
                string[] ns = Util.GetUtil().SplitInParts(newchat, 100).ToArray();
                var arr = Regex.Matches(newchat, @"\[/?color\b.*?\]")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToArray();
                int i = 0;
                if (arr.Length == 0)
                {
                    arr = new[] { "" };
                }
                foreach (var x in ns)
                {
                    Fougerite.Data.GetData().chat_history.Add(x);
                    Fougerite.Data.GetData().chat_history_username.Add(quotedName);

                    if (i == 1)
                    {
                        ConsoleNetworker.Broadcast("chat.add " + quotedName + " " + '"' + arr[arr.Length - 1] + x);
                    }
                    else
                    {
                        ConsoleNetworker.Broadcast("chat.add " + quotedName + " " + x + '"');
                    }
                    i++;
                }
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ChatEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static bool ConsoleReceived(ref ConsoleSystem.Arg a)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            StringComparison ic = StringComparison.InvariantCultureIgnoreCase;
            bool external = a.argUser == null;
            bool adminRights = (a.argUser != null && a.argUser.admin) || external;

            string userid = "[external][external]";
            if (adminRights && !external)
                userid = string.Format("[{0}][{1}]", a.argUser.displayName, a.argUser.userID.ToString());

            string logmsg = string.Format("[ConsoleReceived] userid={0} adminRights={1} command={2}.{3} args={4}", userid, adminRights.ToString(), a.Class, a.Function, (a.HasArgs(1) ? a.ArgsStr : "none"));
            Logger.LogDebug(logmsg);

            if (a.Class.Equals("fougerite", ic) && a.Function.Equals("reload", ic))
            {
                if (adminRights)
                {
                    if (a.HasArgs(1))
                    {
                        string plugin = a.ArgsStr;
                        foreach (var x in ModuleManager.Modules)
                        {
                            if (string.Equals(x.Plugin.Name, plugin, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (x.Initialized) { x.DeInitialize(); }
                                x.Initialize();
                                a.ReplyWith("Fougerite: Reloaded " + x.Plugin.Name + "!");
                                break;
                            }
                        }
                    }
                    else
                    {
                        ModuleManager.ReloadModules();
                        a.ReplyWith("Fougerite: Reloaded!");
                    }
                }
            }
            else if (a.Class.Equals("fougerite", ic) && a.Function.Equals("unload", ic))
            {
                if (adminRights)
                {
                    if (a.HasArgs(1))
                    {
                        string plugin = a.ArgsStr;
                        foreach (var x in ModuleManager.Modules)
                        {
                            if (string.Equals(x.Plugin.Name, plugin, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (x.Initialized)
                                {
                                    x.DeInitialize();
                                    a.ReplyWith("Fougerite: UnLoaded " + x.Plugin.Name + "!");
                                }
                                else
                                {
                                    a.ReplyWith("Fougerite: " + x.Plugin.Name + " is already unloaded!");
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else if (a.Class.Equals("fougerite", ic) && a.Function.Equals("load", ic))
            {
                if (adminRights)
                {
                    if (a.HasArgs(1))
                    {
                        string plugin = a.ArgsStr;
                        foreach (var x in ModuleManager.Modules)
                        {
                            if (string.Equals(x.Plugin.Name, plugin, StringComparison.CurrentCultureIgnoreCase))
                            {
                                if (!x.Initialized)
                                {
                                    x.Initialize();
                                    a.ReplyWith("Fougerite: Loaded " + x.Plugin.Name + "!");
                                }
                                else
                                {
                                    a.ReplyWith("Fougerite: " + x.Plugin.Name + " is already unloaded!");
                                }
                                break;
                            }
                        }
                    }
                }
            }
            else if (a.Class.Equals("fougerite", ic) && a.Function.Equals("save", ic))
            {
                AvatarSaveProc.SaveAll();
                ServerSaveManager.AutoSave();
                if (Fougerite.Server.GetServer().HasRustPP)
                {
                    Fougerite.Server.GetServer().GetRustPPAPI().RustPPSave();
                }
                a.ReplyWith("Fougerite: Saved!");
            }
            else if (a.Class.Equals("fougerite", ic) && a.Function.Equals("rustpp", ic))
            {
                foreach (var module in Fougerite.ModuleManager.Modules)
                {
                    if (module.Plugin.Name.Equals("RustPPModule"))
                    {
                        module.DeInitialize();
                        module.Initialize();
                        break;
                    }
                }
                a.ReplyWith("Rust++ Reloaded!");
            }
            else if (OnConsoleReceived != null)
            {
                string clss = a.Class.ToLower();
                string func = a.Function.ToLower();
                string data;
                if (!string.IsNullOrEmpty(func))
                {
                    data = clss + "." + func;
                }
                else
                {
                    data = clss;
                }
                if (Server.GetServer().ConsoleCommandCancelList.Contains(data))
                {
                    return false;
                }
                try
                {
                    OnConsoleReceived(ref a, external);
                }
                catch (Exception ex)
                {
                    Logger.LogError("ConsoleReceived Error: " + ex.ToString());
                }
            }

            if (string.IsNullOrEmpty(a.Reply))
            {
                a.ReplyWith(string.Format("Fougerite: {0}.{1} was executed!", a.Class, a.Function));
            }
            if (sw == null) return true;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ConsoleEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return true;
        }

        public static bool CheckOwner(DeployableObject obj, Controllable controllable)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            DoorEvent de = new DoorEvent(new Entity(obj));
            if (obj.ownerID == controllable.playerClient.userID)
            {
                de.Open = true;
            }

            if (!(obj is SleepingBag) && OnDoorUse != null)
            {
                try
                {
                    OnDoorUse(Fougerite.Server.Cache[controllable.playerClient.userID], de);
                }
                catch (Exception ex)
                {
                    Logger.LogError("DoorUseEvent Error: " + ex.ToString());
                }
            }
            if (sw == null) return de.Open;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("CheckOwnerEvent(DoorOpen) Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return de.Open;
        }

        public static float EntityDecay(object entity, float dmg)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            if (entity == null)
                return 0f;

            try
            {
                DecayEvent de = new DecayEvent(new Entity(entity), ref dmg);
                try
                {
                    if (OnEntityDecay != null)
                        OnEntityDecay(de);
                }
                catch (Exception ex)
                {
                    Logger.LogError("EntityDecayEvent Error: " + ex.ToString());
                }

                if (decayList.Contains(entity))
                    decayList.Remove(entity);

                decayList.Add(entity);
                return de.DamageAmount;
            }
            catch { }
            if (sw != null)
            {
                sw.Stop();
                if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("EntityDecayEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            }
            return 0f;
        }

        public static void EntityDeployed(object entity, ref uLink.NetworkMessageInfo info)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            Entity e = new Entity(entity);
            uLink.NetworkPlayer nplayer = info.sender;
            Fougerite.Player creator = e.Creator;
            var data = nplayer.GetLocalData();
            Fougerite.Player ActualPlacer = null;
            NetUser user = data as NetUser;
            if (user != null)
            {
                if (Fougerite.Server.Cache.ContainsKey(user.userID)) ActualPlacer = Fougerite.Server.Cache[user.userID];
            }
            try
            {
                if (OnEntityDeployed != null)
                    OnEntityDeployed(creator, e);
            }
            catch (Exception ex)
            {
                Logger.LogError("EntityDeployedEvent Error: " + ex.ToString());
            }
            try
            {
                if (OnEntityDeployedWithPlacer != null)
                    OnEntityDeployedWithPlacer(creator, e, ActualPlacer);
            }
            catch (Exception ex)
            {
                Logger.LogError("EntityDeployedWithPlacerEvent Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("EntityDeployedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            /*ItemRepresentation rp = new TorchItemRep();
            rp.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
            Server.GetServer().Broadcast(ActualPlacer.ToString());
            if (ActualPlacer != null) { Server.GetServer().Broadcast(ActualPlacer.Name.ToString());}
            ScriptableObject td = ScriptableObject.CreateInstance(typeof (ThrowableItemDataBlock));
            Server.GetServer().Broadcast(td.GetType().ToString());
            ThrowableItemDataBlock td2 = td as ThrowableItemDataBlock;
            Server.GetServer().Broadcast(td2.ToString());
            Vector3 arg = Util.GetUtil().Infront(ActualPlacer, 20f);
            Vector3 position = ActualPlacer.Location + ((Vector3)(ActualPlacer.Location * 1f));
            Quaternion rotation = Quaternion.LookRotation(Vector3.up);
            NetCull.InstantiateDynamicWithArgs<Vector3>(td2.throwObjectPrefab, position, rotation, arg);*/
        }

        public static void EntityHurt2(TakeDamage tkd, ref DamageEvent e)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }

            HurtEvent he = new HurtEvent(ref e);
            he.DamageAmount = e.amount;
            if (he.VictimIsPlayer)
            {
                Player vp = (Player) he.Victim;
                try
                {
                    if (OnPlayerHurt != null)
                    {
                        OnPlayerHurt(he);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("PlayerHurtEvent Error: " + ex);
                }
                if (vp.Health - he.DamageAmount > 0 && e.status == LifeStatus.WasKilled)
                {
                    e.status = LifeStatus.IsAlive;
                }
                switch (e.status)
                {
                    case LifeStatus.IsAlive:
                        e.amount = he.DamageAmount;
                        tkd._health -= he.DamageAmount;
                        break;
                    case LifeStatus.WasKilled:
                        tkd._health = 0f;
                        break;
                }
            }
            else if (he.VictimIsSleeper)
            {
                Sleeper vp = (Sleeper)he.Victim;
                try
                {
                    if (OnPlayerHurt != null)
                    {
                        OnPlayerHurt(he);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("PlayerHurtEvent (Sleeper) Error: " + ex);
                }
                if (vp.Health - he.DamageAmount > 0 && e.status == LifeStatus.WasKilled)
                {
                    e.status = LifeStatus.IsAlive;
                }
                switch (e.status)
                {
                    case LifeStatus.IsAlive:
                        e.amount = he.DamageAmount;
                        tkd._health -= he.DamageAmount;
                        break;
                    case LifeStatus.WasKilled:
                        tkd._health = 0f;
                        break;
                }
            }
            else if (he.VictimIsNPC)
            {
                var victim = he.Victim as NPC;
                if (victim != null && victim.Health > 0f)
                {
                    try
                    {
                        if (OnNPCHurt != null)
                        {
                            OnNPCHurt(he);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("NPCHurtEvent Error: " + ex.ToString());
                    }
                    switch (e.status)
                    {
                        case LifeStatus.IsAlive:
                            tkd._health -= he.DamageAmount;
                            break;
                        case LifeStatus.WasKilled:
                            DeathEvent de = new DeathEvent(ref e);
                            try
                            {
                                if (OnNPCKilled != null)
                                {
                                    OnNPCKilled(de);
                                }
                            }
                            catch (Exception ex) { Logger.LogError("NPCKilledEvent Error: " + ex); }
                            tkd._health = 0f;
                            break;
                    }
                }
            }
            else if (he.VictimIsEntity)
            {
                var ent = he.Entity;
                if (decayList.Contains(he.Entity))
                    he.IsDecay = true;

                try
                {
                    if (OnEntityHurt != null)
                    {
                        OnEntityHurt(he);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError("EntityHurtEvent Error: " + ex);
                }
                if (ent.IsStructure() && !he.IsDecay)
                {
                    StructureComponent component = ent.Object as StructureComponent;
                    if (component != null &&
                        ((component.IsType(StructureComponent.StructureComponentType.Ceiling) ||
                          component.IsType(StructureComponent.StructureComponentType.Foundation)) ||
                         component.IsType(StructureComponent.StructureComponentType.Pillar)))
                    {
                        he.DamageAmount = 0f;
                    }
                }
                if (!tkd.takenodamage)
                {
                    switch (e.status)
                    {
                        case LifeStatus.IsAlive:
                            if (!ent.IsDestroyed)
                            {
                                tkd._health -= he.DamageAmount;
                            }
                            break;
                        case LifeStatus.WasKilled:
                            DestroyEvent de2 = new DestroyEvent(ref e, ent, he.IsDecay);
                            try
                            {
                                if (OnEntityDestroyed != null)
                                {
                                    OnEntityDestroyed(de2);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("EntityDestroyEvent Error: " + ex);
                            }
                            if (!ent.IsDestroyed)
                            {
                                tkd._health = 0f;
                            }
                            break;
                        case LifeStatus.IsDead:
                            DestroyEvent de22 = new DestroyEvent(ref e, ent, he.IsDecay);
                            try
                            {
                                if (OnEntityDestroyed != null)
                                {
                                    OnEntityDestroyed(de22);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogError("EntityDestroyEvent Error: " + ex);
                            }
                            if (!ent.IsDestroyed)
                            {
                                tkd._health = 0f;
                                ent.Destroy();
                            }
                            break;
                    }
                }
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("EntityHurt/Destroy Event Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        /*public static void EntityHurt(object entity, ref DamageEvent e)
        {
            if (entity == null)
                return;
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                var ent = new Entity(entity);
                HurtEvent he = new HurtEvent(ref e, ent);
                if (decayList.Contains(entity))
                    he.IsDecay = true;

                if (ent.IsStructure() && !he.IsDecay)
                {
                    StructureComponent component = entity as StructureComponent;
                    if (component != null &&
                        ((component.IsType(StructureComponent.StructureComponentType.Ceiling) ||
                          component.IsType(StructureComponent.StructureComponentType.Foundation)) ||
                         component.IsType(StructureComponent.StructureComponentType.Pillar)))
                    {
                        he.DamageAmount = 0f;
                    }
                }
                TakeDamage takeDamage = ent.GetTakeDamage();
                takeDamage.health += he.DamageAmount;

                // when entity is destroyed
                if (e.status != LifeStatus.IsAlive)
                {
                    DestroyEvent de = new DestroyEvent(ref e, ent, he.IsDecay);
                    if (OnEntityDestroyed != null)
                        OnEntityDestroyed(de);
                }
                else
                {
                    if (OnEntityHurt != null)
                        OnEntityHurt(he);
                }

                //Zone3D zoned = Zone3D.GlobalContains(ent);
                //if ((zoned == null) || !zoned.Protected)
                //{
                if ((he.Entity.GetTakeDamage().health - he.DamageAmount) <= 0f)
                {
                    he.Entity.Destroy();
                }
                else
                {
                    TakeDamage damage2 = ent.GetTakeDamage();
                    damage2.health -= he.DamageAmount;
                }
                //}
                
            }
            catch (Exception ex) { Logger.LogDebug("EntityHurtEvent Error " + ex); }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("EntityHurtEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }*/

        public static void hijack(string name)
        {
            if ((((name != "!Ng") && (name != ":rabbit_prefab_a")) && ((name != ";res_woodpile") && (name != ";res_ore_1"))) && ((((((((((((((name != ";res_ore_2") & (name != ";res_ore_3")) & (name != ":stag_prefab")) & (name != ":boar_prefab")) & (name != ":chicken_prefab")) & (name != ":bear_prefab")) & (name != ":wolf_prefab")) & (name != ":mutant_bear")) & (name != ":mutant_wolf")) & (name != "AmmoLootBox")) & (name != "MedicalLootBox")) & (name != "BoxLoot")) & (name != "WeaponLootBox")) & (name != "SupplyCrate")))
                Logger.LogDebug("Hijack: " + name);
        }

        public static ItemDataBlock[] ItemsLoaded(System.Collections.Generic.List<ItemDataBlock> items, Dictionary<string, int> stringDB, Dictionary<int, int> idDB)
        {
            ItemsBlocks blocks = new ItemsBlocks(items);
            try
            {
                if (OnItemsLoaded != null)
                    OnItemsLoaded(blocks);
            }
            catch (Exception ex)
            {
                Logger.LogError("DataBlockLoadEvent Error: " + ex.ToString());
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

        public static void ItemPickup(Controllable controllable, IInventoryItem item, Inventory local, Inventory.AddExistingItemResult result)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            ItemPickupEvent ipe = new ItemPickupEvent(controllable, item, local, result);
            try
            {
                if (OnItemPickup != null)
                {
                    OnItemPickup(ipe);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ItemPickupEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ItemPickupEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void FallDamage(FallDamage fd, float speed, float num, bool flag, bool flag2)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            FallDamageEvent fde = new FallDamageEvent(fd, speed, num, flag, flag2);
            try
            {
                if (OnFallDamage != null)
                {
                    OnFallDamage(fde);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("FallDamageEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("FallDamageEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void NPCHurt(ref DamageEvent e)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            HurtEvent he = new HurtEvent(ref e);
            var npc = he.Victim as NPC;
            if (npc != null && npc.Health > 0f)
            {
                NPC victim = he.Victim as NPC;
                victim.Health += he.DamageAmount;
                try
                {
                    if (OnNPCHurt != null)
                        OnNPCHurt(he);
                }
                catch (Exception ex)
                {
                    Logger.LogError("NPCHurtEvent Error: " + ex.ToString());
                }
                if (((he.Victim as NPC).Health - he.DamageAmount) <= 0f)
                    (he.Victim as NPC).Kill();
                else
                {
                    NPC npc2 = he.Victim as NPC;
                    npc2.Health -= he.DamageAmount;
                }
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("NPCHurtEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void NPCKilled(ref DamageEvent e)
        {
            /*Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                DeathEvent de = new DeathEvent(ref e);
                if (OnNPCKilled != null)
                    OnNPCKilled(de);
            }
            catch (Exception ex) { Logger.LogError("NPCKilledEvent Error: " + ex); }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("NPCKilledEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");*/
        }

        public static void ConnectHandler(NetUser user)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            GameEvent.DoPlayerConnected(user.playerClient);
            PlayerConnect(user);
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ConnectHandler Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static bool PlayerConnect(NetUser user)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            bool connected = false;

            if (user.playerClient == null)
            {
                Logger.LogDebug("PlayerConnect user.playerClient is null");
                return connected;
            }
            ulong uid = user.userID;
            if (uLinkDCCache.Contains(uid))
            {
                uLinkDCCache.Remove(uid);
            }
            Fougerite.Server srv = Fougerite.Server.GetServer();
            Fougerite.Player player = new Fougerite.Player(user.playerClient);
            if (!Fougerite.Server.Cache.ContainsKey(uid))
            {
                Fougerite.Server.Cache.Add(uid, player);
            }
            else
            {
                Fougerite.Server.Cache[uid] = player;
            }

            if (srv.ContainsPlayer(uid))
            {
                Logger.LogError(string.Format("[PlayerConnect] Server.Players already contains {0} {1}", player.Name, player.SteamID));
                connected = user.connected;
                return connected;
            }
            srv.AddPlayer(uid, player);
            //server.Players.Add(player);
            //Rust.Steam.Server.Steam_UpdateServer(server.maxplayers, srv.Players.Count, server.hostname, server.map, "rust,modded,fougerite");

            try
            {
                if (OnPlayerConnected != null)
                {
                    OnPlayerConnected(player);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerConnectedEvent Error " + ex.ToString());
                return connected;
            }

            connected = user.connected;

            if (Fougerite.Config.GetBoolValue("Fougerite", "tellversion"))
            {
                player.Message(string.Format("This server is powered by Fougerite v.{0}!", Bootstrap.Version));
            }
            Logger.LogDebug("User Connected: " + player.Name + " (" + player.SteamID + ")" + " (" + player.IP + ")");
            if (sw == null) return connected;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PlayerConnectedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return connected;
        }

        public static void PlayerDisconnect(uLink.NetworkPlayer nplayer)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            NetUser user = nplayer.GetLocalData() as NetUser;
            if (user == null)
            {
                return;
            }
            ulong uid = user.userID;
            Fougerite.Player player = null;
            if (Fougerite.Server.Cache.ContainsKey(uid))
            {
                player = Fougerite.Server.Cache[uid];
            }
            else
            {
                Fougerite.Server.GetServer().RemovePlayer(uid);
                Logger.LogWarning("[WeirdDisconnect] Player was null at the disconnection. Something might be wrong? OPT: " + Fougerite.Bootstrap.CR);
                return;
            }
            player.IsDisconnecting = true;
            Fougerite.Server.GetServer().RemovePlayer(uid);
            //if (Fougerite.Server.GetServer().Players.Contains(player)) { Fougerite.Server.GetServer().Players.Remove(player); }
            //player.PlayerClient.netUser.Dispose();
            Fougerite.Server.Cache[uid] = player;
            //Rust.Steam.Server.Steam_UpdateServer(server.maxplayers, Fougerite.Server.GetServer().Players.Count, server.hostname, server.map, "modded, fougerite");
            try
            {
                if (OnPlayerDisconnected != null)
                {
                    OnPlayerDisconnected(player);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerDisconnectedEvent Error " + ex.ToString());
            }
            Logger.LogDebug("User Disconnected: " + player.Name + " (" + player.SteamID + ")" + " (" + player.IP + ")");
            if (Fougerite.Bootstrap.CR && Fougerite.Server.Cache.ContainsKey(uid)) { Fougerite.Server.Cache.Remove(uid); }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PlayerDisconnectEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerGather(Inventory rec, ResourceTarget rt, ResourceGivePair rg, ref int amount)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, rg, amount);
            try
            {
                if (OnPlayerGathering != null)
                {
                    OnPlayerGathering(player, ge);
                }
                amount = ge.Quantity;
                if (!ge.Override)
                {
                    amount = Mathf.Min(amount, rg.AmountLeft());
                }
                rg.ResourceItemName = ge.Item;
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerGatherEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("GatherEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerGatherWood(IMeleeWeaponItem rec, ResourceTarget rt, ref ItemDataBlock db, ref int amount, ref string name)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            Fougerite.Player player = Fougerite.Player.FindByNetworkPlayer(rec.inventory.networkView.owner);
            GatherEvent ge = new GatherEvent(rt, db, amount);
            ge.Item = "Wood";
            try
            {
                if (OnPlayerGathering != null)
                {
                    OnPlayerGathering(player, ge);
                }
                db = Fougerite.Server.GetServer().Items.Find(ge.Item);
                amount = ge.Quantity;
                name = ge.Item;
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerGatherWoodEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("WoodGatherEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerHurt(ref DamageEvent e)
        {
            /*Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                HurtEvent he = new HurtEvent(ref e);
                if (OnPlayerHurt != null)
                {
                    OnPlayerHurt(he);
                }
                if (!(he.Attacker is NPC) && !(he.Victim is NPC))
                {
                    Fougerite.Player attacker = he.Attacker as Fougerite.Player;
                    Fougerite.Player victim = he.Victim as Fougerite.Player;
                    Zone3D zoned = Zone3D.GlobalContains(attacker);
                    if ((zoned != null) && !zoned.PVP)
                    {
                        if (attacker != null) { attacker.Message("You are in a PVP restricted area."); }
                        he.DamageAmount = 0f;
                        e = he.DamageEvent;
                        return;
                    }
                    zoned = Zone3D.GlobalContains(victim);
                    if ((zoned != null) && !zoned.PVP)
                    {
                        if (attacker != null && victim != null) { attacker.Message(victim.Name + " is in a PVP restricted area."); }
                        he.DamageAmount = 0f;
                        e = he.DamageEvent;
                        return;
                    }
                }
                e = he.DamageEvent;
            }
            catch (Exception ex) { Logger.LogError("PlayerHurtEvent Error: " + ex); }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PlayerHurtEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");*/
        }

        public static bool PlayerKilled(ref DamageEvent de)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            bool flag = false;
            try
            {
                DeathEvent event2 = new DeathEvent(ref de);
                flag = event2.DropItems;
                if (OnPlayerKilled != null)
                    OnPlayerKilled(event2);

                flag = event2.DropItems;
            }
            catch (Exception ex) { Logger.LogError("PlayerKilledEvent Error: " + ex); }
            if (sw == null) return flag;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PlayerKilledEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return flag;
        }

        public static void PlayerSpawned(PlayerClient pc, Vector3 pos, bool camp)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            Fougerite.Player player = Fougerite.Server.Cache[pc.userID];
            SpawnEvent se = new SpawnEvent(pos, camp);
            try
            {
                //Fougerite.Player player = Fougerite.Player.FindByPlayerClient(pc);
                if (OnPlayerSpawned != null && player != null)
                {
                    OnPlayerSpawned(player, se);
                }
            }
            catch (Exception ex) { Logger.LogError("PlayerSpawnedEvent Error: " + ex); }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PlayerSpawned Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static Vector3 PlayerSpawning(PlayerClient pc, Vector3 pos, bool camp)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            Fougerite.Player player = Fougerite.Server.Cache[pc.userID];
            SpawnEvent se = new SpawnEvent(pos, camp);
            try
            {
                if (OnPlayerSpawning != null && player != null)
                {
                    OnPlayerSpawning(player, se);
                }
                return new Vector3(se.X, se.Y, se.Z);
            }
            catch (Exception ex) { Logger.LogError("PlayerSpawningEvent Error: " + ex); }
            if (sw == null) return pos;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PlayerSpawningEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
            return pos;
        }

        public static void PluginInit()
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnPluginInit != null)
                {
                    OnPluginInit();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PluginInitEvent Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PluginInit Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerTeleport(Fougerite.Player player, Vector3 from, Vector3 dest)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnPlayerTeleport != null)
                {
                    OnPlayerTeleport(player, from, dest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("TeleportEvent Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("TeleportEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void RecieveNetwork(Metabolism m, float cal, float water, float rad, float anti, float temp, float poison)
        {
            bool h = false;
            Fougerite.Player p = null;
            if (m.playerClient != null)
            {
                p = Fougerite.Server.Cache[m.playerClient.userID];
            }
            if (float.IsNaN(cal) || cal > 3000)
            {
                m.caloricLevel = 600;
                if (p != null)
                {
                    Logger.LogWarning("[CalorieHack] " + p.Name + " | " + p.SteamID + " is using calorie hacks! =)");
                    Fougerite.Server.GetServer().Broadcast("CalorieHack Detected: " + p.Name);
                    Fougerite.Server.GetServer().BanPlayer(p, "Console", "CalorieHack");
                    h = true;
                }
            }
            else
            {
                m.caloricLevel = cal;
            }
            if (rad > 3000)
            {
                m.radiationLevel = 0;
                h = true;
                if (p != null)
                {
                    Logger.LogDebug("[RadiationHack] Someone tried to kill " + p.Name + " with radiation hacks.");
                }
            }
            else if (float.IsNaN(rad))
            {
                m.radiationLevel = 0;
                h = true;
                if (p != null)
                {
                    Logger.LogWarning("[RadiationHack] " + p.Name + " using radiation hacks.");
                    Fougerite.Server.GetServer().Broadcast("RadiationHack Detected: " + p.Name);
                    Fougerite.Server.GetServer().BanPlayer(p, "Console", "RadiationHack");
                }
            }
            else
            {
                m.radiationLevel = rad;
            }
            if (float.IsNaN(poison) || poison > 5000)
            {
                m.poisonLevel = 0;
                h = true;
            }
            else
            {
                m.poisonLevel = poison;
            }
            if (float.IsNaN(water) || water > 5000)
            {
                m.waterLevelLitre = 0;
                h = true;
            }
            else
            {
                m.waterLevelLitre = water;
            }
            if (float.IsNaN(anti) || anti > 3000)
            {
                m.antiRads = 0;
                h = true;
            }
            else
            {
                m.antiRads = anti;
            }
            if (float.IsNaN(temp) || temp > 5000)
            {
                m.coreTemperature = 0;
                h = true;
            }
            else
            {
                m.coreTemperature = temp;
            }
            if ((double)m.coreTemperature >= 1.0) { m._lastWarmTime = Time.time; }
            else if ((double)m.coreTemperature < 0.0) { m._lastWarmTime = -1000f; }

            try
            {
                if (OnRecieveNetwork != null)
                {
                    OnRecieveNetwork(p, m, cal, water, rad, anti, temp, poison);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("RecieveNetworkEvent Error: " + ex.ToString());
            }
            if (!h) { RPOS.MetabolismUpdate(); }
        }

        public static void CraftingEvent(CraftingInventory inv, BlueprintDataBlock blueprint, int amount, ulong startTime)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                CraftingEvent e = new CraftingEvent(inv, blueprint, amount, startTime);
                if (OnCrafting != null)
                {
                    OnCrafting(e);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("CraftingEvent Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("CraftEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void AnimalMovement(BaseAIMovement m, BasicWildLifeAI ai, ulong simMillis)
        {
            var movement = m as NavMeshMovement;
            if (!movement) { return; }

            if (movement._agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                bool IsAlive = ai.GetComponent<TakeDamage>().alive;
                if (IsAlive)
                {
                    TakeDamage.KillSelf(ai.GetComponent<IDBase>());
                    Logger.LogWarning("[NavMesh] AI destroyed for having invalid path.");
                }
            }
        }

        public static void ResourceSpawned(ResourceTarget target)
        {
            try
            {
                if (OnResourceSpawned != null)
                {
                    OnResourceSpawned(target);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ResourceSpawnedEvent Error: " + ex);
            }
        }

        public static void ShootEvent(BulletWeaponDataBlock db, GameObject obj2, ItemRepresentation rep, ref uLink.NetworkMessageInfo info, IBulletWeaponItem bwi)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnShoot != null)
                {
                    ShootEvent se = new ShootEvent(db, obj2, rep, info, bwi);
                    OnShoot(se);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ShootEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ShootEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void BowShootEvent(BowWeaponDataBlock db, ItemRepresentation rep, ref uLink.NetworkMessageInfo info, IBowWeaponItem bwi)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnBowShoot != null)
                {
                    BowShootEvent se = new BowShootEvent(db, rep, info, bwi);
                    OnBowShoot(se);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("BowShootEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("BowShootEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void ShotgunShootEvent(ShotgunDataBlock shotgunDataBlock, uLink.BitStream stream, ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            NetCull.VerifyRPC(ref info, false);
            IBulletWeaponItem found = null;
            if (rep.Item(out found) && (found.uses > 0))
            {
                TakeDamage local = found.inventory.GetLocal<TakeDamage>();
                if (((local == null) || !local.dead) && found.ValidatePrimaryMessageTime(info.timestamp))
                {
                    try
                    {
                        if (OnShotgunShoot != null)
                        {
                            ShotgunShootEvent se = new ShotgunShootEvent(shotgunDataBlock, rep, info, found);
                            OnShotgunShoot(se);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("ShotgunShootEvent Error: " + ex);
                    }
                    int count = 1;
                    found.Consume(ref count);
                    found.itemRepresentation.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                    //float bulletRange = shotgunDataBlock.GetBulletRange(rep);
                    for (int i = 0; i < shotgunDataBlock.numPellets; i++)
                    {
                        GameObject obj2;
                        NetEntityID yid;
                        IDRemoteBodyPart part;
                        bool flag;
                        bool flag2;
                        bool flag3;
                        BodyPart part2;
                        Vector3 vector;
                        Vector3 vector2;
                        Transform transform;
                        shotgunDataBlock.ReadHitInfo(stream, out obj2, out flag, out flag2, out part2, out part, out yid, out transform, out vector, out vector2, out flag3);
                        if (obj2 != null)
                        {
                            shotgunDataBlock.ApplyDamage(obj2, transform, flag3, vector, part2, rep);
                        }
                    }
                    found.TryConditionLoss(0.5f, 0.02f);
                }
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ShotgunShootEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void GrenadeEvent(HandGrenadeDataBlock hgd, uLink.BitStream stream, ItemRepresentation rep, ref uLink.NetworkMessageInfo info)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            IHandGrenadeItem item;
            NetCull.VerifyRPC(ref info, false);
            if (rep.Item<IHandGrenadeItem>(out item) && item.ValidatePrimaryMessageTime(info.timestamp))
            {
                rep.ActionStream(1, uLink.RPCMode.AllExceptOwner, stream);
                Vector3 origin = stream.ReadVector3();
                Vector3 forward = stream.ReadVector3();
                GameObject obj2 = hgd.ThrowItem(rep, origin, forward);
                if (obj2 != null)
                {
                    obj2.rigidbody.AddTorque((Vector3)(new Vector3(UnityEngine.Random.Range((float)-1f, (float)1f), UnityEngine.Random.Range((float)-1f, (float)1f), UnityEngine.Random.Range((float)-1f, (float)1f)) * 10f));
                    try
                    {
                        if (OnGrenadeThrow != null)
                        {
                            GrenadeThrowEvent se = new GrenadeThrowEvent(hgd, obj2, rep, info, item);
                            OnGrenadeThrow(se);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("GrenadeThrowEvent Error: " + ex);
                    }
                }
                int count = 1;
                if (item.Consume(ref count))
                {
                    item.inventory.RemoveItem(item.slot);
                }
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("GrenadeEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static bool ServerSaved()
        {
            if (ServerSaveManager._loading)
            {
                return false;
            }
            DataStore.GetInstance().Save();
            string path = ServerSaveManager.autoSavePath;
            try
            {
                if (OnServerSaved != null)
                {
                    OnServerSaved();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ServerSavedEvent Error: " + ex);
            }
            if (IsShuttingDown)
            {
                SaveAll(path);
                return true;
            }
            if (Bootstrap.TS)
            {
                Loom.ExecuteInBiggerStackThread(() => {
                    SaveAll(path);
                });
            }
            else
            {
                SaveAll(path);
            }
            return true;
        }

        internal static void SaveAll(string path)
        {
            SystemTimestamp restart = SystemTimestamp.Restart;
            if (path == string.Empty)
            {
                path = "savedgame.sav";
            }
            if (!path.EndsWith(".sav"))
            {
                path = path + ".sav";
            }
            if (ServerSaveManager._loading)
            {
                Debug.LogError("Currently loading, aborting save to " + path);
            }
            else
            {
                SystemTimestamp timestamp2;
                SystemTimestamp timestamp3;
                SystemTimestamp timestamp4;
                WorldSave fsave;
                Debug.Log("Saving to '" + path + "'");
                if (!ServerSaveManager._loadedOnce)
                {
                    if (File.Exists(path))
                    {
                        string[] textArray1 = new string[] { path, ".", ServerSaveManager.DateTimeFileString(File.GetLastWriteTime(path)), ".", ServerSaveManager.DateTimeFileString(DateTime.Now), ".bak" };
                        string destFileName = string.Concat(textArray1);
                        File.Copy(path, destFileName);
                        Logger.LogError("A save file exists at target path, but it was never loaded!\n\tbacked up:" + Path.GetFullPath(destFileName));
                    }
                    ServerSaveManager._loadedOnce = true;
                }
                using (Recycler<WorldSave, WorldSave.Builder> recycler = WorldSave.Recycler())
                {
                    WorldSave.Builder builder = recycler.OpenBuilder();
                    timestamp2 = SystemTimestamp.Restart;
                    ServerSaveManager.Get(false).DoSave(ref builder);
                    timestamp2.Stop();
                    timestamp3 = SystemTimestamp.Restart;
                    fsave = builder.Build();
                    timestamp3.Stop();
                }
                int num = fsave.SceneObjectCount + fsave.InstanceObjectCount;
                if (save.friendly)
                {
                    using (FileStream stream = File.Open(path + ".json", FileMode.Create, FileAccess.Write))
                    {
                        JsonFormatWriter writer = JsonFormatWriter.CreateInstance(stream);
                        writer.Formatted();
                        writer.WriteMessage(fsave);
                    }
                }
                SystemTimestamp timestamp5 = timestamp4 = SystemTimestamp.Restart;
                using (FileStream stream2 = File.Open(path + ".new", FileMode.Create, FileAccess.Write))
                {
                    fsave.WriteTo(stream2);
                    stream2.Flush();
                }
                timestamp4.Stop();
                if (File.Exists(path + ".old.5"))
                {
                    File.Delete(path + ".old.5");
                }
                for (int i = 4; i >= 0; i--)
                {
                    if (File.Exists(path + ".old." + i))
                    {
                        File.Move(path + ".old." + i, path + ".old." + (i + 1));
                    }
                }
                if (File.Exists(path))
                {
                    File.Move(path, path + ".old.0");
                }
                if (File.Exists(path + ".new"))
                {
                    File.Move(path + ".new", path);
                }
                timestamp5.Stop();
                restart.Stop();
                if (save.profile)
                {
                    object[] args = new object[] { num, timestamp2.ElapsedSeconds, timestamp2.ElapsedSeconds / restart.ElapsedSeconds, timestamp3.ElapsedSeconds, timestamp3.ElapsedSeconds / restart.ElapsedSeconds, timestamp4.ElapsedSeconds, timestamp4.ElapsedSeconds / restart.ElapsedSeconds, timestamp5.ElapsedSeconds, timestamp5.ElapsedSeconds / restart.ElapsedSeconds, restart.ElapsedSeconds, restart.ElapsedSeconds / restart.ElapsedSeconds };
                    Logger.Log(string.Format(" Saved {0} Object(s) [times below are in elapsed seconds]\r\n  Logic:\t{1,-16:0.000000}\t{2,7:0.00%}\r\n  Build:\t{3,-16:0.000000}\t{4,7:0.00%}\r\n  Stream:\t{5,-16:0.000000}\t{6,7:0.00%}\r\n  All IO:\t{7,-16:0.000000}\t{8,7:0.00%}\r\n  Total:\t{9,-16:0.000000}\t{10,7:0.00%}", args));
                }
                else
                {
                    Logger.Log(string.Concat(new object[] { " Saved ", num, " Object(s). Took ", restart.ElapsedSeconds, " seconds." }));
                }
            }
        }

        public static void ItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                foreach (var x in RPOS.AllWindows)
                {
                    Server.GetServer().Broadcast(x.name);
                }
                //RPOS.Get().
                if (OnItemRemoved != null)
                {
                    Fougerite.Events.InventoryModEvent e = new Fougerite.Events.InventoryModEvent(inventory, slot, item, "Remove");
                    OnItemRemoved(e);
                }
            }
            catch
            {
                //Logger.LogError("InventoryRemoveEvent error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ItemRemoved Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void ItemAdded(Inventory inventory, int slot, IInventoryItem item)
        {
            try
            {
                if (OnItemAdded != null)
                {
                    Fougerite.Events.InventoryModEvent e = new Fougerite.Events.InventoryModEvent(inventory, slot, item, "Add");
                    OnItemAdded(e);
                }
            }
            catch
            {
                //Logger.LogError("InventoryAddEvent error: " + ex);
            }
        }

        public static void Airdrop(Vector3 v)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnAirdropCalled != null)
                {
                    OnAirdropCalled(v);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AirdropEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("Airdrop Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void Airdrop2(SupplyDropZone srz)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnAirdropCalled != null)
                {
                    OnAirdropCalled(srz.GetSupplyTargetPosition());
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AirdropEvent Error: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("Airdrop2 Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        /*public static void AirdropCrateDropped(GameObject go)
        {
            try
            {
                if (OnAirdropCrateDropped != null)
                {
                    OnAirdropCrateDropped(go);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("AirdropCrateDroppedEvent Error: " + ex);
            }
        }*/

        public static void SteamDeny(ClientConnection cc, NetworkPlayerApproval approval, string strReason, NetError errornum)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            SteamDenyEvent sde = new SteamDenyEvent(cc, approval, strReason, errornum);
            try
            {
                if (OnSteamDeny != null)
                {
                    OnSteamDeny(sde);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("SteamDenyEvent Error: " + ex);
            }
            if (sde.ForceAllow)
            {
                if (sw != null)
                {
                    sw.Stop();
                    if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("Airdrop Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
                }
                return;
            }
            string deny = "Auth failed: " + strReason + " - " + cc.UserName + " (" +
                       cc.UserID.ToString() +
                       ")";
            ConsoleSystem.Print(deny, false);
            approval.Deny((uLink.NetworkConnectionError)errornum);
            ConnectionAcceptor.CloseConnection(cc);
            Rust.Steam.Server.OnUserLeave(cc.UserID);
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("SteamDeny Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void HandleuLinkDisconnect(string msg, object NetworkPlayer)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                UnityEngine.Object[] obj = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
                GameObject[] objArray = null;
                if (obj is GameObject[])
                {
                    objArray = obj as GameObject[];
                }
                else
                {
                    Logger.LogWarning("[uLink Failure] Array was not GameObject?!");
                }
                if (objArray == null)
                {
                    Logger.LogWarning("[uLink Failure] Something bad happened during the disconnection... Report this.");
                    return;
                }
                if (NetworkPlayer is uLink.NetworkPlayer)
                {
                    uLink.NetworkPlayer np = (uLink.NetworkPlayer)NetworkPlayer;
                    var data = np.GetLocalData();
                    NetUser user = data as NetUser;
                    if (user != null)
                    {
                        ulong id = user.userID;
                        var client = user.playerClient;
                        var loc = user.playerClient.lastKnownPosition;
                        Fougerite.Server.Cache[id].IsDisconnecting = true;
                        Fougerite.Server.Cache[id].DisconnectLocation = loc;
                        Fougerite.Server.Cache[id].UpdatePlayerClient(client);
                        var srv = Fougerite.Server.GetServer();
                        if (srv.DPlayers.ContainsKey(id))
                        {
                            srv.DPlayers[id].IsDisconnecting = true;
                            srv.DPlayers[id].DisconnectLocation = loc;
                            srv.DPlayers[id].UpdatePlayerClient(client);
                        }
                    }
                }
                //new Thread(() =>
                //{
                //Thread.CurrentThread.IsBackground = true;
                foreach (GameObject obj2 in objArray)
                {
                    //Logger.LogWarning(obj2.name);
                    try
                    {
                        if (obj2 != null)
                        {
                            obj2.SendMessage(msg, NetworkPlayer, SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("[uLink Error] Disconnect failure, report to DreTaX: " + ex);
                    }
                }
                //}).Start();
            }
            catch //(Exception ex)
            {
                //Logger.LogError("[uLink Error] Full Exception: " + ex);
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("HandleuLinkDisconnect Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void PlayerApproval(ConnectionAcceptor ca, NetworkPlayerApproval approval)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            if (ca.m_Connections.Count >= server.maxplayers)
            {
                approval.Deny(uLink.NetworkConnectionError.TooManyConnectedPlayers);
            }
            else
            {
                ClientConnection clientConnection = new ClientConnection();
                if (!clientConnection.ReadConnectionData(approval.loginData))
                {
                    approval.Deny(uLink.NetworkConnectionError.IncorrectParameters);
                    return;
                }
                Fougerite.Server srv = Fougerite.Server.GetServer();
                ulong uid = clientConnection.UserID;
                string ip = approval.ipAddress;
                string name = clientConnection.UserName;
                if (clientConnection.Protocol != 1069)
                {
                    Debug.Log((object)("Denying entry to client with invalid protocol version (" + ip + ")"));
                    approval.Deny(uLink.NetworkConnectionError.IncompatibleVersions);
                }
                else if (BanList.Contains(uid))
                {
                    Debug.Log((object)("Rejecting client (" + uid.ToString() + "in banlist)"));
                    approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                }
                else if (srv.IsBannedID(uid.ToString()) || srv.IsBannedIP(ip))
                {
                    if (!srv.IsBannedIP(ip))
                    {
                        srv.BanPlayerIP(ip, name, "IP is not banned-" + uid.ToString(), "Console");
                        Logger.LogDebug("[FougeriteBan] Detected banned ID, but IP is not banned: "
                                        + name + " - " + ip + " - " + uid);
                    }
                    else
                    {
                        if (DataStore.GetInstance().Get("Ips", ip).ToString() != name)
                        {
                            DataStore.GetInstance().Add("Ips", ip, name);
                        }
                    }
                    if (!srv.IsBannedID(uid.ToString()))
                    {
                        srv.BanPlayerID(uid.ToString(), name, "ID is not banned-" + ip, "Console");
                        Logger.LogDebug("[FougeriteBan] Detected banned IP, but ID is not banned: "
                            + name + " - " + ip + " - " + uid);
                    }
                    else
                    {
                        if (DataStore.GetInstance().Get("Ids", uid.ToString()).ToString() != name)
                        {
                            DataStore.GetInstance().Add("Ids", uid.ToString(), name);
                        }
                    }
                    Logger.LogWarning("[FougeriteBan] Disconnected: " + name
                        + " - " + ip + " - " + uid);
                    approval.Deny(uLink.NetworkConnectionError.ConnectionBanned);
                }
                else if (ca.IsConnected(clientConnection.UserID))
                {
                    PlayerApprovalEvent ape = new PlayerApprovalEvent(ca, approval, clientConnection, true);
                    try
                    {
                        if (OnPlayerApproval != null)
                        {
                            OnPlayerApproval(ape);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("PlayerApprovalEvent Error: " + ex);
                    }
                    if (ape.ForceAccept)
                    {
                        if (Fougerite.Server.Cache.ContainsKey(clientConnection.UserID) && !ape.ServerHasPlayer)
                        {
                            Fougerite.Server.Cache[clientConnection.UserID].Disconnect();
                        }
                        Accept(ca, approval, clientConnection);
                        return;
                    }
                    Debug.Log((object)("Denying entry to " + uid.ToString() + " because they're already connected"));
                    approval.Deny(uLink.NetworkConnectionError.AlreadyConnectedToAnotherServer);
                }
                else
                {
                    PlayerApprovalEvent ape = new PlayerApprovalEvent(ca, approval, clientConnection, false);
                    if (OnPlayerApproval != null) { OnPlayerApproval(ape); }
                    Accept(ca, approval, clientConnection);
                }
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("PlayerApprovalEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        private static void Accept(ConnectionAcceptor ca, NetworkPlayerApproval approval, ClientConnection clientConnection)
        {
            ca.m_Connections.Add(clientConnection);
            ca.StartCoroutine(clientConnection.AuthorisationRoutine(approval));
            approval.Wait();
        }

        public static void ClientMove(HumanController hc, Vector3 origin, int encoded, ushort stateFlags, uLink.NetworkMessageInfo info)
        {
            if (info.sender != hc.networkView.owner)
            {
                return;
            }
            if (float.IsNaN(origin.x) || float.IsInfinity(origin.x) ||
                float.IsNaN(origin.y) || float.IsInfinity(origin.y) ||
                float.IsNaN(origin.z) || float.IsInfinity(origin.z))
            {
                Fougerite.Player player = Fougerite.Server.Cache.ContainsKey(hc.netUser.userID) ? Fougerite.Server.Cache[hc.netUser.userID]
                    : Fougerite.Server.GetServer().FindPlayer(hc.netUser.userID.ToString());
                if (player == null)
                {
                    // Should never happen but just to be sure.
                    if (hc.netUser == null) return;
                    if (hc.netUser.connected)
                    {
                        hc.netUser.Kick(NetError.NoError, true);
                    }
                    return;
                }
                Logger.LogWarning("[TeleportHack] " + player.Name + " sent invalid packets. " + hc.netUser.userID);
                Server.GetServer().Broadcast(player.Name + " might have tried to teleport with hacks.");
                if (Fougerite.Bootstrap.BI)
                {
                    Fougerite.Server.GetServer().BanPlayer(player, "Console", "TeleportHack");
                    return;
                }
                player.Disconnect();
                return;
            }
            var data = stateFlags = (ushort)(stateFlags & -24577);
            Util.PlayerActions action = ((Util.PlayerActions)data);
            try
            {
                if (OnPlayerMove != null)
                {
                    OnPlayerMove(hc, origin, encoded, stateFlags, info, action);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PlayerMoveEvent Error: " + ex);
            }
        }

        public static void ResearchItem(IInventoryItem otherItem)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnResearch != null)
                {
                    ResearchEvent researchEvent = new ResearchEvent(otherItem);
                    OnResearch(researchEvent);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ResearchItem Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ResearchItem Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void SetLooter(LootableObject lo, uLink.NetworkPlayer ply)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            lo.occupierText = null;
            if (ply == uLink.NetworkPlayer.unassigned)
            {
                lo.ClearLooter();
            }
            else
            {
                if (ply == NetCull.player)
                {
                    if (!lo.thisClientIsInWindow)
                    {
                        try
                        {
                            lo._currentlyUsingPlayer = ply;
                            RPOS.OpenLootWindow(lo);
                            lo.thisClientIsInWindow = true;
                        }
                        catch (Exception exception)
                        {
                            Debug.LogError(exception, lo);
                            NetCull.RPC((UnityEngine.MonoBehaviour)lo, "StopLooting", uLink.RPCMode.Server);
                            lo.thisClientIsInWindow = false;
                            ply = uLink.NetworkPlayer.unassigned;
                        }
                    }
                }
                else if ((lo._currentlyUsingPlayer == NetCull.player) && (NetCull.player != uLink.NetworkPlayer.unassigned))
                {
                    lo.ClearLooter();
                }
                lo._currentlyUsingPlayer = ply;
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("SetLooterEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void OnUseEnter(LootableObject lo, Useable use)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            var ulinkuser = uLink.NetworkView.Get((UnityEngine.MonoBehaviour)use.user).owner;
            NetUser user = ulinkuser.GetLocalData() as NetUser;
            lo._useable = use;
            lo._currentlyUsingPlayer = ulinkuser;
            lo._inventory.AddNetListener(lo._currentlyUsingPlayer);
            lo.SendCurrentLooter();
            lo.CancelInvokes();
            lo.InvokeRepeating("RadialCheck", 0f, 10f);
            if (user != null)
            {
                if (Fougerite.Server.Cache.ContainsKey(user.userID))
                {
                    Fougerite.Player pl = Fougerite.Server.Cache[user.userID];
                    LootStartEvent lt = new LootStartEvent(lo, pl, use, ulinkuser);
                    try
                    {
                        if (OnLootUse != null)
                        {
                            OnLootUse(lt);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError("LootStartEvent Error: " + ex);
                    }

                    /*if (lt.IsCancelled)
                    {
                        return;
                    }*/
                }
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ChestEnterEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void RPCFix(Class48 c48, Class5 class5_0, uLink.NetworkPlayer networkPlayer_1)
        {
            Class56 class2 = c48.method_270(networkPlayer_1);
            if (class2 != null)
            {
                c48.method_277(class5_0, class2);
            }
            else
            {
                if (IsShuttingDown) { return; }
                NetUser user = networkPlayer_1.GetLocalData() as NetUser;
                if (user != null)
                {
                    ulong id = user.userID;
                    if (uLinkDCCache.Contains(id)){return;}
                    Logger.LogDebug("===Fougerite uLink===");
                    if (Fougerite.Server.Cache.ContainsKey(id))
                    {
                        Fougerite.Player player = Fougerite.Server.Cache[id];
                        if (player != null)
                        {
                            Logger.LogDebug("[Fougerite uLink] Detected RPC Failing Player: " + player.Name + "-" +
                                              player.SteamID + " Trying to kick...");
                            if (player.IsOnline)
                            {
                                player.Disconnect(false);
                                Logger.LogDebug("[Fougerite uLink] Should be kicked!");
                                return; // Return to avoid the RPC Logging
                            }
                            Logger.LogDebug("[Fougerite uLink] Server says It's offline. Not touching.");
                            uLinkDCCache.Add(player.UID);
                        }
                    }
                    else
                    {
                        Logger.LogDebug("[Fougerite uLink] Not existing in cache...");
                        uLinkDCCache.Add(id);
                    }
                }
                else
                {
                    Logger.LogDebug("===Fougerite uLink===");
                    Logger.LogDebug("[Fougerite uLink] Not existing in cache... (2x0)");
                }
                Logger.LogDebug("[Fougerite uLink] Private RPC (internal RPC " + class5_0.enum0_0 + ")" + " was not sent because a connection to " + class5_0.networkPlayer_1 + " was not found!");
                //NetworkLog.Error<string, string, uLink.NetworkPlayer, string>(NetworkLogFlags.BadMessage | NetworkLogFlags.RPC, "Private RPC ", (class5_0.method_11() ? class5_0.string_0 : ("(internal RPC " + class5_0.enum0_0 + ")")) + " was not sent because a connection to ", class5_0.networkPlayer_1, " was not found!");
            }
        }

        public static void RPCCatch(object obj)
        {
            var info = obj as uLink.NetworkMessageInfo;
            if (info == null) { return; }
            if (info.sender == uLink.NetworkPlayer.server) { return; }
            var netuser = info.sender.localData as NetUser;
            if (netuser == null) { return; }
            Logger.LogWarning("[Fougerite uLink] RPC Message from " + netuser.displayName + "-" + netuser.userID + " triggered an exception. Kicking...");
            if (netuser.connected) { netuser.Kick(NetError.Facepunch_Kick_Violation, true); }
        }

        public static void uLinkCatch(Class0 instance)
        {
            string ip = ((IPEndPoint)(instance.endPoint_0)).Address.ToString();
            Logger.Log("[uLink Ignore] Ignored Socket from: " + ip);
        }

        public static Inventory.SlotOperationResult FGSlotOperation(Inventory inst, int fromSlot, Inventory toInventory, int toSlot, Inventory.SlotOperationsInfo info)
        {
            IInventoryItem itemf;
            IInventoryItem itemf2;
            if (((byte)(((byte)Inventory.SlotOperations.Combine | (byte)Inventory.SlotOperations.Move | (byte)Inventory.SlotOperations.Stack) & (byte)info.SlotOperations)) == 0)
            {
                return Inventory.SlotOperationResult.Error_NoOpArgs;
            }
            if ((inst == null) || (toInventory == null))
            {
                return Inventory.SlotOperationResult.Error_MissingInventory;
            }
            if (inst == toInventory)
            {
                if (toSlot == fromSlot)
                {
                    return Inventory.SlotOperationResult.Error_SameSlot;
                }
                if ((((byte)((byte)Inventory.SlotOperations.EnsureAuthenticLooter & (byte)info.SlotOperations)) == 0x80) && !inst.IsAnAuthorizedLooter(info.Looter, ((byte)((byte)Inventory.SlotOperations.ReportCheater & (byte)info.SlotOperations)) == 0x40, "slotop_srcdst"))
                {
                    return Inventory.SlotOperationResult.Error_NotALooter;
                }
            }
            else if (((byte)((byte)Inventory.SlotOperations.EnsureAuthenticLooter & (byte)info.SlotOperations)) == 0x80)
            {
                bool reportCheater = ((byte)((byte)Inventory.SlotOperations.ReportCheater & (byte)info.SlotOperations)) == 0x40;
                if (!inst.IsAnAuthorizedLooter(info.Looter, reportCheater, "slotop_src") || !toInventory.IsAnAuthorizedLooter(info.Looter, reportCheater, "slotop_dst"))
                {
                    ItemMoveEvent ime4 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                    if (ime4.Player != null)
                    {
                        Logger.LogError("[ItemLoot] The Game says " + ime4.Player.Name +
                                        " probably cheats with inv. Report this to DreTaX on fougerite.com");
                    }
                    return Inventory.SlotOperationResult.Error_NotALooter;
                }
            }
            
            if (!inst.GetItem(fromSlot, out itemf))
            {
                return Inventory.SlotOperationResult.Error_EmptySourceSlot;
            }
            if (toInventory.GetItem(toSlot, out itemf2))
            {
                InventoryItem.MergeResult failed;
                inst.MarkSlotDirty(fromSlot);
                toInventory.MarkSlotDirty(toSlot);
                if ((((byte)(((byte)Inventory.SlotOperations.Combine | (byte)Inventory.SlotOperations.Stack) & (byte)info.SlotOperations)) == 1) && (itemf.datablock.uniqueID == itemf2.datablock.uniqueID))
                {
                    failed = itemf.TryStack(itemf2);
                }
                else if (((byte)(((byte)Inventory.SlotOperations.Combine | (byte)Inventory.SlotOperations.Stack) & (byte)info.SlotOperations)) != 0)
                {
                    failed = itemf2.TryCombine(itemf2);
                }
                else
                {
                    failed = InventoryItem.MergeResult.Failed;
                }
                switch (failed)
                {
                    case InventoryItem.MergeResult.Merged:
                        ItemMoveEvent ime2 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                        try
                        {
                            if (OnItemMove != null)
                            {
                                OnItemMove(ime2);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("ItemMoveEvent Error: " + ex);
                        }
                        return Inventory.SlotOperationResult.Success_Stacked;

                    case InventoryItem.MergeResult.Combined:
                        ItemMoveEvent ime3 = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
                        try
                        {
                            if (OnItemMove != null)
                            {
                                OnItemMove(ime3);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError("ItemMoveEvent Error: " + ex);
                        }
                        return Inventory.SlotOperationResult.Success_Combined;
                }
                if (((byte)((byte)Inventory.SlotOperations.Move & (byte)info.SlotOperations)) == 4)
                {
                    return Inventory.SlotOperationResult.Error_OccupiedDestination;
                }
                return Inventory.SlotOperationResult.NoOp;
            }
            if (((byte)((byte)Inventory.SlotOperations.Move & (byte)info.SlotOperations)) == 0)
            {
                return Inventory.SlotOperationResult.Error_EmptyDestinationSlot;
            }
            if (!inst.MoveItemAtSlotToEmptySlot(toInventory, fromSlot, toSlot))
            {
                return Inventory.SlotOperationResult.Error_Failed;
            }
            if (inst != null)
            {
                inst.MarkSlotDirty(fromSlot);
            }
            if (toInventory != null)
            {
                toInventory.MarkSlotDirty(toSlot);
            }
            ItemMoveEvent ime = new ItemMoveEvent(inst, fromSlot, toInventory, toSlot, info);
            try
            {
                if (OnItemMove != null)
                {
                    OnItemMove(ime);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ItemMoveEvent Error: " + ex);
            }
            return Inventory.SlotOperationResult.Success_Moved;

        }

        public static bool FGCompleteRepair(RepairBench inst, Inventory ingredientInv)
        {
            BlueprintDataBlock block;
            if (!inst.CanRepair(ingredientInv))
            {
                return false;
            }
            IInventoryItem repairItem = inst.GetRepairItem();
            if (!BlueprintDataBlock.FindBlueprintForItem<BlueprintDataBlock>(repairItem.datablock, out block))
            {
                return false;
            }
            RepairEvent re = new RepairEvent(inst, ingredientInv);
            try
            {
                if (OnRepairBench != null)
                {
                    OnRepairBench(re);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("RepairEvent Error: " + ex);
            }
            if (re._cancel)
            {
                return false;
            }
            for (int i = 0; i < block.ingredients.Length; i++)
            {
                BlueprintDataBlock.IngredientEntry entry = block.ingredients[i];
                int count = Mathf.RoundToInt(block.ingredients[i].amount * inst.GetResourceScalar());
                if (count > 0)
                {
                    while (count > 0)
                    {
                        int totalNum = 0;
                        IInventoryItem item2 = ingredientInv.FindItem(entry.Ingredient, out totalNum);
                        if (item2 != null)
                        {
                            if (item2.Consume(ref count))
                            {
                                ingredientInv.RemoveItem(item2.slot);
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
            float num4 = repairItem.maxcondition - repairItem.condition;
            float num5 = (num4 * 0.2f) + 0.05f;
            repairItem.SetMaxCondition(repairItem.maxcondition - num5);
            repairItem.SetCondition(repairItem.maxcondition);
            return true;
        }

        public static bool OnBanEventHandler(BanEvent be)
        {
            try
            {
                if (OnPlayerBan != null)
                {
                    OnPlayerBan(be);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("BanEvent Error: " + ex);
            }
            return be.Cancelled;
        }

        public static void ResetHooks()
        {
            OnPluginInit = delegate
            {
            };
            OnPlayerTeleport = delegate (Fougerite.Player param0, Vector3 param1, Vector3 param2)
            {
            };
            OnChat = delegate (Fougerite.Player param0, ref ChatString param1)
            {
            };
            OnChatRaw = delegate (ref ConsoleSystem.Arg param0)
            {
            };
            OnCommand = delegate (Fougerite.Player param0, string param1, string[] param2)
            {
            };
            OnCommandRaw = delegate (ref ConsoleSystem.Arg param0)
            {
            };
            OnPlayerConnected = delegate (Fougerite.Player param0)
            {
            };
            OnPlayerDisconnected = delegate (Fougerite.Player param0)
            {
            };
            OnNPCKilled = delegate (DeathEvent param0)
            {
            };
            OnNPCHurt = delegate (HurtEvent param0)
            {
            };
            OnPlayerKilled = delegate (DeathEvent param0)
            {
            };
            OnPlayerHurt = delegate (HurtEvent param0)
            {
            };
            OnPlayerSpawned = delegate (Fougerite.Player param0, SpawnEvent param1)
            {
            };
            OnPlayerSpawning = delegate (Fougerite.Player param0, SpawnEvent param1)
            {
            };
            OnPlayerGathering = delegate (Fougerite.Player param0, GatherEvent param1)
            {
            };
            OnEntityHurt = delegate (HurtEvent param0)
            {
            };
            OnEntityDestroyed = delegate (DestroyEvent param0)
            {
            };
            OnEntityDecay = delegate (DecayEvent param0)
            {
            };
            OnEntityDeployed = delegate (Fougerite.Player param0, Entity param1)
            {
            };
            OnEntityDeployedWithPlacer = delegate (Fougerite.Player param0, Entity param1, Fougerite.Player param2)
            {
            };
            OnConsoleReceived = delegate (ref ConsoleSystem.Arg param0, bool param1)
            {
            };
            OnBlueprintUse = delegate (Fougerite.Player param0, BPUseEvent param1)
            {
            };
            OnDoorUse = delegate (Fougerite.Player param0, DoorEvent param1)
            {
            };
            OnTablesLoaded = delegate (Dictionary<string, LootSpawnList> param0)
            {
            };
            OnItemsLoaded = delegate (ItemsBlocks param0)
            {
            };
            OnServerInit = delegate
            {
            };
            OnServerShutdown = delegate
            {
            };
            OnModulesLoaded = delegate
            {
            };
            OnRecieveNetwork = delegate (Fougerite.Player param0, Metabolism param1, float param2, float param3,
                float param4, float param5, float param6, float param7)
            {
            };
            OnShowTalker = delegate (uLink.NetworkPlayer param0, Fougerite.Player param1)
            {
            };
            OnCrafting = delegate (Fougerite.Events.CraftingEvent param0)
            {
            };
            OnResourceSpawned = delegate (ResourceTarget param0)
            {
            };
            OnItemRemoved = delegate (Fougerite.Events.InventoryModEvent param0)
            {
            };
            OnItemAdded = delegate (Fougerite.Events.InventoryModEvent param0)
            {
            };
            OnAirdropCalled = delegate (Vector3 param0)
            {
            };
            OnSteamDeny = delegate (SteamDenyEvent param0)
            {
            };
            OnPlayerApproval = delegate (PlayerApprovalEvent param0)
            {
            };
            OnPlayerMove = delegate (HumanController param0, Vector3 param1, int param2, ushort param3, uLink.NetworkMessageInfo param4, Util.PlayerActions param5)
            {
            };
            OnResearch = delegate (ResearchEvent param0)
            {
            };
            OnServerSaved = delegate
            {
            };
            OnItemPickup = delegate (ItemPickupEvent param0)
            {
            };
            OnFallDamage = delegate (FallDamageEvent param0)
            {
            };
            OnLootUse = delegate (LootStartEvent param0)
            {
            };
            OnShoot = delegate (ShootEvent param0)
            {
            };
            OnBowShoot = delegate (BowShootEvent param0)
            {
            };
            OnShotgunShoot = delegate (ShotgunShootEvent param0)
            {
            };
            OnGrenadeThrow = delegate (GrenadeThrowEvent param0)
            {
            };
            OnPlayerBan = delegate (BanEvent param0)
            {
            };
            OnRepairBench = delegate (RepairEvent param0)
            {
            };
            OnItemMove = delegate (ItemMoveEvent param0)
            {
            };
            foreach (Fougerite.Player player in Fougerite.Server.GetServer().Players)
            {
                player.FixInventoryRef();
            }
        }

        public static void ServerShutdown()
        {
            IsShuttingDown = true;
            DataStore.GetInstance().Save();
            //ServerSaveManager.AutoSave();
            try
            {
                if (OnServerShutdown != null)
                    OnServerShutdown();
            }
            catch (Exception ex)
            {
                Logger.LogError("ServerShutdownEvent Error: " + ex.ToString());
            }
        }

        public static void ServerStarted()
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            DataStore.GetInstance().Load();
            Server.GetServer().UpdateBanlist();
            try
            {
                if (OnServerInit != null)
                    OnServerInit();
            }
            catch (Exception ex)
            {
                Logger.LogError("ServerInitEvent Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ServerStartedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static void ShowTalker(uLink.NetworkPlayer player, PlayerClient p)
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            var pl = Fougerite.Server.Cache[p.userID];
            try
            {
                if (OnShowTalker != null)
                    OnShowTalker(player, pl);
            }
            catch (Exception ex)
            {
                Logger.LogError("ShowTalkerEvent Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("MicUseEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        internal static void ModulesLoaded()
        {
            Stopwatch sw = null;
            if (Logger.showSpeed)
            {
                sw = new Stopwatch();
                sw.Start();
            }
            try
            {
                if (OnModulesLoaded != null)
                    OnModulesLoaded();
            }
            catch (Exception ex)
            {
                Logger.LogError("ModulesLoadedEvent Error: " + ex.ToString());
            }
            if (sw == null) return;
            sw.Stop();
            if (sw.Elapsed.TotalSeconds > 0) Logger.LogSpeed("ModulesLoadedEvent Speed: " + Math.Round(sw.Elapsed.TotalSeconds) + " secs");
        }

        public static Dictionary<string, LootSpawnList> TablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            try
            {
                if (OnTablesLoaded != null)
                    OnTablesLoaded(lists);
            }
            catch (Exception ex)
            {
                Logger.LogError("TablesLoadedEvent Error: " + ex.ToString());
            }
            return lists;
        }

        public delegate void BlueprintUseHandlerDelegate(Fougerite.Player player, BPUseEvent ae);
        public delegate void ChatHandlerDelegate(Fougerite.Player player, ref ChatString text);
        public delegate void ChatRawHandlerDelegate(ref ConsoleSystem.Arg arg);
        public delegate void CommandHandlerDelegate(Fougerite.Player player, string cmd, string[] args);
        public delegate void CommandRawHandlerDelegate(ref ConsoleSystem.Arg arg);
        public delegate void ConnectionHandlerDelegate(Fougerite.Player player);
        public delegate void ConsoleHandlerDelegate(ref ConsoleSystem.Arg arg, bool external);
        public delegate void DisconnectionHandlerDelegate(Fougerite.Player player);
        public delegate void DoorOpenHandlerDelegate(Fougerite.Player player, DoorEvent de);
        public delegate void EntityDecayDelegate(DecayEvent de);
        public delegate void EntityDeployedDelegate(Fougerite.Player player, Entity e);
        public delegate void EntityDeployedWithPlacerDelegate(Fougerite.Player player, Entity e, Fougerite.Player actualplacer);
        public delegate void EntityHurtDelegate(HurtEvent he);
        public delegate void EntityDestroyedDelegate(DestroyEvent de);
        public delegate void HurtHandlerDelegate(HurtEvent he);
        public delegate void ItemsDatablocksLoaded(ItemsBlocks items);
        public delegate void KillHandlerDelegate(DeathEvent de);
        public delegate void LootTablesLoaded(Dictionary<string, LootSpawnList> lists);
        public delegate void PlayerGatheringHandlerDelegate(Fougerite.Player player, GatherEvent ge);
        public delegate void PlayerSpawnHandlerDelegate(Fougerite.Player player, SpawnEvent se);
        public delegate void ShowTalkerDelegate(uLink.NetworkPlayer player, Fougerite.Player p);
        public delegate void PluginInitHandlerDelegate();
        public delegate void TeleportDelegate(Fougerite.Player player, Vector3 from, Vector3 dest);
        public delegate void ServerInitDelegate();
        public delegate void ServerShutdownDelegate();
        public delegate void ModulesLoadedDelegate();
        public delegate void RecieveNetworkDelegate(Fougerite.Player player, Metabolism m, float cal, float water, float rad, float anti, float temp, float poison);
        public delegate void CraftingDelegate(Fougerite.Events.CraftingEvent e);
        public delegate void ResourceSpawnDelegate(ResourceTarget t);
        public delegate void ItemRemovedDelegate(Fougerite.Events.InventoryModEvent e);
        public delegate void ItemAddedDelegate(Fougerite.Events.InventoryModEvent e);
        public delegate void AirdropDelegate(Vector3 v);
        public delegate void SteamDenyDelegate(SteamDenyEvent sde);
        public delegate void PlayerApprovalDelegate(PlayerApprovalEvent e);
        public delegate void PlayerMoveDelegate(HumanController hc, Vector3 origin, int encoded, ushort stateFlags, uLink.NetworkMessageInfo info, Util.PlayerActions action);
        public delegate void ResearchDelegate(ResearchEvent re);
        public delegate void ServerSavedDelegate();
        public delegate void ItemPickupDelegate(ItemPickupEvent itemPickupEvent);
        public delegate void FallDamageDelegate(FallDamageEvent fallDamageEvent);
        public delegate void LootEnterDelegate(LootStartEvent lootStartEvent);
        public delegate void ShootEventDelegate(ShootEvent shootEvent);
        public delegate void ShotgunShootEventDelegate(ShotgunShootEvent shootEvent);
        public delegate void BowShootEventDelegate(BowShootEvent bowshootEvent);
        public delegate void GrenadeThrowEventDelegate(GrenadeThrowEvent grenadeThrowEvent);
        public delegate void BanEventDelegate(BanEvent banEvent);
        public delegate void RepairBenchEventDelegate(RepairEvent repairEvent);
        public delegate void ItemMoveEventDelegate(ItemMoveEvent itemMoveEvent);
        //public delegate void AirdropCrateDroppedDelegate(GameObject go);
    }
}