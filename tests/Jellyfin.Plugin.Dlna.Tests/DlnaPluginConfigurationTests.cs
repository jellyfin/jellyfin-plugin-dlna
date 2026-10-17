using Jellyfin.Plugin.Dlna.Configuration;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class DlnaPluginConfigurationTests
{
    [Fact]
    public void EnableSubtitleBurnIn_DefaultsToFalse()
    {
        Assert.False(new DlnaPluginConfiguration().EnableSubtitleBurnIn);
    }
}
