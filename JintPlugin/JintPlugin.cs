﻿
using Jint.Native;

namespace JintModule
{
    using System;
    using System.Net;
    using System.Text;
    using System.Collections.Generic;
    using System.IO;
    using Fougerite;
    using Fougerite.Events;
    using Jint;
    using Jint.Parser;
    using Jint.Parser.Ast;
    using System.Linq;
    using UnityEngine;

    public class JintPlugin
    {
        public readonly Engine Engine;
        public readonly string Name;
        public readonly string Code;
        public readonly string Author;
        public readonly string Version;
        public readonly string About;
        public readonly DirectoryInfo RootDirectory;
        public readonly Dictionary<string, TimedEvent> Timers;
        public readonly AdvancedTimer AdvancedTimers;
        public readonly List<string> CommandList;
        public List<string> FunctionNames; 
        private const string brktname = "[Jint]";

        public JintPlugin(DirectoryInfo directory, string name, string code)
        {
            Name = name;
            Code = code;
            RootDirectory = directory;
            FunctionNames = new List<string>();
            CommandList = new List<string>();
            Timers = new Dictionary<string, TimedEvent>();
            AdvancedTimers = new AdvancedTimer(this);

            Engine = new Engine(cfg => cfg.AllowClr(typeof(UnityEngine.GameObject).Assembly,
                typeof(uLink.NetworkPlayer).Assembly,
                typeof(PlayerInventory).Assembly))
                .SetValue("Server", Server.GetServer())
                .SetValue("DataStore", DataStore.GetInstance())
                .SetValue("Util", Util.GetUtil())
                .SetValue("World", World.GetWorld())
                .SetValue("Web", new Web())
                .SetValue("Plugin", this)
                .SetValue("Data", Data.GetData())
                .SetValue("PluginCollector", GlobalPluginCollector.GetPluginCollector())
                .SetValue("Loom", Loom.Current)
                .SetValue("JSON", JsonAPI.GetInstance)
                .SetValue("MySQL", MySQLConnector.GetInstance)
                .SetValue("SQLite", SQLiteConnector.GetInstance)
                .Execute(code);

            object author = GetGlobalObject("Author");
            object about = GetGlobalObject("About");
            object version = GetGlobalObject("Version");
            Author = author == null || author == "undefined" ? "Unknown" : author.ToString();
            About = about == null || about == "undefined" ? "" : about.ToString();
            Version = version == null || version == "undefined" ? "1.0" : version.ToString();
            Logger.LogDebug(string.Format("{0} AllowClr for Assemblies: {1} {2} {3}", brktname,
                typeof(UnityEngine.GameObject).Assembly.GetName().Name,
                typeof(uLink.NetworkPlayer).Assembly.GetName().Name,
                typeof(PlayerInventory).Assembly.GetName().Name));
        }

        public JintPlugin GetPlugin(string name)
        {
            JintPlugin plugin;
            JintPluginModule.Plugins.TryGetValue(name, out plugin);
            if (plugin == null)
            {
                Logger.LogDebug("[JintPlugin] [GetPlugin] '" + name + "' plugin not found!");
                return null;
            }
            return plugin;
        }

        public JsValue GetVariable(string name)
        {
            return Engine.GetValue(name);
        }

        public IniParser GetIni(string path)
        {
            path = ValidateRelativePath(path + ".ini");

            if (path == null)
                return null;

            if (File.Exists(path))
                return new IniParser(path);

            return null;
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

                File.WriteAllText(path, "");
                return new IniParser(path);
            }
            catch (Exception ex)
            {
                Logger.LogError("[JintPlugin] " + Name + " Failed to Create IniFile! Path: " + path + " Exception: " + ex);
            }

            return null;
        }

        public List<IniParser> GetInis(string path)
        {
            path = ValidateRelativePath(path);

            if (path == null)
                return new List<IniParser>();

            return Directory.GetFiles(path).Select(p => new IniParser(p)).ToList();
        }

        public object Invoke(string func, params object[] obj)
        {
            try
            {
                if (FunctionNames.Contains(func))
                { 
                    return Engine.Invoke(func, obj);
                }
            } catch (Exception ex) {
                Logger.LogError(string.Format("{0} Error invoking function {1} in {2} plugin.", brktname, func, Name));
                Logger.LogException(ex);
            }
            return null;
        }

        public object GetGlobalObject(string identifier)
        {
            return Engine.GetValue(identifier);
        }

