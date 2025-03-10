using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Jellyfin.Plugin.Dlna.Common;
using Jellyfin.Plugin.Dlna.Ssdp;

namespace Jellyfin.Plugin.Dlna.PlayTo;

/// <summary>
/// Defines the <see cref="TransportCommands" />.
/// </summary>
public class TransportCommands
{
    private static readonly CompositeFormat _commandBase = CompositeFormat.Parse("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n" + "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" + "<SOAP-ENV:Body>" + "<m:{0} xmlns:m=\"{1}\">" + "{2}" + "</m:{0}>" + "</SOAP-ENV:Body></SOAP-ENV:Envelope>");

    /// <summary>
    /// Gets or sets the state variables.
    /// </summary>
    public IReadOnlyList<StateVariable> StateVariables { get; set; } = [];

    /// <summary>
    /// Gets or sets the service actions.
    /// </summary>
    public IReadOnlyList<ServiceAction> ServiceActions { get; set; } = [];

    /// <summary>
    /// Creates <see cref="TransportCommands"/> based on the input <see cref="XDocument"/>.
    /// </summary>
    /// <param name="document">The <see cref="XDocument"/>.</param>
    public static TransportCommands Create(XDocument document)
    {
        var actionList = document.Descendants(UPnpNamespaces.Svc + "actionList");
        var stateValues = document.Descendants(UPnpNamespaces.ServiceStateTable).FirstOrDefault();

        return new()
        {
            ServiceActions = GetServiceActions(actionList),
            StateVariables = GetStateVariables(stateValues)
        };
    }

    private static List<StateVariable> GetStateVariables(XElement? stateValues)
    {
        return stateValues?.Descendants(UPnpNamespaces.Svc + "stateVariable").Select(FromXml).ToList() ?? [];
    }

    private static List<ServiceAction> GetServiceActions(IEnumerable<XElement> actionList)
    {
        return actionList.Descendants(UPnpNamespaces.Svc + "action").Select(ServiceActionFromXml).ToList();
    }

    private static ServiceAction ServiceActionFromXml(XElement container)
    {
        return new()
        {
            Name = container.GetValue(UPnpNamespaces.Svc + "name") ?? string.Empty,
            ArgumentList = GetArguments(container)
        };
    }

    private static List<Argument> GetArguments(XElement container)
    {
        return container.Descendants(UPnpNamespaces.Svc + "argument").Select(ArgumentFromXml).ToList();
    }

    private static Argument ArgumentFromXml(XElement container)
    {
        ArgumentNullException.ThrowIfNull(container);

        return new Argument
        {
            Name = container.GetValue(UPnpNamespaces.Svc + "name") ?? string.Empty,
            Direction = container.GetValue(UPnpNamespaces.Svc + "direction") ?? string.Empty,
            RelatedStateVariable = container.GetValue(UPnpNamespaces.Svc + "relatedStateVariable") ?? string.Empty
        };
    }

    private static StateVariable FromXml(XElement container)
    {
        var allowedValues = Array.Empty<string>();
        var element = container.Descendants(UPnpNamespaces.Svc + "allowedValueList")
            .FirstOrDefault();

        if (element is not null)
        {
            var values = element.Descendants(UPnpNamespaces.Svc + "allowedValue");

            allowedValues = values.Select(child => child.Value).ToArray();
        }

        return new StateVariable
        {
            Name = container.GetValue(UPnpNamespaces.Svc + "name") ?? string.Empty,
            DataType = container.GetValue(UPnpNamespaces.Svc + "dataType") ?? string.Empty,
            AllowedValues = allowedValues
        };
    }

    /// <summary>
    /// Builds the POST payload for a <see cref="ServiceAction"/>.
    /// </summary>
    /// <param name="action">The <see cref="ServiceAction"/>.</param>
    /// <param name="xmlNamespace">The XML namespace.</param>
    public string BuildPost(ServiceAction action, string xmlNamespace)
    {
        var stateString = string.Empty;

        foreach (var arg in action.ArgumentList)
        {
            if (string.Equals(arg.Direction, "out", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(arg.Name, "InstanceID", StringComparison.Ordinal))
            {
                stateString += BuildArgumentXml(arg, "0");
            }
            else
            {
                stateString += BuildArgumentXml(arg, null);
            }
        }

        return string.Format(CultureInfo.InvariantCulture, _commandBase, action.Name, xmlNamespace, stateString);
    }

    /// <summary>
    /// Builds the POST payload for a <see cref="ServiceAction"/>.
    /// </summary>
    /// <param name="action">The <see cref="ServiceAction"/>.</param>
    /// <param name="xmlNamespace">The XML namespace.</param>
    /// <param name="value">The value.</param>
    /// <param name="commandParameter">The command parameter.</param>
    public string BuildPost(ServiceAction action, string xmlNamespace, object value, string commandParameter = "")
    {
        var stateString = string.Empty;

        foreach (var arg in action.ArgumentList)
        {
            if (string.Equals(arg.Direction, "out", StringComparison.Ordinal))
            {
                continue;
            }

            if (string.Equals(arg.Name, "InstanceID", StringComparison.Ordinal))
            {
                stateString += BuildArgumentXml(arg, "0");
            }
            else
            {
                stateString += BuildArgumentXml(arg, value.ToString(), commandParameter);
            }
        }

        return string.Format(CultureInfo.InvariantCulture, _commandBase, action.Name, xmlNamespace, stateString);
    }

    /// <summary>
    /// Builds the POST payload for a <see cref="ServiceAction"/>.
    /// </summary>
    /// <param name="action">The <see cref="ServiceAction"/>.</param>
    /// <param name="xmlNamespace">The XML namespace.</param>
    /// <param name="value">The value.</param>
    /// <param name="argumentValueDictionary">The argument values.</param>
    public string BuildPost(ServiceAction action, string xmlNamespace, object value, Dictionary<string, string> argumentValueDictionary)
    {
        var stateString = string.Empty;

        foreach (var arg in action.ArgumentList)
        {
            if (string.Equals(arg.Name, "InstanceID", StringComparison.Ordinal))
            {
                stateString += BuildArgumentXml(arg, "0");
            }
            else if (argumentValueDictionary.TryGetValue(arg.Name, out var argValue))
            {
                stateString += BuildArgumentXml(arg, argValue);
            }
            else
            {
                stateString += BuildArgumentXml(arg, value.ToString());
            }
        }

        return string.Format(CultureInfo.InvariantCulture, _commandBase, action.Name, xmlNamespace, stateString);
    }

    private string BuildArgumentXml(Argument argument, string? value, string commandParameter = "")
    {
        var state = StateVariables.FirstOrDefault(a => string.Equals(a.Name, argument.RelatedStateVariable, StringComparison.OrdinalIgnoreCase));

        if (state is not null)
        {
            var sendValue = state.AllowedValues.FirstOrDefault(a => string.Equals(a, commandParameter, StringComparison.OrdinalIgnoreCase)) ??
                (state.AllowedValues.Count > 0 ? state.AllowedValues[0] : value);

            return string.Format(CultureInfo.InvariantCulture, "<{0} xmlns:dt=\"urn:schemas-microsoft-com:datatypes\" dt:dt=\"{1}\">{2}</{0}>", argument.Name, state.DataType, sendValue);
        }

        return string.Format(CultureInfo.InvariantCulture, "<{0}>{1}</{0}>", argument.Name, value);
    }
}
