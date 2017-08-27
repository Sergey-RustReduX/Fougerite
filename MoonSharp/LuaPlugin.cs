﻿
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fougerite;
using Fougerite.Events;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;
using UnityEngine;

namespace MoonSharpModule
{
    public class LuaPlugin
    {
        public readonly string Name;
        public readonly string Code;
        public readonly object Class;
        public readonly string Author;
        public readonly string Version;
        public readonly string About;
        public readonly DirectoryInfo RootDir;
        public readonly IList<string> Globals;
        public readonly Table Tables;
        public readonly Script script;
        public readonly Dictionary<string, MoonSharpTE> Timers;
        public readonly List<MoonSharpTE> ParallelTimers;

        public LuaPlugin(string name, string code, DirectoryInfo path)
        {
            //MoonSharp.Interpreter.Script.DefaultOptions.ScriptLoader = new MoonSharp.Interpreter.Loaders.EmbeddedResourcesScriptLoader();
            Globals = new List<string>();
            Timers = new Dictionary<string, MoonSharpTE>();
            ParallelTimers = new List<MoonSharpTE>();
			Name = name;
			Code = code;
			RootDir = path;
            UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
            script = new Script();
            script.DoString(code);
            script.Globals.Set("Util", UserData.Create(Fougerite.Util.GetUtil()));
            script.Globals.Set("Plugin", UserData.Create(this));
            script.Globals.Set("Server", UserData.Create(Fougerite.Server.GetServer()));
            script.Globals.Set("DataStore", UserData.Create(Fougerite.DataStore.GetInstance()));
            script.Globals.Set("Data", UserData.Create(Fougerite.Data.GetData()));
            script.Globals.Set("Web", UserData.Create(new Fougerite.Web()));
            script.Globals.Set("World", UserData.Create(Fougerite.World.GetWorld()));
            script.Globals.Set("PluginCollector", UserData.Create(GlobalPluginCollector.GetPluginCollector()));
            script.Globals.Set("Loom", UserData.Create(Loom.Current));
            script.Globals.Set("JSON", UserData.Create(JsonAPI.GetInstance));
            script.Globals.Set("MySQL", UserData.Create(MySQLConnector.GetInstance));
            script.Globals.Set("SQLite", UserData.Create(Fougerite.SQLiteConnector.GetInstance));
            foreach (DynValue v in script.Globals.Keys)
            {
                Globals.Add(v.ToString().Replace('"'.ToString(), ""));
            }
            Author = string.IsNullOrEmpty(script.Globals.Get("Author").String) ? "Unknown" : script.Globals.Get("Author").String;
            Version = string.IsNullOrEmpty(script.Globals.Get("Version").String) ? "1.0" : script.Globals.Get("Version").String;
            About = script.Globals.Get("About").String;
            Tables = script.Globals;
        }

        public object Invoke(string func, params object[] obj)
        {
            try
            {
                DynValue luaFactFunction = script.Globals.Get(func);
                if (luaFactFunction != null)
                {
                    return script.Call(luaFactFunction, obj);
                }
                Fougerite.Logger.LogDebug("[MoonSharp] Function: " + func + " not found in plugin: " + Name);
            }
            catch (Exception ex)
            {
                Fougerite.Logger.LogError("[MoonSharp] Error in plugin " + Name + ":");
                Fougerite.Logger.LogError("[MoonSharp] Invoke failed: " + ex.ToString());
            }
            return null;
        }

        public void OnTablesLoaded(Dictionary<string, LootSpawnList> tables)
        {
            this.Invoke("On_TablesLoaded", tables);
        }

        public void OnAllPluginsLoaded()
        {
            this.Invoke("On_AllPluginsLoaded", new object[0]);
        }

        public void OnBlueprintUse(Fougerite.Player player, BPUseEvent evt)
        {
            this.Invoke("On_BlueprintUse", new object[] { player, evt });
        }

        public void OnChat(Fougerite.Player player, ref ChatString text)
        {
            this.Invoke("On_Chat", new object[] { player, text });
        }

        public void OnCommand(Fougerite.Player player, string command, string[] args)
        {
            this.Invoke("On_Command", new object[] { player, command, args });
        }