        public IEnumerable<FunctionDeclaration> GetSourceCodeGlobalFunctions()
        {
            JavaScriptParser parser = new JavaScriptParser();
            foreach (FunctionDeclaration funcDecl in parser.Parse(Code).FunctionDeclarations) {
                yield return funcDecl;
            }
        }

        public void InstallHooks()
        {
            foreach (var funcDecl in GetSourceCodeGlobalFunctions()) {
                Logger.LogDebug(string.Format("{0} Found Function: {1}", brktname, funcDecl.Id.Name));
                if (!FunctionNames.Contains(funcDecl.Id.Name)) { FunctionNames.Add(funcDecl.Id.Name);}
                switch (funcDecl.Id.Name) {
                case "On_ServerInit":
                    Hooks.OnServerInit += OnServerInit;
                    break;
                case "On_PluginInit":
                    Hooks.OnPluginInit += OnPluginInit;
                    break;
                case "On_ServerShutdown":
                    Hooks.OnServerShutdown += OnServerShutdown;
                    break;
                case "On_ItemsLoaded":
                    Hooks.OnItemsLoaded += OnItemsLoaded;
                    break;
                case "On_TablesLoaded":
                    Hooks.OnTablesLoaded += OnTablesLoaded;
                    break;
                case "On_Chat":
                    Hooks.OnChat += OnChat;
                    break;
                case "On_Console":
                    Hooks.OnConsoleReceived += OnConsole;
                    break;
                case "On_Command":
                    Hooks.OnCommand += OnCommand;
                    break;
                case "On_PlayerConnected":
                    Hooks.OnPlayerConnected += OnPlayerConnected;
                    break;
                case "On_PlayerDisconnected":
                    Hooks.OnPlayerDisconnected += OnPlayerDisconnected;
                    break;
                case "On_PlayerKilled":
                    Hooks.OnPlayerKilled += OnPlayerKilled;
                    break;
                case "On_PlayerHurt":
                    Hooks.OnPlayerHurt += OnPlayerHurt;
                    break;
                case "On_PlayerSpawning":
                    Hooks.OnPlayerSpawning += OnPlayerSpawn;
                    break;
                case "On_PlayerSpawned":
                    Hooks.OnPlayerSpawned += OnPlayerSpawned;
                    break;
                case "On_PlayerGathering":
                    Hooks.OnPlayerGathering += OnPlayerGathering;
                    break;
                case "On_EntityHurt":
                    Hooks.OnEntityHurt += OnEntityHurt;
                    break;
                case "On_EntityDecay":
                    Hooks.OnEntityDecay += OnEntityDecay;
                    break;
                case "On_EntityDeployed":
                    switch (funcDecl.Parameters.Count())
                    {
                        case 2:
                            Hooks.OnEntityDeployed += OnEntityDeployed;
                            break;
                        case 3:
                            Hooks.OnEntityDeployedWithPlacer += OnEntityDeployed2;
                            break;
                    }
                    break;
                case "On_NPCHurt":
                    Hooks.OnNPCHurt += OnNPCHurt;
                    break;
                case "On_NPCKilled":
                    Hooks.OnNPCKilled += OnNPCKilled;
                    break;
                case "On_BlueprintUse":
                    Hooks.OnBlueprintUse += OnBlueprintUse;
                    break;
                case "On_DoorUse":
                    Hooks.OnDoorUse += OnDoorUse;
                    break;
                case "On_PlayerTeleport":
                    Hooks.OnPlayerTeleport += OnPlayerTeleport;
                    break;
                case "On_Crafting":
                    Hooks.OnCrafting += OnCrafting;
                    break;
                case "On_ResourceSpawn":
                    Hooks.OnResourceSpawned += OnResourceSpawned;
                    break;
                case "On_ItemAdded":
                    Hooks.OnItemAdded += OnItemAdded;
                    break;
                case "On_ItemRemoved":
                    Hooks.OnItemRemoved += OnItemRemoved;
                    break;
                case "On_Airdrop":
                    Hooks.OnAirdropCalled += OnAirdrop;
                    break;
                /*case "On_AirdropCrateDropped":
                    Hooks.OnAirdropCrateDropped += OnAirdropCrateDropped;
                    break;*/
                case "On_SteamDeny":
                    Hooks.OnSteamDeny += OnSteamDeny;
                    break;
                case "On_PlayerApproval":
                    Hooks.OnPlayerApproval += OnPlayerApproval;
                    break;
                case "On_Research":
                    Hooks.OnResearch += OnResearch;
                    break;
                case "On_ServerSaved":
                    Hooks.OnServerSaved += OnServerSaved;
                    break;
                case "On_AllPluginsLoaded":
                    JintPluginModule.OnAllLoaded += OnAllLoaded;
                    break;
                case "On_VoiceChat":
                    Hooks.OnShowTalker += OnShowTalker;
                    break;
                case "On_ItemPickup":
                    Hooks.OnItemPickup += OnItemPickup;
                    break;
                case "On_FallDamage":
                    Hooks.OnFallDamage += OnFallDamage;
                    break;
                case "On_LootUse":
                    Hooks.OnLootUse += OnLootUse;
                    break;
                case "On_PlayerBan":
                    Hooks.OnPlayerBan += OnBanEvent;
                    break;
                case "On_RepairBench":
                    Hooks.OnRepairBench += OnRepairBench;
                    break;
                case "On_ItemMove":
                    Hooks.OnItemMove += OnItemMove;
                    break;
                }
            }
        }

