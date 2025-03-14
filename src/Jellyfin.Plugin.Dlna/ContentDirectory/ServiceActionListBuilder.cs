using System.Collections.Generic;
using Jellyfin.Plugin.Dlna.Common;

namespace Jellyfin.Plugin.Dlna.ContentDirectory;

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
            GetSearchCapabilitiesAction(),
            GetSortCapabilitiesAction(),
            GetGetSystemUpdateIDAction(),
            GetBrowseAction(),
            GetSearchAction(),
            GetX_GetFeatureListAction(),
            GetXSetBookmarkAction(),
            GetBrowseByLetterAction()
        ];
    }

    /// <summary>
    /// Returns the action details for "GetSystemUpdateID".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetGetSystemUpdateIDAction()
    {
        var action = new ServiceAction
        {
            Name = "GetSystemUpdateID",
            ArgumentList = [
                new Argument
                {
                    Name = "Id",
                    Direction = "out",
                    RelatedStateVariable = "SystemUpdateID"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "GetSearchCapabilities".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetSearchCapabilitiesAction()
    {
        var action = new ServiceAction
        {
            Name = "GetSearchCapabilities",
            ArgumentList = [
                new Argument
                {
                    Name = "SearchCaps",
                    Direction = "out",
                    RelatedStateVariable = "SearchCapabilities"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "GetSortCapabilities".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetSortCapabilitiesAction()
    {
        var action = new ServiceAction
        {
            Name = "GetSortCapabilities",
            ArgumentList = [
                new Argument
                {
                    Name = "SortCaps",
                    Direction = "out",
                    RelatedStateVariable = "SortCapabilities"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "X_GetFeatureList".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetX_GetFeatureListAction()
    {
        var action = new ServiceAction
        {
            Name = "X_GetFeatureList",
            ArgumentList = [
                new Argument
                {
                    Name = "FeatureList",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Featurelist"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "Search".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetSearchAction()
    {
        var action = new ServiceAction
        {
            Name = "Search",
            ArgumentList = [
                new Argument
                {
                    Name = "ContainerID",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_ObjectID"
                },
                new Argument
                {
                    Name = "SearchCriteria",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_SearchCriteria"
                },
                new Argument
                {
                    Name = "Filter",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Filter"
                },
                new Argument
                {
                    Name = "StartingIndex",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Index"
                },
                new Argument
                {
                    Name = "RequestedCount",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "SortCriteria",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_SortCriteria"
                },
                new Argument
                {
                    Name = "Result",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Result"
                },
                new Argument
                {
                    Name = "NumberReturned",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "TotalMatches",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "UpdateID",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_UpdateID"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "Browse".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetBrowseAction()
    {
        var action = new ServiceAction
        {
            Name = "Browse",
            ArgumentList = [
                new Argument
                {
                    Name = "ObjectID",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_ObjectID"
                },
                new Argument
                {
                    Name = "BrowseFlag",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_BrowseFlag"
                },
                new Argument
                {
                    Name = "Filter",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Filter"
                },
                new Argument
                {
                    Name = "StartingIndex",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Index"
                },
                new Argument
                {
                    Name = "RequestedCount",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "SortCriteria",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_SortCriteria"
                },
                new Argument
                {
                    Name = "Result",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Result"
                },
                new Argument
                {
                    Name = "NumberReturned",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "TotalMatches",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "UpdateID",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_UpdateID"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "X_BrowseByLetter".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetBrowseByLetterAction()
    {
        var action = new ServiceAction
        {
            Name = "X_BrowseByLetter",
            ArgumentList = [
                new Argument
                {
                    Name = "ObjectID",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_ObjectID"
                },
                new Argument
                {
                    Name = "BrowseFlag",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_BrowseFlag"
                },
                new Argument
                {
                    Name = "Filter",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Filter"
                },
                new Argument
                {
                    Name = "StartingLetter",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_BrowseLetter"
                },
                new Argument
                {
                    Name = "RequestedCount",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "SortCriteria",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_SortCriteria"
                },
                new Argument
                {
                    Name = "Result",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Result"
                },
                new Argument
                {
                    Name = "NumberReturned",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "TotalMatches",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Count"
                },
                new Argument
                {
                    Name = "UpdateID",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_UpdateID"
                },
                new Argument
                {
                    Name = "StartingIndex",
                    Direction = "out",
                    RelatedStateVariable = "A_ARG_TYPE_Index"
                }
            ]
        };

        return action;
    }

    /// <summary>
    /// Returns the action details for "X_SetBookmark".
    /// </summary>
    /// <returns>The <see cref="ServiceAction"/>.</returns>
    private static ServiceAction GetXSetBookmarkAction()
    {
        var action = new ServiceAction
        {
            Name = "X_SetBookmark",
            ArgumentList = [
                new Argument
                {
                    Name = "CategoryType",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_CategoryType"
                },
                new Argument
                {
                    Name = "RID",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_RID"
                },
                new Argument
                {
                    Name = "ObjectID",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_ObjectID"
                },
                new Argument
                {
                    Name = "PosSecond",
                    Direction = "in",
                    RelatedStateVariable = "A_ARG_TYPE_PosSec"
                }
            ]
        };

        return action;
    }
}
