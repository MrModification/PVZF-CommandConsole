using System.Collections.Generic;

public class ScriptContext
{
    public Dictionary<string, string> Variables = new Dictionary<string, string>();
    public Dictionary<string, System.Collections.Generic.List<string>> Arrays = new Dictionary<string, System.Collections.Generic.List<string>>();
    public Dictionary<string, System.Collections.Generic.List<string>> Functions = new Dictionary<string, System.Collections.Generic.List<string>>();
}