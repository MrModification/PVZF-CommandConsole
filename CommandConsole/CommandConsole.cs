using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.Injection;
using MelonLoader;
using System.Collections.Generic;
using UnityEngine;
using static Il2Cpp.HorseBoss;

namespace CheatConsole
{
    // ---------------------------------------------------------
    // LOG ENTRY (colored log lines)
    // ---------------------------------------------------------
    public class LogEntry
    {
        public string Text;
        public Color Color;

        public LogEntry(string text, Color color)
        {
            Text = text;
            Color = color;
        }
    }

    // ---------------------------------------------------------
    // MOD LOGGING API
    // ---------------------------------------------------------
    public static class CheatConsoleLog
    {
        public static void Msg(string msg)
        {
            if (CheatConsoleManager.Instance != null)
                CheatConsoleManager.Instance.LogEntries.Add(new LogEntry(msg, new Color(0.75f, 0.75f, 0.75f, 1f)));//silver
            TrimLog();
        }
        public static void Info(string msg)
        {
            if (CheatConsoleManager.Instance != null)
                CheatConsoleManager.Instance.LogEntries.Add(new LogEntry(msg, Color.cyan));//blue
            TrimLog();
        }

        public static void Warn(string msg)
        {
            if (CheatConsoleManager.Instance != null)
                CheatConsoleManager.Instance.LogEntries.Add(new LogEntry(msg, Color.yellow));//yellow
            TrimLog();
        }

        public static void Error(string msg)
        {
            if (CheatConsoleManager.Instance != null)
                CheatConsoleManager.Instance.LogEntries.Add(new LogEntry(msg, Color.red));//red
            TrimLog();
        }

        public static void BuiltIn(string msg)
        {
            if (CheatConsoleManager.Instance != null)
                CheatConsoleManager.Instance.LogEntries.Add(new LogEntry(msg, Color.white));//white
            TrimLog();
        }
        public static void TrimLog()
        {
            if (CheatConsoleManager.Instance == null)
                return;

            var list = CheatConsoleManager.Instance.LogEntries;
            int maxCount = 500;
            if (list.Count > maxCount)
            {
                int removeCount = list.Count - maxCount;
                list.RemoveRange(0, removeCount);
            }
        }
    }

    // ---------------------------------------------------------
    // CHEAT DEFINITION
    // ---------------------------------------------------------
    public class CheatDefinition
    {
        public string Command;
        public string HelpText;
        public string FullDefinition;
        public System.Action<string[]> Action;

        public CheatDefinition(string cmd, string help, string full, System.Action<string[]> act)
        {
            Command = cmd;
            HelpText = help;
            FullDefinition = full;
            Action = act;
        }
    }

    // ---------------------------------------------------------
    // ALIAS REGISTRY
    // ---------------------------------------------------------
    public static class AliasRegistry
    {
        // alias -> main
        public static readonly Dictionary<string, string> AliasToMain = new Dictionary<
            string,
            string
        >();

        // main -> list of aliases
        public static readonly Dictionary<string, List<string>> MainToAliases = new Dictionary<
            string,
            List<string>
        >();

        public static void AddAlias(string main, string alias)
        {
            if (string.IsNullOrEmpty(main) || string.IsNullOrEmpty(alias))
                return;

            main = main.ToLower();
            alias = alias.ToLower();

            if (alias == main)
                return;

            AliasToMain[alias] = main;

            List<string> list;
            if (!MainToAliases.TryGetValue(main, out list))
            {
                list = new List<string>();
                MainToAliases[main] = list;
            }

            if (!list.Contains(alias))
                list.Add(alias);
        }

        public static string Resolve(string command)
        {
            command = command.ToLower();
            string main;
            if (AliasToMain.TryGetValue(command, out main))
                return main;
            return command;
        }

        public static List<string> GetAliases(string main)
        {
            main = main.ToLower();
            List<string> list;
            if (MainToAliases.TryGetValue(main, out list))
                return list;
            return null;
        }
    }