        public void OnConsole(ref ConsoleSystem.Arg arg, bool external)
        {
            string clss = arg.Class.ToLower();
            string func = arg.Function.ToLower();
            if (!external)
            {
                Fougerite.Player player = Fougerite.Player.FindByPlayerClient(arg.argUser.playerClient);
                arg.ReplyWith(player.Name + " executed: " + clss + "." + func);
                this.Invoke("On_Console", new object[] { player, arg });
            }
            else
            {
                arg.ReplyWith("Rcon: " + clss + "." + func);
                this.Invoke("On_Console", new object[] { null, arg });
            }
        }

        public void OnDoorUse(Fougerite.Player player, DoorEvent evt)
        {
            this.Invoke("On_DoorUse", new object[] { player, evt });
        }

        public void OnEntityDecay(DecayEvent evt)
        {
            this.Invoke("On_EntityDecay", new object[] { evt });
        }

        public void OnEntityDeployed(Fougerite.Player player, Entity entity, Fougerite.Player actualplacer)
        {
            try
            {
                DynValue luaFactFunction = script.Globals.Get("On_EntityDeployed");
                if (luaFactFunction != null)
                {
                    script.Call(luaFactFunction, new object[] {player, entity, actualplacer});
                }
            }
            catch(Exception ex)
            {
                Logger.LogError("lua: " + ex);
                this.Invoke("On_EntityDeployed", new object[] {player, entity});
            }
        }

        public void OnEntityDestroyed(DestroyEvent evt)
        {
            this.Invoke("On_EntityDestroyed", new object[] { evt });
        }

        public void OnEntityHurt(HurtEvent evt)
        {
            this.Invoke("On_EntityHurt", new object[] { evt });
        }

        public void OnItemsLoaded(ItemsBlocks items)
        {
            this.Invoke("On_ItemsLoaded", new object[] { items });
        }

        public void OnNPCHurt(HurtEvent evt)
        {
            this.Invoke("On_NPCHurt", new object[] { evt });
        }

        public void OnNPCKilled(DeathEvent evt)
        {
            this.Invoke("On_NPCKilled", new object[] { evt });
        }

        public void OnPlayerConnected(Fougerite.Player player)
        {
            this.Invoke("On_PlayerConnected", new object[] { player });
        }

        public void OnPlayerDisconnected(Fougerite.Player player)
        {
            this.Invoke("On_PlayerDisconnected", new object[] { player });
        }

        public void OnPlayerGathering(Fougerite.Player player, GatherEvent evt)
        {
            this.Invoke("On_PlayerGathering", new object[] { player, evt });
        }

        public void OnPlayerHurt(HurtEvent evt)
        {
            this.Invoke("On_PlayerHurt", new object[] { evt });
        }

        public void OnPlayerKilled(DeathEvent evt)
        {
            this.Invoke("On_PlayerKilled", new object[] { evt });
        }

        public void OnPlayerTeleport(Fougerite.Player player, Vector3 from, Vector3 dest)
        {
            this.Invoke("On_PlayerTeleport", new object[] { player, from, dest });
        }

        public void OnPlayerSpawn(Fougerite.Player player, SpawnEvent evt)
        {
            this.Invoke("On_PlayerSpawning", new object[] { player, evt });
        }

        public void OnPlayerSpawned(Fougerite.Player player, SpawnEvent evt)
        {
            this.Invoke("On_PlayerSpawned", new object[] { player, evt });
        }

        public void OnResearch(ResearchEvent evt)
        {
            this.Invoke("On_Research", new object[] { evt });
        }

        public void OnServerInit()
        {
            this.Invoke("On_ServerInit", new object[0]);
        }

        public void OnServerShutdown()
        {
            this.Invoke("On_ServerShutdown", new object[0]);
        }

        public void OnServerSaved()
        {
            this.Invoke("On_ServerSaved", new object[0]);
        }

        public void OnCrafting(CraftingEvent e)
        {
            this.Invoke("On_Crafting", new object[] { e });
        }

        public void OnResourceSpawned(ResourceTarget t)
        {
            this.Invoke("On_ResourceSpawn", new object[] { t });
        }

        public void OnItemAdded(InventoryModEvent e)
        {
            this.Invoke("On_ItemAdded", new object[] { e });
        }

        public void OnShowTalker(uLink.NetworkPlayer np, Fougerite.Player player)
        {
            this.Invoke("On_VoiceChat", np, player);
        }