        public void RemoveHooks()
        {
            foreach (var funcDecl in GetSourceCodeGlobalFunctions()) {
                Logger.LogDebug(string.Format("{0} RemoveHooks, found function {1}", brktname, funcDecl.Id.Name));
                switch (funcDecl.Id.Name) {
                case "On_ServerInit":
                    Hooks.OnServerInit -= OnServerInit;
                    break;
                case "On_PluginInit":
                    Hooks.OnPluginInit -= OnPluginInit;
                    break;
                case "On_ServerShutdown":
                    Hooks.OnServerShutdown -= OnServerShutdown;
                    break;
                case "On_ItemsLoaded":
                    Hooks.OnItemsLoaded -= OnItemsLoaded;
                    break;
                case "On_TablesLoaded":
                    Hooks.OnTablesLoaded -= OnTablesLoaded;
                    break;
                case "On_Chat":
                    Hooks.OnChat -= OnChat;
                    break;
                case "On_Console":
                    Hooks.OnConsoleReceived -= OnConsole;
                    break;
                case "On_Command":
                    Hooks.OnCommand -= OnCommand;
                    break;
                case "On_PlayerConnected":
                    Hooks.OnPlayerConnected -= OnPlayerConnected;
                    break;
                case "On_PlayerDisconnected":
                    Hooks.OnPlayerDisconnected -= OnPlayerDisconnected;
                    break;
                case "On_PlayerKilled":
                    Hooks.OnPlayerKilled -= OnPlayerKilled;
                    break;
                case "On_PlayerHurt":
                    Hooks.OnPlayerHurt -= OnPlayerHurt;
                    break;
                case "On_PlayerSpawning":
                    Hooks.OnPlayerSpawning -= OnPlayerSpawn;
                    break;
                case "On_PlayerSpawned":
                    Hooks.OnPlayerSpawned -= OnPlayerSpawned;
                    break;
                case "On_PlayerGathering":
                    Hooks.OnPlayerGathering -= OnPlayerGathering;
                    break;
                case "On_EntityHurt":
                    Hooks.OnEntityHurt -= OnEntityHurt;
                    break;
                case "On_EntityDecay":
                    Hooks.OnEntityDecay -= OnEntityDecay;
                    break;
                case "On_EntityDeployed":
                    switch (funcDecl.Parameters.Count())
                    {
                        case 2:
                            Hooks.OnEntityDeployed -= OnEntityDeployed;
                            break;
                        case 3:
                            Hooks.OnEntityDeployedWithPlacer -= OnEntityDeployed2;
                            break;
                    }
                    break;
                case "On_NPCHurt":
                    Hooks.OnNPCHurt -= OnNPCHurt;
                    break;
                case "On_NPCKilled":
                    Hooks.OnNPCKilled -= OnNPCKilled;
                    break;
                case "On_BlueprintUse":
                    Hooks.OnBlueprintUse -= OnBlueprintUse;
                    break;
                case "On_DoorUse":
                    Hooks.OnDoorUse -= OnDoorUse;
                    break;
                case "On_PlayerTeleport":
                    Hooks.OnPlayerTeleport -= OnPlayerTeleport;
                    break;
                case "On_Crafting":
                    Hooks.OnCrafting -= OnCrafting;
                    break;
                case "On_ResourceSpawn":
                    Hooks.OnResourceSpawned -= OnResourceSpawned;
                    break;
                case "On_ItemAdded":
                    Hooks.OnItemAdded -= OnItemAdded;
                    break;
                case "On_ItemRemoved":
                    Hooks.OnItemRemoved -= OnItemRemoved;
                    break;
                case "On_Airdrop":
                    Hooks.OnAirdropCalled -= OnAirdrop;
                    break;
                /*case "On_AirdropCrateDropped":
                    Hooks.OnAirdropCrateDropped -= OnAirdropCrateDropped;
                    break;*/
                case "On_SteamDeny":
                    Hooks.OnSteamDeny -= OnSteamDeny;
                    break;
                case "On_PlayerApproval":
                    Hooks.OnPlayerApproval -= OnPlayerApproval;
                    break;
                case "On_Research":
                    Hooks.OnResearch -= OnResearch;
                    break;
                case "On_ServerSaved":
                    Hooks.OnServerSaved -= OnServerSaved;
                    break;
                case "On_AllPluginsLoaded":
                    JintPluginModule.OnAllLoaded -= OnAllLoaded;
                    break;
                case "On_VoiceChat":
                    Hooks.OnShowTalker -= OnShowTalker;
                    break;
                case "On_ItemPickup":
                    Hooks.OnItemPickup -= OnItemPickup;
                    break;
                case "On_FallDamage":
                    Hooks.OnFallDamage -= OnFallDamage;
                    break;
                case "On_LootUse":
                    Hooks.OnLootUse -= OnLootUse;
                    break;
                case "On_PlayerBan":
                    Hooks.OnPlayerBan -= OnBanEvent;
                    break;
                case "On_RepairBench":
                    Hooks.OnRepairBench -= OnRepairBench;
                    break;
                case "On_ItemMove":
                    Hooks.OnItemMove -= OnItemMove;
                    break;
                }
            }
        }

