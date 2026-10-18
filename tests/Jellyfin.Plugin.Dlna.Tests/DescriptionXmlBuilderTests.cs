using System;
using Jellyfin.Plugin.Dlna.Model;
using Jellyfin.Plugin.Dlna.Server;
using Xunit;

namespace Jellyfin.Plugin.Dlna.Tests;

public class DescriptionXmlBuilderTests
{
    private const string ServerUdn = "11111111-1111-1111-1111-111111111111";

    [Theory]
    [InlineData("http://192.168.1.10:8096", "/dlna/" + ServerUdn + "/icons/logo240.png")]
    [InlineData("http://192.168.1.10:8096/jellyfin", "/jellyfin/dlna/" + ServerUdn + "/icons/logo240.png")]
    [InlineData("/jellyfin", "/jellyfin/dlna/" + ServerUdn + "/icons/logo240.png")]
    public void IconUrls_AreRootRelative(string serverAddress, string expectedIconUrl)
    {
        var xml = new DescriptionXmlBuilder(new DlnaDeviceProfile(), ServerUdn, serverAddress, "Jellyfin", "server-id").GetXml();

        Assert.Contains("<url>" + expectedIconUrl + "</url>", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("<url>http", xml, StringComparison.Ordinal);
        Assert.Contains(
            "<controlURL>" + serverAddress.TrimEnd('/') + "/dlna/" + ServerUdn + "/contentdirectory/control</controlURL>",
            xml,
            StringComparison.Ordinal);
    }
}