        /*public void OnItemAddedOxide(InventoryModEvent e)
        {
            this.Invoke("OnItemAdded", new object[] { e.Inventory, e.Slot, e.Item });
        }*/

        public void OnItemRemoved(InventoryModEvent e)
        {
            this.Invoke("On_ItemRemoved", new object[] { e });
        }

        public void OnItemPickup(ItemPickupEvent e)
        {
            this.Invoke("On_ItemPickup", new object[] { e });
        }

        public void OnFallDamage(FallDamageEvent e)
        {
            this.Invoke("On_FallDamage", new object[] { e });
        }

        /*public void OnItemRemovedOxide(InventoryModEvent e)
        {
            this.Invoke("OnItemRemoved", new object[] { e.Inventory, e.Slot, e.Item });
        }*/

        public void OnAirdrop(Vector3 v)
        {
            this.Invoke("On_Airdrop", new object[] { v });
        }

        /*public void OnAirdropCrateDropped(GameObject go)
        {
            this.Invoke("On_AirdropCrateDropped", new object[] { new Entity(go) });
        }*/

        public void OnSteamDeny(SteamDenyEvent e)
        {
            this.Invoke("On_SteamDeny", new object[] { e });
        }

        public void OnPlayerApproval(PlayerApprovalEvent e)
        {
            this.Invoke("On_PlayerApproval", new object[] { e });
        }

        public void OnPluginShutdown()
        {
            this.Invoke("On_PluginShutdown", new object[0]);
        }

        public void OnLootUse(LootStartEvent le)
        {
            this.Invoke("On_LootUse", new object[] { le });
        }

        public void OnBanEvent(BanEvent be)
        {
            this.Invoke("On_PlayerBan", new object[] { be });
        }
   
        public void OnRepairBench(Fougerite.Events.RepairEvent be)
        {
            this.Invoke("On_RepairBench", new object[] { be });
        }

        public void OnItemMove(ItemMoveEvent be)
        {
            this.Invoke("On_ItemMove", new object[] { be });
        }

