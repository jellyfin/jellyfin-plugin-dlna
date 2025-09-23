using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Http;

namespace Jellyfin.Plugin.Dlna.Model;

/// <summary>
/// Defines the <see cref="IDlnaManager" /> interface.
/// </summary>
public interface IDlnaManager
{
    /// <summary>
    /// Gets the profile infos.
    /// </summary>
    /// <returns>IEnumerable{DeviceProfileInfo}.</returns>
    IEnumerable<DeviceProfileInfo> GetProfileInfos();

    /// <summary>
    /// Gets the profile.
    /// </summary>
    /// <param name="headers">The headers.</param>
    /// <returns>DeviceProfile.</returns>
    DlnaDeviceProfile? GetProfile(IHeaderDictionary headers);

    /// <summary>
    /// Gets the default profile.
    /// </summary>
    /// <returns>DeviceProfile.</returns>
    DlnaDeviceProfile GetDefaultProfile();

    /// <summary>
    /// Creates the profile.
    /// </summary>
    /// <param name="profile">The profile.</param>
    void CreateProfile(DlnaDeviceProfile profile);

    /// <summary>
    /// Updates the profile.
    /// </summary>
    /// <param name="profileId">The profile id.</param>
    /// <param name="profile">The profile.</param>
    void UpdateProfile(string profileId, DlnaDeviceProfile profile);

    /// <summary>
    /// Deletes the profile.
    /// </summary>
    /// <param name="id">The identifier.</param>
    void DeleteProfile(string id);

    /// <summary>
    /// Gets the profile.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>DeviceProfile.</returns>
    DlnaDeviceProfile? GetProfile(string id);

    /// <summary>
    /// Gets the profile.
    /// </summary>
    /// <param name="deviceInfo">The device information.</param>
    /// <returns>DeviceProfile.</returns>
    DlnaDeviceProfile? GetProfile(DeviceIdentification deviceInfo);

    /// <summary>
    /// Gets the server description XML.
    /// </summary>
    /// <param name="headers">The headers.</param>
    /// <param name="serverUuId">The server uu identifier.</param>
    /// <param name="serverAddress">The server address.</param>
    /// <returns>System.String.</returns>
    string GetServerDescriptionXml(IHeaderDictionary headers, string serverUuId, string serverAddress);

    /// <summary>
    /// Gets the icon.
    /// </summary>
    /// <param name="filename">The filename.</param>
    /// <returns>DlnaIconResponse.</returns>
    Stream? GetIcon(string filename);
}
