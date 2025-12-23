using CheatConsole;
using MelonLoader;
using MelonLoader.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum ExecSignal
{
    None,
    Break,
    Continue,
    Return
}

public static class ScriptEngine
{

    private static readonly Dictionary<string, IList<string>> LoadedScripts =
        new Dictionary<string, IList<string>>(StringComparer.OrdinalIgnoreCase);

    private static string _scriptsFolder;
    private static readonly Dictionary<string, DateTime> _fileTimestamps =
        new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

    private static float _nextPollTime;
    private static float _pollInterval = 2f;
    private static bool _pollingStarted;
    static ScriptEngine()
    {
        try
        {
            string folder = Path.Combine(MelonEnvironment.ModsDirectory, "CommandConsole");
            LoadScriptsFromFolder(folder, true);
            CheatConsoleLog.Info("ScriptEngine static init. Script folder: " + folder);
        }
        catch (Exception ex)
        {
            CheatConsoleLog.Error("ScriptEngine static init failed: " + ex.Message);
        }
    }

    // -------- Public registry / API --------
    public static bool TryGetAllScriptNames(out List<string> names)
    {
        names = new List<string>(LoadedScripts.Keys);
        return true;
    }
    public static void RegisterScript(string name, IList<string> lines)
    {
        if (string.IsNullOrWhiteSpace(name) || lines == null)
            return;

        LoadedScripts[name] = lines;
    }

    public static bool TryGetScript(string name, out IList<string> lines)
    {
        return LoadedScripts.TryGetValue(name, out lines);
    }

    public static void RunScript(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            CheatConsoleLog.Error("Script name is empty.");
            return;
        }

        IList<string> lines;
        if (!LoadedScripts.TryGetValue(name, out lines))
        {
            CheatConsoleLog.Error("Script '" + name + "' not found.");
            return;
        }