        public void OnTimerCB(MoonSharpTE evt)
        {
            if (Globals.Contains(evt.Name + "Callback"))
            {
                try
                {
                    this.Invoke(evt.Name + "Callback", evt);
                }
                catch (Exception ex)
                {
                    Fougerite.Logger.LogError("Failed to invoke callback " + evt.Name + " Ex: " + ex);
                }
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private string ValidateRelativePath(string path)
        {
            string normalizedPath = NormalizePath(Path.Combine(RootDir.FullName, path));
            string rootDirNormalizedPath = NormalizePath(RootDir.FullName);

            if (!normalizedPath.StartsWith(rootDirNormalizedPath))
                return null;

            return normalizedPath;
        }

        public void DeleteLog(string path)
        {
            path = ValidateRelativePath(path + ".log");
            if (path == null)
                return;

            if (File.Exists(path))
                File.Delete(path);
        }


        public void Log(string p, string text)
        {
            string path = ValidateRelativePath(p + ".log");
            if (path == null)
                return;

            File.AppendAllText(path, "[" + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToString("HH:mm:ss") + "] " + text + "\r\n");
            FileInfo fi = new FileInfo(path);
            if (fi.Exists)
            {
                float mega = (fi.Length / 1024f) / 1024f;
                if (mega > 1.0)
                {
                    try
                    {
                        string d = DateTime.Now.ToShortDateString().Replace('/', '-');
                        File.Move(path, ValidateRelativePath(p + "-OLD-" + d + ".log"));
                    }
                    catch
                    {
                    }
                }
            }
        }

        #region inifiles

        public IniParser GetIni(string path)
        {
            path = ValidateRelativePath(path + ".ini");
            if (path == null)
                return (IniParser)null;

            if (File.Exists(path))
                return new IniParser(path);

            return (IniParser)null;
        }

        public bool IniExists(string path)
        {
            path = ValidateRelativePath(path + ".ini");
            if (path == null)
                return false;

            return File.Exists(path);
        }

        public IniParser CreateIni(string path)
        {
            try
            {
                path = ValidateRelativePath(path + ".ini");
                if (path == null)
                    return (IniParser)null;

                File.WriteAllText(path, "");
                return new IniParser(path);
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            return (IniParser)null;
        }

        public List<IniParser> GetInis(string path)
        {
            path = ValidateRelativePath(path);
            if (path == null)
                return new List<IniParser>();

            return Directory.GetFiles(path).Select(p => new IniParser(p)).ToList();
        }

        #endregion

        public LuaPlugin GetPlugin(string name)
        {
            LuaPlugin plugin;
            LuaModule.Plugins.TryGetValue(name, out plugin);
            if (plugin == null)
            {
                Logger.LogDebug("[MoonSharp] [GetPlugin] '" + name + "' plugin not found!");
                return null;
            }
            return plugin;
        }

        #region time
        // CONSIDER: putting these into a separate class along with some new shortcut
        //				Time.GetDate() looks more appropriate than Plugin.GetDate()
        public string GetDate()
        {
            return DateTime.Now.ToShortDateString();
        }

        public int GetTicks()
        {
            return Environment.TickCount;
        }

        public string GetTime()
        {
            return DateTime.Now.ToShortTimeString();
        }

        public long GetTimestamp()
        {
            TimeSpan span = (TimeSpan)(DateTime.UtcNow - new DateTime(0x7b2, 1, 1, 0, 0, 0));
            return (long)span.TotalSeconds;
        }

        #endregion

        #region timer methods

        public MoonSharpTE CreateTimer(string name, int timeoutDelay)
        {
            MoonSharpTE timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                timedEvent = new MoonSharpTE(name, (double)timeoutDelay);
                timedEvent.OnFire += new MoonSharpTE.TimedEventFireDelegate(OnTimerCB);
                Timers.Add(name, timedEvent);
            }
            return timedEvent;
        }

        public MoonSharpTE CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args)
        {
            MoonSharpTE timedEvent = GetTimer(name);
            if (timedEvent == null)
            {
                timedEvent = new MoonSharpTE(name, (double)timeoutDelay);
                timedEvent.Args = args;
                timedEvent.OnFire += new MoonSharpTE.TimedEventFireDelegate(OnTimerCB);
                Timers.Add(name, timedEvent);
            }
            return timedEvent;
        }

        public MoonSharpTE GetTimer(string name)
        {
            MoonSharpTE result;
            if (Timers.ContainsKey(name))
            {
                result = Timers[name];
            }
            else
            {
                result = null;
            }
            return result;
        }

        public void KillTimer(string name)
        {
            MoonSharpTE timer = GetTimer(name);
            if (timer == null)
                return;

            timer.Kill();
            Timers.Remove(name);
        }

        public void KillTimers()
        {
            foreach (MoonSharpTE current in Timers.Values)
            {
                current.Kill();
            }
            foreach (MoonSharpTE timer in ParallelTimers)
            {
                timer.Kill();
            }
            Timers.Clear();
            ParallelTimers.Clear();
        }

        #endregion

        #region ParalellTimers

        public MoonSharpTE CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args)
        {
            MoonSharpTE timedEvent = new MoonSharpTE(name, (double)timeoutDelay);
            timedEvent.Args = args;
            timedEvent.OnFire += new MoonSharpTE.TimedEventFireDelegate(OnTimerCB);
            ParallelTimers.Add(timedEvent);
            return timedEvent;
        }

        public List<MoonSharpTE> GetParallelTimer(string name)
        {
            return (from timer in ParallelTimers
                    where timer.Name == name
                    select timer).ToList();
        }

        public void KillParallelTimer(string name)
        {
            foreach (MoonSharpTE timer in GetParallelTimer(name))
            {
                timer.Kill();
                ParallelTimers.Remove(timer);
            }
        }

        #endregion

        public Dictionary<string, object> CreateDict()
        {
            return new Dictionary<string, object>();
        }

        public List<string> CreateList()
        {
            return new List<string>();
        }

        private string getBetween(string strSource, string strStart, string strEnd)
        {
            int Start, End;
            if (strSource.Contains(strStart) && strSource.Contains(strEnd))
            {
                Start = strSource.IndexOf(strStart, 0) + strStart.Length;
                End = strSource.IndexOf(strEnd, Start);
                return strSource.Substring(Start, End - Start);
            }
            else
            {
                return "";
            }
        }
    }
}