        #region File operations.

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private string ValidateRelativePath(string path)
        {
            string normalizedPath = NormalizePath(Path.Combine(RootDirectory.FullName, path));
            string rootDirNormalizedPath = NormalizePath(RootDirectory.FullName);

            if (!normalizedPath.StartsWith(rootDirNormalizedPath))
                return null;

            return normalizedPath;
        }

        public bool CreateDir(string path)
        {
            try {
                path = ValidateRelativePath(path);
                if (path == null)
                    return false;

                if (Directory.Exists(path))
                    return true;

                Directory.CreateDirectory(path);
                return true;
            } catch { }

            return false;
        }

        public void ToJsonFile(string path, string json)
        {
            path = ValidateRelativePath(path + ".json");
            File.WriteAllText(path, json);
        }

        public string FromJsonFile(string path)
        {
            string json = string.Empty;
            path = ValidateRelativePath(path + ".json");
            if (File.Exists(path))
                json = File.ReadAllText(path);

            return json;
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
            {
                return;
            }
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

        public void RotateLog(string logfile, int max = 6)
        {
            logfile = ValidateRelativePath(logfile + ".log");
            if (logfile == null)
                return;

            string pathh, pathi;
            int i, h;
            for (i = max, h = i - 1; i > 1; i--, h--)
            {
                pathi = ValidateRelativePath(logfile + i + ".log");
                pathh = ValidateRelativePath(logfile + h + ".log");

                try {
                    if (!File.Exists(pathi))
                        File.Create(pathi);

                    if (!File.Exists(pathh))
                    {
                        File.Replace(logfile, pathi, null);
                    } else
                    {
                        File.Replace(pathh, pathi, null);
                    }
                } catch (Exception ex) {
                    Logger.LogError("[JintPlugin] RotateLog " + logfile + ", " + pathh + ", " + pathi + ", " + ex);
                    continue;
                }
            }
        }

        #endregion

        #region Timer functions.

        public TimedEvent GetTimer(string name)
        {
            if (Timers.ContainsKey(name))
                return Timers[name];
            return null;
        }

        public TimedEvent CreateTimer(string name, int timeoutDelay)
        {
            TimedEvent timer = this.GetTimer(name);
            if (timer == null) {
                timer = new TimedEvent(name, (double)timeoutDelay);
                timer.OnFire += OnTimerCB;
                Timers[name] = timer;
                return timer;
            }
            return timer;
        }

        public TimedEvent CreateTimer(string name, int timeoutDelay, List<object> args)
        {
            TimedEvent timer = CreateTimer(name, timeoutDelay);
            timer.Args = args.ToArray<object>();
            timer.OnFire -= OnTimerCB;
            timer.OnFireArgs += OnTimerCBArgs;
            return timer;
        }

        public void KillTimer(string name)
        {
            TimedEvent timer = GetTimer(name);
            if (timer != null) {
                timer.Stop();
                Timers.Remove(name);
            }
        }

        public void KillTimers()
        {
            foreach (var timer in Timers.Values)
                timer.Stop();
            Timers.Clear();
        }

        #endregion Timer functions.

        #region Other functions.
        public Fougerite.Player PlayerByGameID(string uid)
        {
            return Fougerite.Player.FindByGameID(uid);
        }

        public Fougerite.Player PlayerByName(string name)
        {
            return Fougerite.Player.FindByName(name);
        }

        public string Today
        {
            get
            {
                return DateTime.Now.ToShortDateString();
            }
        }

        public int Ticks
        {
            get
            {
                return Environment.TickCount;
            }
        }

        public float Uptime
        {
            get
            {
                return UnityEngine.Time.realtimeSinceStartup;
            }
        }

        public string ClockTime
        {
            get
            {
                return DateTime.Now.ToShortTimeString();
            }
        }

        public int Timestamp
        {
            get
            {
                return POSIX.Time.NowStamp;
            }
        }

        public int TimeSince(int when)
        {
            return POSIX.Time.ElapsedStampSince(when);
        }

        public List<string> CreateList()
        {
            return new List<string>();
        }

        public Dictionary<string, object> CreateDict()
        {
            return new Dictionary<string, object>();
        }

        #endregion

        #region Web

        public string GET(string url)
        {
            using (System.Net.WebClient client = new System.Net.WebClient()) {
                return client.DownloadString(url);
            }
        }

        public string POSTJson(string url, string json)
        {
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                byte[] bytes = client.UploadData(url, "POST", Encoding.UTF8.GetBytes(json));
                return Encoding.UTF8.GetString(bytes);
            }
        }

