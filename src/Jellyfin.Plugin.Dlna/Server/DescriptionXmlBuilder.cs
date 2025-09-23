using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.Model;

namespace Jellyfin.Plugin.Dlna.Server;

/// <summary>
/// Defines the <see cref="DescriptionXmlBuilder" />.
/// </summary>
public class DescriptionXmlBuilder
{
    private readonly DlnaDeviceProfile _profile;

    private readonly string _serverUdn;
    private readonly string _serverAddress;
    private readonly string _serverName;
    private readonly string _serverId;

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptionXmlBuilder"/> class.
    /// </summary>
    /// <param name="profile">The <see cref="DlnaDeviceProfile"/>.</param>
    /// <param name="serverUdn">The server UDN.</param>
    /// <param name="serverAddress">The address.</param>
    /// <param name="serverName">The name.</param>
    /// <param name="serverId">The id.</param>
    public DescriptionXmlBuilder(DlnaDeviceProfile profile, string serverUdn, string serverAddress, string serverName, string serverId)
    {
        ArgumentException.ThrowIfNullOrEmpty(serverUdn);

        _profile = profile;
        _serverUdn = serverUdn;
        _serverAddress = serverAddress;
        _serverName = serverName;
        _serverId = serverId;
    }

    /// <summary>
    /// Gets the description XML.
    /// </summary>
    public string GetXml()
    {
        var builder = new StringBuilder();

        builder.Append("<?xml version=\"1.0\"?>");

        builder.Append("<root");

        var attributes = _profile.XmlRootAttributes.ToList();

        attributes.Insert(0, new XmlAttribute
        {
            Name = "xmlns:dlna",
            Value = "urn:schemas-dlna-org:device-1-0"
        });
        attributes.Insert(0, new XmlAttribute
        {
            Name = "xmlns",
            Value = "urn:schemas-upnp-org:device-1-0"
        });

        foreach (var att in attributes)
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, " {0}=\"{1}\"", att.Name, att.Value);
        }

        builder.Append('>');

        builder.Append("<specVersion>");
        builder.Append("<major>1</major>");
        builder.Append("<minor>0</minor>");
        builder.Append("</specVersion>");

        AppendDeviceInfo(builder);

        builder.Append("</root>");

        return builder.ToString();
    }

    private void AppendDeviceInfo(StringBuilder builder)
    {
        builder.Append("<device>");
        AppendDeviceProperties(builder);

        AppendIconList(builder);

        builder.Append("<presentationURL>")
            .Append(SecurityElement.Escape(_serverAddress))
            .Append("/web/index.html</presentationURL>");

        AppendServiceList(builder);
        builder.Append("</device>");
    }

    private void AppendDeviceProperties(StringBuilder builder)
    {
        builder.Append("<dlna:X_DLNACAP/>");

        builder.Append("<dlna:X_DLNADOC xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">DMS-1.50</dlna:X_DLNADOC>");
        builder.Append("<dlna:X_DLNADOC xmlns:dlna=\"urn:schemas-dlna-org:device-1-0\">M-DMS-1.50</dlna:X_DLNADOC>");

        builder.Append("<deviceType>urn:schemas-upnp-org:device:MediaServer:1</deviceType>");

        builder.Append("<friendlyName>")
            .Append(SecurityElement.Escape(GetFriendlyName()))
            .Append("</friendlyName>");
        builder.Append("<manufacturer>")
            .Append(SecurityElement.Escape(_profile.Manufacturer ?? string.Empty))
            .Append("</manufacturer>");
        builder.Append("<manufacturerURL>")
            .Append(SecurityElement.Escape(_profile.ManufacturerUrl ?? string.Empty))
            .Append("</manufacturerURL>");

        builder.Append("<modelDescription>")
            .Append(SecurityElement.Escape(_profile.ModelDescription ?? string.Empty))
            .Append("</modelDescription>");
        builder.Append("<modelName>")
            .Append(SecurityElement.Escape(_profile.ModelName ?? string.Empty))
            .Append("</modelName>");

        builder.Append("<modelNumber>")
            .Append(SecurityElement.Escape(_profile.ModelNumber ?? string.Empty))
            .Append("</modelNumber>");
        builder.Append("<modelURL>")
            .Append(SecurityElement.Escape(_profile.ModelUrl ?? string.Empty))
            .Append("</modelURL>");

        if (string.IsNullOrEmpty(_profile.SerialNumber))
        {
            builder.Append("<serialNumber>")
                .Append(SecurityElement.Escape(_serverId))
                .Append("</serialNumber>");
        }
        else
        {
            builder.Append("<serialNumber>")
                .Append(SecurityElement.Escape(_profile.SerialNumber))
                .Append("</serialNumber>");
        }

        builder.Append("<UPC/>");

        builder.Append("<UDN>uuid:")
            .Append(SecurityElement.Escape(_serverUdn))
            .Append("</UDN>");

        if (!string.IsNullOrEmpty(_profile.SonyAggregationFlags))
        {
            builder.Append("<av:aggregationFlags xmlns:av=\"urn:schemas-sony-com:av\">")
                .Append(SecurityElement.Escape(_profile.SonyAggregationFlags))
                .Append("</av:aggregationFlags>");
        }
    }

    internal string GetFriendlyName()
    {
        if (string.IsNullOrEmpty(_profile.FriendlyName))
        {
            return _serverName;
        }

        if (!_profile.FriendlyName.Contains("${HostName}", StringComparison.OrdinalIgnoreCase))
        {
            return _profile.FriendlyName;
        }

        var characterList = new List<char>();

        foreach (var c in _serverName)
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                characterList.Add(c);
            }
        }

        var serverName = string.Create(
            characterList.Count,
            characterList,
            (dest, source) =>
            {
                for (int i = 0; i < dest.Length; i++)
                {
                    dest[i] = source[i];
                }
            });

        return _profile.FriendlyName.Replace("${HostName}", serverName, StringComparison.OrdinalIgnoreCase);
    }

    private void AppendIconList(StringBuilder builder)
    {
        builder.Append("<iconList>");

        foreach (var icon in GetIcons())
        {
            builder.Append("<icon>");

            builder.Append("<mimetype>")
                .Append(SecurityElement.Escape(icon.MimeType))
                .Append("</mimetype>");
            builder.Append("<width>")
                .Append(SecurityElement.Escape(icon.Width.ToString(CultureInfo.InvariantCulture)))
                .Append("</width>");
            builder.Append("<height>")
                .Append(SecurityElement.Escape(icon.Height.ToString(CultureInfo.InvariantCulture)))
                .Append("</height>");
            builder.Append("<depth>")
                .Append(SecurityElement.Escape(icon.Depth))
                .Append("</depth>");
            builder.Append("<url>")
                .Append(BuildUrl(icon.Url))
                .Append("</url>");

            builder.Append("</icon>");
        }

        builder.Append("</iconList>");
    }

    private void AppendServiceList(StringBuilder builder)
    {
        builder.Append("<serviceList>");

        foreach (var service in GetServices())
        {
            builder.Append("<service>");

            builder.Append("<serviceType>")
                .Append(SecurityElement.Escape(service.ServiceType))
                .Append("</serviceType>");
            builder.Append("<serviceId>")
                .Append(SecurityElement.Escape(service.ServiceId))
                .Append("</serviceId>");
            builder.Append("<SCPDURL>")
                .Append(BuildUrl(service.ScpdUrl))
                .Append("</SCPDURL>");
            builder.Append("<controlURL>")
                .Append(BuildUrl(service.ControlUrl))
                .Append("</controlURL>");
            builder.Append("<eventSubURL>")
                .Append(BuildUrl(service.EventSubUrl))
                .Append("</eventSubURL>");

            builder.Append("</service>");
        }

        builder.Append("</serviceList>");
    }

    private string BuildUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        url = _serverAddress.TrimEnd('/') + "/dlna/" + _serverUdn + "/" + url.TrimStart('/');

        return SecurityElement.Escape(url);
    }

    private static IEnumerable<DeviceIcon> GetIcons()
        =>
        [
            new DeviceIcon
            {
                MimeType = "image/png",
                Depth = "24",
                Width = 240,
                Height = 240,
                Url = "icons/logo240.png"
            },

            new DeviceIcon
            {
                MimeType = "image/jpeg",
                Depth = "24",
                Width = 240,
                Height = 240,
                Url = "icons/logo240.jpg"
            },

            new DeviceIcon
            {
                MimeType = "image/png",
                Depth = "24",
                Width = 120,
                Height = 120,
                Url = "icons/logo120.png"
            },

            new DeviceIcon
            {
                MimeType = "image/jpeg",
                Depth = "24",
                Width = 120,
                Height = 120,
                Url = "icons/logo120.jpg"
            },

            new DeviceIcon
            {
                MimeType = "image/png",
                Depth = "24",
                Width = 48,
                Height = 48,
                Url = "icons/logo48.png"
            },

            new DeviceIcon
            {
                MimeType = "image/jpeg",
                Depth = "24",
                Width = 48,
                Height = 48,
                Url = "icons/logo48.jpg"
            }
        ];

    private List<DeviceService> GetServices()
    {
        var list = new List<DeviceService>
        {
            new() {
                ServiceType = "urn:schemas-upnp-org:service:ContentDirectory:1",
                ServiceId = "urn:upnp-org:serviceId:ContentDirectory",
                ScpdUrl = "contentdirectory/contentdirectory.xml",
                ControlUrl = "contentdirectory/control",
                EventSubUrl = "contentdirectory/events"
            },
            new() {
                ServiceType = "urn:schemas-upnp-org:service:ConnectionManager:1",
                ServiceId = "urn:upnp-org:serviceId:ConnectionManager",
                ScpdUrl = "connectionmanager/connectionmanager.xml",
                ControlUrl = "connectionmanager/control",
                EventSubUrl = "connectionmanager/events"
            }
        };

        if (_profile.EnableMSMediaReceiverRegistrar)
        {
            list.Add(new DeviceService
            {
                ServiceType = "urn:microsoft.com:service:X_MS_MediaReceiverRegistrar:1",
                ServiceId = "urn:microsoft.com:serviceId:X_MS_MediaReceiverRegistrar",
                ScpdUrl = "mediareceiverregistrar/mediareceiverregistrar.xml",
                ControlUrl = "mediareceiverregistrar/control",
                EventSubUrl = "mediareceiverregistrar/events"
            });
        }

        return list;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return GetXml();
    }
}
