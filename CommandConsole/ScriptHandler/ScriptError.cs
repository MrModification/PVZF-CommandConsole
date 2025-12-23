using CheatConsole;

public static class ScriptError
{
    public static void Report(string message, int line)
    {
        CheatConsoleLog.Error("[Line " + line + "] " + message);
    }
}