        public string POSTJsonFile(string url, string path)
        {
            path = ValidateRelativePath(path + ".json");
            if (!File.Exists(path)) {
                Logger.LogError(string.Format("{0} JsonFile not found: {1}", brktname, path));
                return null;
            }

            string json = File.ReadAllText(path);
            using (WebClient client = new WebClient()) {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                byte[] bytes = client.UploadData(url, "POST", Encoding.UTF8.GetBytes(json));
                return Encoding.UTF8.GetString(bytes);
            }
        }

        #endregion

        #region Hooks

        public void OnAllLoaded()
        {
            Invoke("On_AllPluginsLoaded");
        }

        public void OnBlueprintUse(Player player, BPUseEvent evt)
        {
            Invoke("On_BlueprintUse", player, evt);
        }

        public void OnChat(Player player, ref ChatString text)
        {
            Invoke("On_Chat", player, text);
        }

        public void OnCommand(Player player, string command, string[] args)
        {
            if (CommandList.Count != 0 && !CommandList.Contains(command) && !Fougerite.Server.ForceCallForCommands.Contains(command)) { return; }
            Invoke("On_Command", player, command, args);
        }

        public void OnConsole(ref ConsoleSystem.Arg arg, bool external)
        {
            Player player = Player.FindByPlayerClient(arg.argUser.playerClient);

            if (!external)
                Invoke("On_Console", player, arg);
            else
                Invoke("On_Console", null, arg);
        }

        public void OnCrafting(CraftingEvent e)
        {
            Invoke("On_Crafting", e);
        }

        public void OnDoorUse(Player player, DoorEvent evt)
        {
            Invoke("On_DoorUse", player, evt);
        }

        public void OnEntityDecay(DecayEvent evt)
        {
            Invoke("On_EntityDecay", evt);
        }

        public void OnEntityDeployed(Player player, Entity entity)
        {
            Invoke("On_EntityDeployed", player, entity);
        }

        public void OnEntityDeployed2(Player player, Entity entity, Fougerite.Player actualplacer)
        {
            Invoke("On_EntityDeployed", player, entity, actualplacer);
        }

        public void OnEntityHurt(HurtEvent evt)
        {
            Invoke("On_EntityHurt", evt);
        }

        public void OnItemsLoaded(ItemsBlocks items)
        {
            Invoke("On_ItemsLoaded", items);
        }

