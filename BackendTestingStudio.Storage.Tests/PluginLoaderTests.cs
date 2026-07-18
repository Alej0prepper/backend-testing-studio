using BackendTestingStudio.Core.Plugins;
using BackendTestingStudio.Plugins;
using Xunit;

namespace BackendTestingStudio.Storage.Tests;

public sealed class PluginLoaderTests
{
    [Fact]
    public async Task GetInstalledAsync_DiscoversRegisteredPlugins()
    {
        var loader = new PluginLoader();

        var plugins = await loader.GetInstalledAsync();

        Assert.NotEmpty(plugins);
        Assert.Contains(plugins, plugin => plugin.Name == "Built-in Scenarios");
    }
}