        ExecuteLines(lines, new ScriptContext());
    }

    public static void LoadScriptsFromFolder(string folderPath, bool enableHotReload, string searchPattern = "*.txt")
    {
        if (string.IsNullOrEmpty(folderPath))
            return;

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        _scriptsFolder = folderPath;
        LoadAllScripts(searchPattern);

        if (enableHotReload && !_pollingStarted)
        {
            _pollingStarted = true;
            _nextPollTime = 0f;
            MelonCoroutines.Start(PollForChanges(searchPattern));
            CheatConsoleLog.Info("ScriptEngine hot-reload polling started for: " + folderPath);
        }
    }

    private static void LoadAllScripts(string searchPattern)
    {
        LoadedScripts.Clear();
        _fileTimestamps.Clear();

        if (string.IsNullOrEmpty(_scriptsFolder))
            return;

        string[] files;
        try
        {
            files = Directory.GetFiles(_scriptsFolder, searchPattern);
        }
        catch (Exception ex)
        {
            CheatConsoleLog.Error("ScriptEngine.LoadAllScripts error: " + ex.Message);
            return;
        }

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            string name = Path.GetFileName(file);

            string[] lines;
            try
            {
                lines = File.ReadAllLines(file);
            }
            catch (Exception ex)
            {
                CheatConsoleLog.Error("Failed to read script file '" + file + "': " + ex.Message);
                continue;
            }

            RegisterScript(name, lines);
            try
            {
                _fileTimestamps[file] = File.GetLastWriteTimeUtc(file);
            }
            catch
            {
            }

            CheatConsoleLog.Info("Loaded script '" + name + "' from " + file);
        }
    }

    private static System.Collections.IEnumerator PollForChanges(string searchPattern)
    {
        while (true)
        {
            if (Time.time >= _nextPollTime)
            {
                _nextPollTime = Time.time + _pollInterval;
                CheckForScriptChanges(searchPattern);
            }

            yield return null;
        }
    }

    private static void CheckForScriptChanges(string searchPattern)
    {
        if (string.IsNullOrEmpty(_scriptsFolder))
            return;

        string[] files;
        try
        {
            files = Directory.GetFiles(_scriptsFolder, searchPattern);
        }
        catch (Exception ex)
        {
            CheatConsoleLog.Error("ScriptEngine.CheckForScriptChanges: " + ex.Message);
            return;
        }

        bool changed = false;

        for (int i = 0; i < files.Length; i++)
        {
            string file = files[i];
            DateTime lastWrite;

            try
            {
                lastWrite = File.GetLastWriteTimeUtc(file);
            }
            catch
            {
                continue;
            }

            DateTime prev;
            if (!_fileTimestamps.TryGetValue(file, out prev) || lastWrite != prev)
            {
                _fileTimestamps[file] = lastWrite;
                changed = true;
            }
        }

        List<string> knownFiles = new List<string>(_fileTimestamps.Keys);
        for (int i = 0; i < knownFiles.Count; i++)
        {
            string known = knownFiles[i];
            if (Array.IndexOf(files, known) < 0)
            {
                _fileTimestamps.Remove(known);
                changed = true;
            }
        }

        if (changed)
        {
            CheatConsoleLog.Info("Script changes detected. Reloading scripts...");
            LoadAllScripts(searchPattern);
        }
    }

    public static void ExecuteLines(IList<string> lines, ScriptContext ctx)
    {
        NormalizeLines(lines);
        ExecuteLinesInternal(lines, ctx);
    }

    private static void NormalizeLines(IList<string> lines)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];

            line = line.Trim().Trim('\uFEFF');

            if (line.EndsWith("{"))
            {
                string before = line.Substring(0, line.Length - 1).TrimEnd();

                lines[i] = before;

                lines.Insert(i + 1, "{");
            }
            else
            {
                lines[i] = line;
            }
        }
    }

    private static ExecSignal ExecuteLinesInternal(IList<string> lines, ScriptContext ctx)
    {
        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];

            if (string.IsNullOrWhiteSpace(line))
                continue;
            if (line.StartsWith("#") || line.StartsWith("//"))
                continue;

            string next = (i + 1 < lines.Count)
                ? lines[i + 1].Trim().Trim('\uFEFF')
                : "";

            ExecSignal signal = ExecSignal.None;

            try
            {
                if (line.StartsWith("repeat ") && next == "{")
                {
                    signal = HandleRepeatBlock(lines, ref i, ctx);
                }
                else if (line.StartsWith("if ") && next == "{")
                {
                    signal = HandleIfBlock(lines, ref i, ctx);
                }
                else if (line.StartsWith("while ") && next == "{")
                {
                    signal = HandleWhileBlock(lines, ref i, ctx);
                }
                else if (line.StartsWith("for ") && next == "{")
                {
                    signal = HandleForBlock(lines, ref i, ctx);
                }
                else if (line.StartsWith("switch ") && next == "{")
                {
                    signal = HandleSwitch(lines, ref i, ctx);
                }
                else if (line.StartsWith("try") && next == "{")
                {
                    signal = HandleTryCatch(lines, ref i, ctx);
                }
                else if (line.StartsWith("function "))
                {
                    HandleFunctionDefinition(lines, ref i, ctx);
                    continue;
                }
                else
                {
                    signal = ExecuteLine(line, ctx);
                }
            }
            catch (Exception ex)
            {
                ScriptError.Report(ex.Message, i + 1);
            }

            if (signal != ExecSignal.None)
                return signal;
        }

        return ExecSignal.None;
    }

    private static ExecSignal ExecuteLine(string line, ScriptContext ctx)
    {
        if (line == "return")
            return ExecSignal.Return;
        if (line == "break")
            return ExecSignal.Break;
        if (line == "continue")
            return ExecSignal.Continue;

        if (line.StartsWith("set "))
        {
            HandleSet(line, ctx);
            return ExecSignal.None;
        }

        if (line.StartsWith("wait "))
        {
            HandleWait(line);
            return ExecSignal.None;
        }

        line = SubstituteVariables(line, ctx);
        line = ResolveArrayAccesses(line, ctx);

        List<string> funcBlock;
        if (ctx.Functions.TryGetValue(line, out funcBlock))
        {
            ExecSignal signal = ExecuteLinesInternal(funcBlock, ctx);

            if (signal == ExecSignal.Return)
                return ExecSignal.None;

            return signal;
        }

        CheatConsoleRouter.RunCheat(line);
        return ExecSignal.None;
    }

    private static List<string> ReadBlock(IList<string> lines, ref int index)
    {
        List<string> block = new List<string>();
        int depth = 0;

        index++;

        for (; index < lines.Count; index++)
        {
            string line = lines[index].Trim();

            if (line == "{")
            {
                depth++;
                continue;
            }

            if (line == "}")
            {
                if (depth == 0)
                    break;

                depth--;
                continue;
            }

            block.Add(line);
        }

        return block;
    }

    private static ExecSignal HandleRepeatBlock(IList<string> lines, ref int index, ScriptContext ctx)
    {
        string header = lines[index];
        string[] parts = header.Split(new[] { ' ' }, 2);

        int count;
        if (parts.Length < 2 || !int.TryParse(parts[1].Trim(), out count))
        {
            ScriptError.Report("Invalid repeat count", index + 1);
            return ExecSignal.None;
        }

        List<string> block = ReadBlock(lines, ref index);

        for (int i = 0; i < count; i++)
        {
            ExecSignal signal = ExecuteLinesInternal(block, ctx);

            if (signal == ExecSignal.Return)
                return ExecSignal.Return;
            if (signal == ExecSignal.Break)
                break;
            if (signal == ExecSignal.Continue)
                continue;
        }

        return ExecSignal.None;
    }

    private static ExecSignal HandleIfBlock(IList<string> lines, ref int index, ScriptContext ctx)
    {
        string header = lines[index];

        string[] parts = header.Split(new[] { ' ' }, 4);
        if (parts.Length < 4)
        {
            ScriptError.Report("Invalid if syntax", index + 1);
            return ExecSignal.None;
        }

        string left = SubstituteVariables(parts[1], ctx);
        string op = parts[2];
        string right = SubstituteVariables(parts[3], ctx);

        float a, b;
        if (!float.TryParse(left, out a) || !float.TryParse(right, out b))
        {
            ScriptError.Report("Invalid numeric comparison in if", index + 1);
            return ExecSignal.None;
        }

        List<string> block = ReadBlock(lines, ref index);

        if (Compare(a, op, b))
        {
            return ExecuteLinesInternal(block, ctx);
        }

        return ExecSignal.None;
    }

    private static ExecSignal HandleWhileBlock(IList<string> lines, ref int index, ScriptContext ctx)
    {
        string header = lines[index];
        string[] parts = header.Split(new[] { ' ' }, 4);
        if (parts.Length < 4)
        {
            ScriptError.Report("Invalid while syntax", index + 1);
            return ExecSignal.None;
        }

        List<string> block = ReadBlock(lines, ref index);

        int safety = 0;

        while (true)
        {
            string left = SubstituteVariables(parts[1], ctx);
            string op = parts[2];
            string right = SubstituteVariables(parts[3], ctx);

            float a, b;
            if (!float.TryParse(left, out a) || !float.TryParse(right, out b))
            {
                ScriptError.Report("Invalid numeric comparison in while", index + 1);
                break;
            }

            if (!Compare(a, op, b))
                break;

            ExecSignal signal = ExecuteLinesInternal(block, ctx);

            if (signal == ExecSignal.Return)
                return ExecSignal.Return;
            if (signal == ExecSignal.Break)
                break;

            safety++;
            if (safety > 10000)
            {
                ScriptError.Report("While loop exceeded 10,000 iterations", index + 1);
                break;
            }
        }

        return ExecSignal.None;
    }

    private static ExecSignal HandleForBlock(IList<string> lines, ref int index, ScriptContext ctx)
    {
        string header = lines[index];

        string[] parts = header.Split(' ');
        if (parts.Length < 6 || parts[2] != "=" || parts[4] != "to")
        {
            ScriptError.Report("Invalid for syntax", index + 1);
            return ExecSignal.None;
        }

        string varName = parts[1];
        int start, end;
        if (!int.TryParse(parts[3], out start) || !int.TryParse(parts[5], out end))
        {
            ScriptError.Report("Invalid numeric range in for", index + 1);
            return ExecSignal.None;
        }

        List<string> block = ReadBlock(lines, ref index);

        for (int i = start; i <= end; i++)
        {
            ctx.Variables[varName] = i.ToString();

            ExecSignal signal = ExecuteLinesInternal(block, ctx);

            if (signal == ExecSignal.Return)
                return ExecSignal.Return;
            if (signal == ExecSignal.Break)
                break;
        }

        return ExecSignal.None;
    }

    private static ExecSignal HandleSwitch(IList<string> lines, ref int index, ScriptContext ctx)
    {
        string header = lines[index];
        string expr = header.Substring("switch".Length).Trim();

        expr = SubstituteVariables(expr, ctx);
        expr = EvaluateMath(expr);

        string switchValue = expr;

        List<string> switchBlock = ReadBlock(lines, ref index);

        for (int i = 0; i < switchBlock.Count; i++)
        {
            string line = switchBlock[i].Trim();

            if (line.StartsWith("case "))
            {
                string caseValue = line.Substring(5).Trim();

                if (i + 1 < switchBlock.Count && switchBlock[i + 1].Trim() == "{")
                {
                    List<string> caseBlock = ReadBlock(switchBlock, ref i);

                    if (caseValue == switchValue)
                    {
                        return ExecuteLinesInternal(caseBlock, ctx);
                    }
                }
            }

            if (line.StartsWith("default"))
            {
                if (i + 1 < switchBlock.Count && switchBlock[i + 1].Trim() == "{")
                {
                    List<string> defaultBlock = ReadBlock(switchBlock, ref i);
                    return ExecuteLinesInternal(defaultBlock, ctx);
                }
            }
        }

        return ExecSignal.None;
    }

    private static ExecSignal HandleTryCatch(IList<string> lines, ref int index, ScriptContext ctx)
    {
        List<string> tryBlock = ReadBlock(lines, ref index);

        if (index + 1 >= lines.Count || lines[index + 1].Trim() != "catch")
        {
            ScriptError.Report("Expected 'catch' after try block", index + 1);
            return ExecSignal.None;
        }

        index += 1;
        if (index + 1 >= lines.Count || lines[index + 1].Trim() != "{")
        {
            ScriptError.Report("Catch missing block", index + 1);
            return ExecSignal.None;
        }

        List<string> catchBlock = ReadBlock(lines, ref index);

        try
        {
            return ExecuteLinesInternal(tryBlock, ctx);
        }
        catch (Exception ex)
        {
            ctx.Variables["error"] = ex.Message;
            return ExecuteLinesInternal(catchBlock, ctx);
        }
    }

    private static void HandleFunctionDefinition(IList<string> lines, ref int index, ScriptContext ctx)
    {
        string header = lines[index];

        string[] parts = header.Split(' ');
        if (parts.Length < 2)
        {
            ScriptError.Report("Invalid function syntax", index + 1);
            return;
        }

        string name = parts[1].Trim();

        if (index + 1 >= lines.Count || lines[index + 1].Trim() != "{")
        {
            ScriptError.Report("Function missing block", index + 1);
            return;
        }

        List<string> block = ReadBlock(lines, ref index);

        ctx.Functions[name] = block;
        CheatConsoleLog.Info("Defined function '" + name + "'");
    }

    private static bool Compare(float a, string op, float b)
    {
        switch (op)
        {
            case ">": return a > b;
            case "<": return a < b;
            case ">=": return a >= b;
            case "<=": return a <= b;
            case "==": return Math.Abs(a - b) < 0.0001f;
            case "!=": return Math.Abs(a - b) > 0.0001f;
            default: return false;
        }
    }

    private static void HandleSet(string line, ScriptContext ctx)
    {
        string expr = line.Substring(4);

        string[] parts = expr.Split(new[] { '=' }, 2);
        if (parts.Length != 2)
            return;

        string key = parts[0].Trim();
        string value = parts[1].Trim();

        int bracketIndex = key.IndexOf('[');
        if (bracketIndex >= 0)
        {
            int endBracket = key.IndexOf(']', bracketIndex + 1);
            if (endBracket > bracketIndex)
            {
                string arrName = key.Substring(0, bracketIndex);
                string indexStr = key.Substring(bracketIndex + 1, endBracket - bracketIndex - 1);

                int idx;
                if (!int.TryParse(indexStr, out idx))
                    return;

                value = SubstituteVariables(value, ctx);
                value = EvaluateMath(value);

                List<string> arr;
                if (!ctx.Arrays.TryGetValue(arrName, out arr))
                {
                    arr = new List<string>();
                    ctx.Arrays[arrName] = arr;
                }

                while (arr.Count <= idx)
                    arr.Add("");

                arr[idx] = value;
                return;
            }
        }

        if (value.StartsWith("[") && value.EndsWith("]"))
        {
            string inner = value.Substring(1, value.Length - 2);
            string[] items = inner.Split(',');

            List<string> arr = new List<string>();

            for (int i = 0; i < items.Length; i++)
            {
                string item = items[i].Trim();
                item = SubstituteVariables(item, ctx);
                item = EvaluateMath(item);
                arr.Add(item);
            }

            ctx.Arrays[key] = arr;
            return;
        }

        value = SubstituteVariables(value, ctx);
        value = EvaluateMath(value);

        ctx.Variables[key] = value;
    }

    private static void HandleWait(string line)
    {
        string num = line.Substring(5).Trim();
        float seconds;
        if (float.TryParse(num, out seconds))
        {
            if (ScriptRunnerBehaviour.Instance != null)
                ScriptRunnerBehaviour.Instance.RunWait(seconds);
        }
    }

    private static string SubstituteVariables(string line, ScriptContext ctx)
    {
        foreach (KeyValuePair<string, string> kv in ctx.Variables)
        {
            line = line.Replace("$" + kv.Key, kv.Value);
        }
        return line;
    }

    private static string ResolveArrayAccesses(string line, ScriptContext ctx)
    {
        int dollar = line.IndexOf('$');
        while (dollar != -1)
        {
            int bracket = line.IndexOf('[', dollar + 1);
            if (bracket == -1) break;

            int endBracket = line.IndexOf(']', bracket + 1);
            if (endBracket == -1) break;

            string name = line.Substring(dollar + 1, bracket - (dollar + 1));
            string indexStr = line.Substring(bracket + 1, endBracket - (bracket + 1));

            List<string> arr;
            if (ctx.Arrays.TryGetValue(name, out arr))
            {
                int idx;
                if (int.TryParse(indexStr, out idx) && idx >= 0 && idx < arr.Count)
                {
                    string full = line.Substring(dollar, endBracket - dollar + 1);
                    line = line.Replace(full, arr[idx]);
                }
            }

            dollar = line.IndexOf('$', endBracket + 1);
        }

        return line;
    }

    private static string EvaluateMath(string value)
    {
        if (value.IndexOf('+') >= 0 || value.IndexOf('-') >= 0 ||
            value.IndexOf('*') >= 0 || value.IndexOf('/') >= 0 ||
            value.IndexOf('%') >= 0)
        {
            try
            {
                float result = MathEval.Eval(value);
                return result.ToString();
            }
            catch
            {
            }
        }
        return value;
    }
}