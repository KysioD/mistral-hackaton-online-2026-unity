using System.Collections.Generic;

namespace DefaultNamespace.npcs.functions
{
    public interface INpcFunction
    {
        string processFunction(string functionName, IDictionary<string, string> args);

        List<string> FunctionsList();
    }
}