    // ---------------------------------------------------------
    // OG CHEAT DESCRIPTIONS
    // ---------------------------------------------------------
    public static class OGCheatDescriptions
    {
        public static readonly Dictionary<string, string> Descriptions = new Dictionary<
            string,
            string
        >()
        {
            {
                "cheatmode",
                "Enables/Disables developer mode (Instant CD, Unlimited Sun & Coins, Nyan Squash spawns Spicy Squashes)."
            },
            { "kill", "Summons an Inferno Meteor to decimate the lawn, or plants, zombies if specified" },
            { "clearplant", "Removes all plants from the lawn." },
            { "clearzombie", "Removes all zombies from the lawn." },
            { "irwinner", "Instantly wins the current level." },
            { "moresun", "Gains 5,000 Sun." },
            { "mysmoney", "Gains $1,000,000 for Harvest Mode." },
            { "givecard", "Spawns a random Seed Packet for Harvest Mode" },
            { "bigcannon", "Enables/Disables a Wall-nut Turret. Left Click to shoot." },
            { "upup", "Instantly levels-up your Chibi Mini-bosses." },
            { "debug", "Enables/Disables debug mode" },
            { "reload", "Reloads all Fusion Showcase levels." },
            { "report", "Exports the current level's statistics."}
        };
    }

    // ---------------------------------------------------------
    // CHEAT API
    // ---------------------------------------------------------
    public static class CheatAPI
    {
        private static readonly Dictionary<string, CheatDefinition> _cheats = new Dictionary<
            string,
            CheatDefinition
        >();

        public static void Register(
            string command,
            string help,
            string full,
            System.Action<string[]> action
        )
        {
            command = command.ToLower();
            _cheats[command] = new CheatDefinition(command, help, full, action);
            MelonLogger.Msg("[CheatAPI] Registered cheat: " + command);
        }

        public static bool TryGet(string command, out CheatDefinition def)
        {
            return _cheats.TryGetValue(command.ToLower(), out def);
        }

        public static IEnumerable<CheatDefinition> All()
        {
            foreach (KeyValuePair<string, CheatDefinition> kv in _cheats)
                yield return kv.Value;
        }
    }

    // ---------------------------------------------------------
    // PARSING HELPERS
    // ---------------------------------------------------------
    public static class CheatParse
    {
        public static bool TryFloat(string s, out float f)
        {
            return float.TryParse(
                s,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out f
            );
        }

        public static bool TryInt(string s, out int i)
        {
            return int.TryParse(s, out i);
        }

        public static bool TryEnum<T>(string s, out T value) where T : struct
        {
            return System.Enum.TryParse<T>(s, true, out value);
        }
    }

    // ---------------------------------------------------------
    // CHEAT CONSOLE MANAGER (IL2CPP-COMPATIBLE MONOBEHAVIOUR)
    // ---------------------------------------------------------
    public class CheatConsoleManager : MonoBehaviour
    {
        public static CheatConsoleManager Instance;

        private int EmptyBackspaceCount = 0;

        public string CurrentSuggestion = "";
        public bool HasSuggestion = false;

        public bool ConsoleOpen = false;
        public string CurrentInput = "";
        public List<LogEntry> LogEntries = new List<LogEntry>();

        public List<string> History = new List<string>();
        public int HistoryIndex = -1;

        public List<string> AutoMatches = new List<string>();
        public int AutoIndex = 0;

        public Vector2 ScrollPos = Vector2.zero;
        public CheatConsoleManager(System.IntPtr ptr) : base(ptr) { }

        public CheatConsoleManager()
            : base(ClassInjector.DerivedConstructorPointer<CheatConsoleManager>())
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        private void Awake()
        {
            Instance = this;
            CommandScriptManager.Initialize();
        }

