using System.Runtime.Versioning;

namespace CompileTarget;

public static class CompileTarget
{
    [SupportedOSPlatform("windows")]
    public static async Task<int> Main()
    {
        return await Task.FromResult(0);
    }
}