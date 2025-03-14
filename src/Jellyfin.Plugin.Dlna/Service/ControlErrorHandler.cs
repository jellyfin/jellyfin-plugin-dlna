using System;
using System.IO;
using System.Text;
using System.Xml;
using Jellyfin.Plugin.Dlna.Didl;

namespace Jellyfin.Plugin.Dlna.Service;

/// <summary>
/// Defines the <see cref="ControlErrorHandler" />.
/// </summary>
public static class ControlErrorHandler
{
    private const string NsSoapEnv = "http://schemas.xmlsoap.org/soap/envelope/";

    /// <summary>
    /// Gets the response for an <see cref="Exception"/>.
    /// </summary>
    /// <param name="ex">The <see cref="Exception"/>.</param>
    public static ControlResponse GetResponse(Exception ex)
    {
        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            CloseOutput = false
        };

        StringWriter builder = new StringWriterWithEncoding(Encoding.UTF8);

        using (var writer = XmlWriter.Create(builder, settings))
        {
            writer.WriteStartDocument(true);

            writer.WriteStartElement("SOAP-ENV", "Envelope", NsSoapEnv);
            writer.WriteAttributeString(string.Empty, "encodingStyle", NsSoapEnv, "http://schemas.xmlsoap.org/soap/encoding/");

            writer.WriteStartElement("SOAP-ENV", "Body", NsSoapEnv);
            writer.WriteStartElement("SOAP-ENV", "Fault", NsSoapEnv);

            writer.WriteElementString("faultcode", "500");
            writer.WriteElementString("faultstring", ex.Message);

            writer.WriteStartElement("detail");
            writer.WriteRaw("<UPnPError xmlns=\"urn:schemas-upnp-org:control-1-0\"><errorCode>401</errorCode><errorDescription>Invalid Action</errorDescription></UPnPError>");
            writer.WriteFullEndElement();

            writer.WriteFullEndElement();
            writer.WriteFullEndElement();

            writer.WriteFullEndElement();
            writer.WriteEndDocument();
        }

        return new ControlResponse(builder.ToString(), false);
    }
}
