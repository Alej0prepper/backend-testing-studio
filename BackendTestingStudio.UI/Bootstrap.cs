using System.Runtime.CompilerServices;

namespace BackendTestingStudio.UI;

internal static class Bootstrap
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        // Avoid exhausting inotify/file-watcher limits in constrained environments.
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
    }
}