        private void UpdateGhostSuggestion()
        {
            string inputLower = CurrentInput.ToLower();
            if (string.IsNullOrEmpty(inputLower))
            {
                HasSuggestion = false;
                CurrentSuggestion = "";
                return;
            }

            foreach (CheatDefinition def in CheatAPI.All())
            {
                if (def.Command.StartsWith(inputLower))
                {
                    CurrentSuggestion = def.Command;
                    HasSuggestion = true;
                    return;
                }

                var aliases = AliasRegistry.GetAliases(def.Command);
                if (aliases != null)
                {
                    for (int i = 0; i < aliases.Count; i++)
                    {
                        string alias = aliases[i];
                        if (alias.StartsWith(inputLower))
                        {
                            CurrentSuggestion = alias;
                            HasSuggestion = true;
                            return;
                        }
                    }
                }
            }

            HasSuggestion = false;
            CurrentSuggestion = "";
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.BackQuote))
            {
                ConsoleOpen = !ConsoleOpen;
                if (!ConsoleOpen)
                {
                    CurrentInput = "";
                    HasSuggestion = false;
                    CurrentSuggestion = "";
                }
            }

            if (!ConsoleOpen)
                return;

            // History navigation
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (History.Count > 0 && HistoryIndex > 0)
                {
                    HistoryIndex--;
                    CurrentInput = History[HistoryIndex];
                }
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (History.Count > 0 && HistoryIndex < History.Count - 1)
                {
                    HistoryIndex++;
                    CurrentInput = History[HistoryIndex];
                }
                else
                {
                    HistoryIndex = History.Count;
                    CurrentInput = "";
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (HasSuggestion)
                {
                    CurrentInput = CurrentSuggestion;
                    HasSuggestion = false;
                    CurrentSuggestion = "";
                }
            }

