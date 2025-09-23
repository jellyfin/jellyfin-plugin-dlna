using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Common;

namespace Jellyfin.Plugin.Dlna.MediaReceiverRegistrar;

/// <summary>
/// Defines the <see cref="ServiceActionListBuilder" />.
/// </summary>
public static class ServiceActionListBuilder
{
    /// <summary>
    /// Returns a list of services that this instance provides.
    /// </summary>
    /// <returns>An <see cref="IEnumerable{ServiceAction}"/>.</returns>
    public static IEnumerable<ServiceAction> GetActions()
    {
        return
        [
            GetIsValidated(),
            GetIsAuthorized(),
            GetRegisterDevice(),
            GetGetAuthorizationDeniedUpdateID(),
            GetGetAuthorizationGrantedUpdateID(),
            GetGetValidationRevokedUpdateID(),
            GetGetValidationSucceededUpdateID()
        ];
    }

    /// <summary>
    /// Returns the action details for "IsValidated".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetIsValidated()
    {
        var action = new ServiceAction
        {
            Name = "IsValidated",
            ArgumentList = [
                new Argument
                {
                    Name = "DeviceID",
                    Direction = "in"
                },
                new Argument
                {
                    Name = "Result",
                    Direction = "out"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "IsAuthorized".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetIsAuthorized()
    {
        var action = new ServiceAction
        {
            Name = "IsAuthorized",
            ArgumentList = [
                new Argument
                {
                    Name = "DeviceID",
                    Direction = "in"
                },
                new Argument
                {
                    Name = "Result",
                    Direction = "out"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "RegisterDevice".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetRegisterDevice()
    {
        var action = new ServiceAction
        {
            Name = "RegisterDevice",
            ArgumentList = [
                new Argument
                {
                    Name = "RegistrationReqMsg",
                    Direction = "in"
                },
                new Argument
                {
                    Name = "RegistrationRespMsg",
                    Direction = "out"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "GetValidationSucceededUpdateID".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetGetValidationSucceededUpdateID()
    {
        var action = new ServiceAction
        {
            Name = "GetValidationSucceededUpdateID",
            ArgumentList = [
                new Argument
                {
                    Name = "ValidationSucceededUpdateID",
                    Direction = "out"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "GetGetAuthorizationDeniedUpdateID".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetGetAuthorizationDeniedUpdateID()
    {
        var action = new ServiceAction
        {
            Name = "GetAuthorizationDeniedUpdateID",
            ArgumentList = [
                new Argument
                {
                    Name = "AuthorizationDeniedUpdateID",
                    Direction = "out"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "GetValidationRevokedUpdateID".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetGetValidationRevokedUpdateID()
    {
        var action = new ServiceAction
        {
            Name = "GetValidationRevokedUpdateID",
            ArgumentList = [
                new Argument
                {
                    Name = "ValidationRevokedUpdateID",
                    Direction = "out"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "GetAuthorizationGrantedUpdateID".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetGetAuthorizationGrantedUpdateID()
    {
        var action = new ServiceAction
        {
            Name = "GetAuthorizationGrantedUpdateID",
            ArgumentList = [
                new Argument
                {
                    Name = "AuthorizationGrantedUpdateID",
                    Direction = "out"
                }
            ]
        };

        return action;
    }
}
