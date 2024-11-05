using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.Service;

namespace Jellyfin.Plugin.Dlna.MediaReceiverRegistrar;

/// <summary>
/// Defines the <see cref="MediaReceiverRegistrarXmlBuilder" />.
/// See https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-drmnd/5d37515e-7a63-4709-8258-8fd4e0ed4482.
/// </summary>
public static class MediaReceiverRegistrarXmlBuilder
{
    /// <summary>
    /// Retrieves an XML description of the X_MS_MediaReceiverRegistrar.
    /// </summary>
    /// <returns>An XML representation of this service.</returns>
    public static string GetXml()
    {
        return ServiceXmlBuilder.GetXml(ServiceActionListBuilder.GetActions(), GetStateVariables());
    }

    /// <summary>
    /// The a list of all the state variables for this invocation.
    /// </summary>
    /// <returns>The <see cref="IEnumerable{StateVariable}"/>.</returns>
    private static IReadOnlyList<StateVariable> GetStateVariables()
    {
        return
        [
            new() {
                Name = "AuthorizationGrantedUpdateID",
                DataType = "ui4",
                SendsEvents = true
            },

            new() {
                Name = "A_ARG_TYPE_DeviceID",
                DataType = "string",
                SendsEvents = false
            },

            new() {
                Name = "AuthorizationDeniedUpdateID",
                DataType = "ui4",
                SendsEvents = true
            },

            new() {
                Name = "ValidationSucceededUpdateID",
                DataType = "ui4",
                SendsEvents = true
            },

            new() {
                Name = "A_ARG_TYPE_RegistrationRespMsg",
                DataType = "bin.base64",
                SendsEvents = false
            },

            new() {
                Name = "A_ARG_TYPE_RegistrationReqMsg",
                DataType = "bin.base64",
                SendsEvents = false
            },

            new() {
                Name = "ValidationRevokedUpdateID",
                DataType = "ui4",
                SendsEvents = true
            },

            new() {
                Name = "A_ARG_TYPE_Result",
                DataType = "int",
                SendsEvents = false
            }
        ];
    }
}