            foreach (char c in Input.inputString)
            {
                if (CurrentInput == "`")
                {
                    CurrentInput = "";
                }

                // Backspace
                if (c == '\b')
                {
                    if (CurrentInput.Length > 0)
                    {
                        CurrentInput = CurrentInput.Substring(0, CurrentInput.Length - 1);
                        EmptyBackspaceCount = 0;
                    }
                    else
                    {
                        EmptyBackspaceCount++;

                        if (EmptyBackspaceCount >= 3)
                        {
                            LogEntries.Clear();
                            EmptyBackspaceCount = 0;
                        }
                    }
                    continue;
                }

                // Enter
                if (c == '\n' || c == '\r')
                {
                    ExecuteCommand(CurrentInput);
                    CurrentInput = "";
                    HasSuggestion = false;
                    CurrentSuggestion = "";
                    continue;
                }

                // Normal characters
                CurrentInput += c;
                HasSuggestion = false;
                CurrentSuggestion = "";
            }
            UpdateGhostSuggestion();
        }

        public void ExecuteCommand(string cmd)
        {
            CheatConsoleLog.BuiltIn("> " + cmd);

            if (!string.IsNullOrWhiteSpace(cmd))
            {
                History.Add(cmd);
                HistoryIndex = History.Count;
            }

            // runscript command
            if (cmd.StartsWith("runscript "))
            {
                string name = cmd.Substring("runscript ".Length).Trim();
                ScriptEngine.RunScript(name);
                return;
            } else
            {
            // Normal cheat commands
                CheatConsoleRouter.RunCheat(cmd);
            }
        }

        public static class CheatPreview
        {
            public static string GetPreview(string input)
            {
                if (string.IsNullOrWhiteSpace(input))
                    return "";

                string[] parts = input.Split(' ');
                string cmd = parts[0].ToLower();

                // Resolve alias → main command
                cmd = AliasRegistry.Resolve(cmd);

                CheatDefinition def;
                if (CheatAPI.TryGet(cmd, out def))
                {
                    return def.Command + " — " + def.HelpText;
                }

                return "";
            }
        }

        private void OnGUI()
        {
            if (!ConsoleOpen)
                return;

            // Background
            GUI.Box(new Rect(20, 20, 600, 360), "Cheat Console");

            // Log area
            int lineHeight = 18;
            int maxLines = 12;
            int startX = 30;
            int startY = 50;
            int maxWidth = 580;

            int total = LogEntries.Count;
            int firstVisible = Mathf.Max(0, total - maxLines);

            int y = startY;
            for (int i = firstVisible; i < total; i++)
            {
                LogEntry entry = LogEntries[i];
                GUI.contentColor = entry.Color;
                GUI.Label(new Rect(startX, y, maxWidth, lineHeight), entry.Text);
                y += lineHeight;
            }

            GUI.contentColor = Color.white;

            // Command preview (green)
            string previewSource = HasSuggestion ? CurrentSuggestion : CurrentInput;
            string preview = CheatPreview.GetPreview(previewSource);

            if (!string.IsNullOrEmpty(preview))
            {
                GUI.contentColor = Color.green;
                GUI.Label(new Rect(30, 285, 580, 20), preview);
                GUI.contentColor = Color.white;
            }

            // Ghost autocomplete preview
            if (HasSuggestion && CurrentSuggestion.Length > CurrentInput.Length)
            {
                string typed = CurrentInput;
                string rest = CurrentSuggestion.Substring(CurrentInput.Length);

                // Measure typed text INCLUDING prefix
                string typedWithPrefix = "> " + typed;
                float typedWidth = GUI.skin.label.CalcSize(new GUIContent(typedWithPrefix)).x;

                // Draw ghost remainder (gray)
                GUI.contentColor = Color.gray;
                GUI.Label(new Rect(35 + typedWidth, 297, 580, 20), rest);
                GUI.contentColor = Color.white;
            }

            // Typed text==
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(35, 297, 570, 25), "> " + CurrentInput);
            GUI.contentColor = Color.white;
        }
    }

    // ---------------------------------------------------------
    // ROUTER — splits command + args, resolves aliases
    // ---------------------------------------------------------
    public static class CheatConsoleRouter
    {
        public static void RunCheat(string cmd)
        {
            if (string.IsNullOrWhiteSpace(cmd))
                return;

            string[] parts = cmd.Split(' ');
            string command = parts[0].ToLower();
            string[] args = new string[parts.Length - 1];

            for (int i = 1; i < parts.Length; i++)
                args[i - 1] = parts[i];

            // Special "kill" command is not an alias
            if (command == "kill")
            {
                RunKill(args);
                return;
            }

            // Resolve alias → main command
            string mainCommand = AliasRegistry.Resolve(command);

            CheatDefinition def;
            if (CheatAPI.TryGet(mainCommand, out def))
            {
                def.Action.Invoke(args);
            }
            else
            {
                CheatConsoleLog.Warn("Unknown command: " + command);
            }
        }

        private static void RunKill(string[] args)
        {
            // Behavior:
            // kill           -> summon meteor
            // kill all       -> clearplant + clearzombie
            // kill plant(s)  -> clearplant
            // kill zombie(s) -> clearzombie

            string mode = "";
            if (args.Length > 0)
                mode = args[0].ToLower();

            Il2Cpp.CheatKey cheatKey = UnityEngine.Object.FindObjectOfType<Il2Cpp.CheatKey>();
            if (cheatKey == null || cheatKey.CheatKeys == null)
            {
                CheatConsoleLog.Error("CheatKey not found; kill command unavailable");
                return;
            }

            Il2CppSystem.Action clearPlant;
            Il2CppSystem.Action clearZombie;
            Il2CppSystem.Action kill;

            cheatKey.CheatKeys.TryGetValue("clearplant", out clearPlant);
            cheatKey.CheatKeys.TryGetValue("clearzombie", out clearZombie);
            cheatKey.CheatKeys.TryGetValue("kill", out kill);

            if (mode == "all")
            {
                if (clearPlant != null)
                    clearPlant.Invoke();
                if (clearZombie != null)
                    clearZombie.Invoke();

                CheatConsoleLog.Info("Killed all plants and zombies");
                return;
            }

            if (mode == "plant" || mode == "plants")
            {
                if (clearPlant != null)
                {
                    clearPlant.Invoke();
                    CheatConsoleLog.Info("Killed all plants");
                }
                else
                {
                    CheatConsoleLog.Error("Original cheat 'clearplant' not found");
                }
                return;
            }

            if (mode == "zombie" || mode == "zombies")
            {
                if (clearZombie != null)
                {
                    clearZombie.Invoke();
                    CheatConsoleLog.Info("Killed all zombies");
                }
                else
                {
                    CheatConsoleLog.Error("Original cheat 'clearzombie' not found");
                }
                return;
            }

            if (mode == "")
            {
                kill.Invoke();
                CheatConsoleLog.Info("Summoned an Inferno Meteor");
            }
        }
    }

    // ---------------------------------------------------------
    // PATCH: Disable CheatKey
    // ---------------------------------------------------------
    [HarmonyPatch(typeof(Il2Cpp.CheatKey), "CheckCheatCodes")]
    public static class Patch_CheatKey_CheckCheatCodes_Gate
    {
        static bool Prefix()
        {
            return false;
        }
    }

    // ---------------------------------------------------------
    // PATCH: Inject cheats during first CheckCheatCodes call
    // ---------------------------------------------------------
    [HarmonyPatch(typeof(Il2Cpp.CheatKey), "CheckCheatCodes")]
    public static class Patch_CheatKey_InjectCheats
    {
        private static bool initialized = false;

        static void Prefix(Il2Cpp.CheatKey __instance)
        {
            if (initialized)
                return;

            initialized = true;

            try
            {
                // Mirror built-in cheats into CheatAPI with descriptions
                var dict = __instance.CheatKeys;
                if (dict != null)
                {
                    foreach (var kv in dict)
                    {
                        string key = kv.Key.ToLower();
                        string desc;
                        if (!OGCheatDescriptions.Descriptions.TryGetValue(key, out desc))
                            desc = "Built-in cheat";

                        CheatAPI.Register(
                            key,
                            desc,
                            desc,
                            delegate (string[] a)
                            {
                                kv.Value.Invoke();
                            }
                        );
                    }

                    CheatConsoleLog.Info("Imported built-in CheatKey cheats");
                }

                // ---------------- ALIASES FOR PREBUILT CHEATS ----------------

                // cheatmode aliases: dev, devmode, god
                AliasRegistry.AddAlias("cheatmode", "dev");
                AliasRegistry.AddAlias("cheatmode", "devmode");
                AliasRegistry.AddAlias("cheatmode", "god");

                // irwinner alias: win
                AliasRegistry.AddAlias("irwinner", "win");

                // spawnplant alias: plant
                AliasRegistry.AddAlias("spawnplant", "plant");

                // spawnzombie aliases: zombie, zom
                AliasRegistry.AddAlias("spawnzombie", "zombie");
                AliasRegistry.AddAlias("spawnzombie", "zom");

                // ---------------------------------------------------------
                // RUN SCRIPT
                // ---------------------------------------------------------

                CheatAPI.Register(
                    "runscript",
                    "runscript <name> — runs a loaded script",
                    "Executes a script previously loaded by ScriptEngine.",
                    delegate (string[] args)
                    {
                        if (args.Length < 1)
                        {
                            CheatConsoleLog.Warn("Usage: runscript <name>");
                            return;
                        }

                        ScriptEngine.RunScript(args[0]);
                    }
                );

                // ---------------------------------------------------------
                // HELP COMMAND
                // ---------------------------------------------------------

                CheatAPI.Register(
                    "help",
                    "help — list commands\nhelp <cmd>\nhelp alias <alias>\nhelp plantlist\nhelp zombielist",
                    "Shows help for commands, aliases, plants, and zombies.",
                    delegate (string[] args)
                    {
                        // help
                        if (args.Length == 0)
                        {
                            foreach (CheatDefinition def in CheatAPI.All())
                            {
                                CheatConsoleLog.BuiltIn(def.Command + " — " + def.HelpText);

                                List<string> aliasList = AliasRegistry.GetAliases(def.Command);
                                if (aliasList != null && aliasList.Count > 0)
                                {
                                    string aliasLine = "    Aliases: ";
                                    for (int i = 0; i < aliasList.Count; i++)
                                    {
                                        if (i > 0)
                                            aliasLine += ", ";
                                        aliasLine += aliasList[i];
                                    }
                                    CheatConsoleLog.BuiltIn(aliasLine);
                                }
                            }
                            return;
                        }

                        // help alias <aliasname>
                        if (args.Length == 2 && args[0].ToLower() == "alias")
                        {
                            string alias = args[1].ToLower();

                            if (!AliasRegistry.AliasToMain.TryGetValue(alias, out string main))
                            {
                                CheatConsoleLog.Warn("No command found for alias: " + alias);
                                return;
                            }

                            if (!CheatAPI.TryGet(main, out CheatDefinition def))
                            {
                                CheatConsoleLog.Warn("No command found for alias: " + alias);
                                return;
                            }

                            CheatConsoleLog.BuiltIn(def.Command + " — " + def.HelpText);

                            List<string> aliasList = AliasRegistry.GetAliases(def.Command);
                            if (aliasList != null && aliasList.Count > 0)
                            {
                                string line = "Aliases: ";
                                for (int i = 0; i < aliasList.Count; i++)
                                {
                                    if (i > 0)
                                        line += ", ";
                                    line += aliasList[i];
                                }
                                CheatConsoleLog.BuiltIn(line);
                            }

                            if (!string.IsNullOrEmpty(def.FullDefinition))
                                CheatConsoleLog.BuiltIn(def.FullDefinition);

                            return;
                        }

                        // help plantlist
                        if (args.Length == 1 && args[0].ToLower() == "plantlist")
                        {
                            foreach (PlantType p in System.Enum.GetValues(typeof(PlantType)))
                                CheatConsoleLog.BuiltIn(p.ToString());
                            return;
                        }

                        // help zombielist
                        if (args.Length == 1 && args[0].ToLower() == "zombielist")
                        {
                            foreach (ZombieType z in System.Enum.GetValues(typeof(ZombieType)))
                                CheatConsoleLog.BuiltIn(z.ToString());
                            return;
                        }

                        // help <commandname>
                        if (args.Length == 1)
                        {
                            string cmd = args[0].ToLower();

                            // Try main command
                            if (!CheatAPI.TryGet(cmd, out CheatDefinition def))
                            {
                                // Try alias
                                if (AliasRegistry.AliasToMain.TryGetValue(cmd, out string main))
                                {
                                    if (!CheatAPI.TryGet(main, out def))
                                    {
                                        CheatConsoleLog.Warn("No help found for: " + cmd);
                                        return;
                                    }
                                }
                                else
                                {
                                    CheatConsoleLog.Warn("No help found for: " + cmd);
                                    return;
                                }
                            }

                            CheatConsoleLog.BuiltIn(def.Command + " — " + def.HelpText);

                            List<string> aliasList = AliasRegistry.GetAliases(def.Command);
                            if (aliasList != null && aliasList.Count > 0)
                            {
                                string line = "Aliases: ";
                                for (int i = 0; i < aliasList.Count; i++)
                                {
                                    if (i > 0)
                                        line += ", ";
                                    line += aliasList[i];
                                }
                                CheatConsoleLog.BuiltIn(line);
                            }

                            if (!string.IsNullOrEmpty(def.FullDefinition))
                                CheatConsoleLog.BuiltIn(def.FullDefinition);

                            return;
                        }

                        // Unknown help syntax
                        CheatConsoleLog.Warn("Unknown help format.");
                    }
                );

                // ---------------------------------------------------------
                // SEARCH COMMANDS
                // ---------------------------------------------------------
                CheatAPI.Register(
                    "search",
                    "search <term> — search plants and zombies",
                    "Searches both PlantType and ZombieType enums.",
                    delegate (string[] args)
                    {
                        if (args.Length < 1)
                        {
                            CheatConsoleLog.Warn("Usage: search <term>");
                            return;
                        }

                        string term = args[0].ToLower();
                        foreach (PlantType p in System.Enum.GetValues(typeof(PlantType)))
                        {
                            string name = p.ToString();
                            if (name.ToLower().Contains(term))
                            {
                                int id = (int)p;
                                CheatConsoleLog.BuiltIn($"[{id}] {name}");
                            }
                        }

                        foreach (ZombieType z in System.Enum.GetValues(typeof(ZombieType)))
                        {
                            string name = z.ToString();
                            if (name.ToLower().Contains(term))
                            {
                                int id = (int)z;
                                CheatConsoleLog.BuiltIn($"[{id}] {name}");
                            }
                        }
                    }
                );

                CheatAPI.Register(
                    "searchplant",
                    "searchplant <term> — search plant types",
                    "Searches only PlantType enum.",
                    delegate (string[] args)
                    {
                        if (args.Length < 1)
                        {
                            CheatConsoleLog.Warn("Usage: searchplant <term>");
                            return;
                        }

                        string term = args[0].ToLower();

                        foreach (PlantType p in System.Enum.GetValues(typeof(PlantType)))
                        {
                            string name = p.ToString();
                            if (name.ToLower().Contains(term))
                            {
                                int id = (int)p;
                                CheatConsoleLog.BuiltIn($"[{id}] {name}");
                            }
                        }
                    }
                );

                CheatAPI.Register(
                    "searchzombie",
                    "searchzombie <term> — search zombie types",
                    "Searches only ZombieType enum.",
                    delegate (string[] args)
                    {
                        if (args.Length < 1)
                        {
                            CheatConsoleLog.Warn("Usage: searchzombie <term>");
                            return;
                        }

                        string term = args[0].ToLower();

                        foreach (ZombieType z in System.Enum.GetValues(typeof(ZombieType)))
                        {
                            string name = z.ToString();
                            if (name.ToLower().Contains(term))
                            {
                                int id = (int)z;
                                CheatConsoleLog.BuiltIn($"[{id}] {name}");
                            }
                        }
                    }
                );

                // ---------------------------------------------------------
                // SUN COMMAND
                // ---------------------------------------------------------
                CheatAPI.Register(
                    "sun",
                    "sun <amount> — gives sun",
                    "Adds the specified amount of sun using Board.GetSun(amount).",
                    delegate (string[] args)
                    {
                        if (args.Length < 1)
                        {
                            CheatConsoleLog.Warn("Usage: sun <amount>");
                            return;
                        }

                        int amount;
                        if (!CheatParse.TryInt(args[0], out amount))
                        {
                            CheatConsoleLog.Error("Invalid sun amount");
                            return;
                        }

                        Board board = UnityEngine.Object.FindObjectOfType<Board>();
                        if (board == null)
                        {
                            CheatConsoleLog.Error("Board not found");
                            return;
                        }

                        board.GetSun(amount, 1, true);
                        CheatConsoleLog.Info("Gave sun: " + amount);
                    }
                );

                // ---------------------------------------------------------
                // SETSUN COMMAND
                // ---------------------------------------------------------
                CheatAPI.Register(
                    "setsun",
                    "setsun <amount> — sets exact sun value",
                    "Directly sets the player's sun using Board.SetSun(amount).",
                    delegate (string[] args)
                    {
                        if (args.Length < 1)
                        {
                            CheatConsoleLog.Warn("Usage: setsun <amount>");
                            return;
                        }

                        int amount;
                        if (!CheatParse.TryInt(args[0], out amount))
                        {
                            CheatConsoleLog.Error("Invalid sun amount");
                            return;
                        }

                        Board board = UnityEngine.Object.FindObjectOfType<Board>();
                        if (board == null)
                        {
                            CheatConsoleLog.Error("Board not found");
                            return;
                        }

                        board.SetSun(amount);
                        CheatConsoleLog.Info("Set sun to: " + amount);
                    }
                );

                // ---------------------------------------------------------
                // MONEY COMMAND
                // ---------------------------------------------------------
                CheatAPI.Register(
                    "money",
                    "money <amount> — gives money",
                    "Adds the specified amount of money using Board.GetMoney(amount).",
                    delegate (string[] args)
                    {
                        if (args.Length < 1)
                        {
                            CheatConsoleLog.Warn("Usage: money <amount>");
                            return;
                        }

                        int amount;
                        if (!CheatParse.TryInt(args[0], out amount))
                        {
                            CheatConsoleLog.Error("Invalid money amount");
                            return;
                        }

                        Board board = UnityEngine.Object.FindObjectOfType<Board>();
                        if (board == null)
                        {
                            CheatConsoleLog.Error("Board not found");
                            return;
                        }

                        board.GetMoney(amount);
                        CheatConsoleLog.Info("Gave money: " + amount);
                    }
                );

                // ---------------------------------------------------------
                // SPAWN ZOMBIE
                // ---------------------------------------------------------
                CheatAPI.Register(
                    "spawnzombie",
                    "spawnzombie <id> <row> — spawn a zombie",
                    "Spawns a zombie of the given type in the given row.",
                    delegate (string[] args)
                    {
                        if (args.Length < 2)
                        {
                            CheatConsoleLog.Warn("Usage: spawnzombie <id> <row>");
                            return;
                        }

                        if (!int.TryParse(args[0], out int zombieId))
                        {
                            CheatConsoleLog.Warn("Invalid zombie ID");
                            return;
                        }

                        if (!int.TryParse(args[1], out int row))
                        {
                            CheatConsoleLog.Warn("Invalid row");
                            return;
                        }

                        ZombieType type = (ZombieType)zombieId;

                        float x = 9.9f;

                        var cz = CreateZombie.Instance;
                        if (cz == null)
                        {
                            CheatConsoleLog.Warn("CreateZombie instance not found");
                            return;
                        }

                        var zombie = cz.SetZombie(row, type, x, false);

                        if (zombie != null)
                            CheatConsoleLog.BuiltIn(
                                $"Spawned zombie {type} ({zombieId}) in row {row}"
                            );
                        else
                            CheatConsoleLog.Warn("Failed to spawn zombie");
                    }
                );

                // ---------------------------------------------------------
                // SPAWN PLANT CARD
                // ---------------------------------------------------------
                CheatAPI.Register(
                    "spawnplant",
                    "spawnplant [PlantType]",
                    "Spawns a plant card. If no PlantType is given, picks a random one.",
                    delegate (string[] args)
                    {
                        PlantType pType;
                        if (args.Length == 0)
                        {
                            System.Array values = System.Enum.GetValues(typeof(PlantType));
                            int max = values.Length;

                            int index = UnityEngine.Random.Range(0, max);

                            pType = (PlantType)values.GetValue(index);

                            CheatConsoleLog.Info("Random plant selected: " + pType);
                        }
                        else
                        {
                            if (!CheatParse.TryEnum<PlantType>(args[0], out pType))
                            {
                                CheatConsoleLog.Error("Invalid plant type");
                                return;
                            }
                        }

                        Vector2 pos = new Vector2(2f, 2f);
                        int cost = 0;

                        Lawnf.SetDroppedCard(pos, pType, cost);

                        CheatConsoleLog.Info("Dropped plant card: " + pType);
                    }
                );
                // ---------------------------------------------------------
                // List Scripts
                // ---------------------------------------------------------

                CheatAPI.Register(
                    "listscripts",
                    "listscripts — shows all loaded script names",
                    "Lists all scripts currently loaded by the ScriptEngine.",
                    delegate (string[] args)
                    {
                        if (ScriptEngine.TryGetAllScriptNames(out var names))
                        {
                            if (names.Count == 0)
                            {
                                CheatConsoleLog.Info("No scripts loaded.");
                                return;
                            }

                            CheatConsoleLog.Info("Loaded scripts:");
                            for (int i = 0; i < names.Count; i++)
                                CheatConsoleLog.Info(" - " + names[i]);
                        }
                        else
                        {
                            CheatConsoleLog.Error("ScriptEngine returned no script list.");
                        }
                    }
                );

                MelonLogger.Msg("[CheatConsole] Custom cheat codes registered");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[CheatConsole] Error injecting cheat codes: " + ex);
            }
        }
    }

    // ---------------------------------------------------------
    // LOADER console manager and register type
    // ---------------------------------------------------------
    public class CheatConsoleLoader : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CheatConsoleManager>();
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (CheatConsoleManager.Instance == null)
            {
                GameObject go = new GameObject("CheatConsoleManager");
                go.AddComponent<CheatConsoleManager>();
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
        }
    }
}