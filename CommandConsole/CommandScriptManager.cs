using CheatConsole;
using System;
using System.IO;

public static class CommandScriptManager
{
    public static readonly string ScriptFolder =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods", "CommandConsole");

    public static void Initialize()
    {
        if (!Directory.Exists(ScriptFolder))
        {
            Directory.CreateDirectory(ScriptFolder);
            CreateSampleScript();
            return;
        }

        if (Directory.GetFiles(ScriptFolder, "*.txt").Length == 0 &&
            Directory.GetFiles(ScriptFolder, "*.cmd").Length == 0 &&
            Directory.GetFiles(ScriptFolder, "*.bat").Length == 0)
        {
            CreateSampleScript();
        }
    }

    private static void CreateSampleScript()
    {
        string samplePath = Path.Combine(ScriptFolder, "sample.txt");

        if (!File.Exists(samplePath))
        {
            File.WriteAllText(samplePath,
        @"# Sample CommandConsole script

set row = 3
set type = 5

spawnzombie $type $row
    wait 0.5
spawnzombie $type $row
    wait 0.5
spawnzombie $type $row
    wait 0.5
spawnzombie $type $row
    wait 0.5
spawnzombie $type $row
    wait 0.5");
        }

    }

    public static bool TryRunScript(string command)
    {
        string[] extensions = { ".txt", ".cmd", ".bat" };

        for (int i = 0; i < extensions.Length; i++)
        {
            string ext = extensions[i];
            string path = Path.Combine(ScriptFolder, command + ext);
            if (File.Exists(path))
            {
                RunScript(path);
                return true;
            }
        }

        return false;
    }

    private static void RunScript(string path)
    {
        string[] lines = File.ReadAllLines(path);

        CheatConsoleLog.BuiltIn("Running script: " + Path.GetFileName(path));

        ScriptContext ctx = new ScriptContext();
        ScriptEngine.ExecuteLines(lines, ctx);
    }
}