        public void OnNPCHurt(HurtEvent evt)
        {
            Invoke("On_NPCHurt", evt);
        }

        public void OnNPCKilled(DeathEvent evt)
        {
            Invoke("On_NPCKilled", evt);
        }

        public void OnPlayerConnected(Player player)
        {
            Invoke("On_PlayerConnected", player);
        }

        public void OnPlayerDisconnected(Player player)
        {
            Invoke("On_PlayerDisconnected", player);
        }

        public void OnPlayerGathering(Player player, GatherEvent evt)
        {
            Invoke("On_PlayerGathering", player, evt);
        }

        public void OnPlayerHurt(HurtEvent evt)
        {
            Invoke("On_PlayerHurt", evt);
        }

        public void OnPlayerKilled(DeathEvent evt)
        {
            Invoke("On_PlayerKilled", evt);
        }

        public void OnPlayerTeleport(Fougerite.Player player, Vector3 from, Vector3 to)
        {
            Invoke("On_PlayerTeleport", player, from, to);
        }

        public void OnPlayerSpawn(Player player, SpawnEvent evt)
        {
            Invoke("On_PlayerSpawning", player, evt);
        }

        public void OnPlayerSpawned(Player player, SpawnEvent evt)
        {
            Invoke("On_PlayerSpawned", player, evt);
        }

        public void OnResearch(ResearchEvent evt)
        {
            Invoke("On_Research", evt);
        }

        public void OnResourceSpawned(ResourceTarget t)
        {
            Invoke("On_ResourceSpawn", t);
        }

        public void OnItemAdded(InventoryModEvent e)
        {
            Invoke("On_ItemAdded", e);
        }

        public void OnItemRemoved(InventoryModEvent e)
        {
            Invoke("On_ItemRemoved", e);
        }

        public void OnFallDamage(FallDamageEvent e)
        {
            Invoke("On_FallDamage",  e);
        }

        public void OnAirdrop(Vector3 v)
        {
            Invoke("On_Airdrop", v);
        }

        /*public void OnAirdropCrateDropped(GameObject go)
        {
            Invoke("On_AirdropCrateDropped", go);
        }*/

        public void OnSteamDeny(SteamDenyEvent e)
        {
            Invoke("On_SteamDeny", e);
        }

        public void OnPlayerApproval(PlayerApprovalEvent e)
        {
            Invoke("On_PlayerApproval", e);
        }

        public void OnPluginInit()
        {
            Invoke("On_PluginInit");
        }

        public void OnServerInit()
        {
            Invoke("On_ServerInit");
        }

        public void OnServerShutdown()
        {
            Invoke("On_ServerShutdown");
        }

        public void OnServerSaved()
        {
            Invoke("On_ServerSaved");
        }

        public void OnTablesLoaded(Dictionary<string, LootSpawnList> lists)
        {
            Invoke("On_TablesLoaded", lists);
        }

        public void OnShowTalker(uLink.NetworkPlayer np, Fougerite.Player player)
        {
            Invoke("On_VoiceChat", np, player);
        }

        public void OnItemPickup(ItemPickupEvent e)
        {
            Invoke("On_ItemPickup", e);
        }

        public void OnLootUse(LootStartEvent le)
        {
            Invoke("On_LootUse", le);
        }

        public void OnBanEvent(BanEvent be)
        {
            Invoke("On_PlayerBan", be);
        }

        public void OnRepairBench(Fougerite.Events.RepairEvent be)
        {
            Invoke("On_RepairBench", be);
        }

        public void OnItemMove(ItemMoveEvent be)
        {
            Invoke("On_ItemMove", be);
        }

        public void OnPluginShutdown()
        {
            try
            {
                Invoke("On_PluginShutdown");
            }
            catch { }
        }

        public void OnTimerCB2(JintTE evt)
        {
            try
            {
                Invoke(evt.Name + "Callback", evt);
            }
            catch (Exception ex)
            {
                Fougerite.Logger.LogError("Failed to invoke callback " + evt.Name + " Ex: " + ex);
            }
        }

        public void OnTimerCB(string name)
        {
            if (Code.Contains(name + "Callback"))
                Invoke(name + "Callback");
        }

        public void OnTimerCBArgs(string name, object[] args)
        {
            if (Code.Contains(name + "Callback"))
                Invoke(name + "Callback", args);
        }

        #endregion
    }
}