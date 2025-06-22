// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using TablePlusCommandPalet.Commands;
using TablePlusCommandPalet.Services;

namespace TablePlusCommandPalet;

internal sealed partial class TablePlusCommandPaletPage : ListPage
{
    private readonly TablePlusConnectionService _connectionService;

    public TablePlusCommandPaletPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "TablePlus";
        Name = "Open";
        _connectionService = new TablePlusConnectionService();
    }

    public override IListItem[] GetItems()
    {
        var connectionsWithGroups = _connectionService.GetConnectionsWithGroups().ToList();

        if (!connectionsWithGroups.Any())
        {
            return [
                new ListItem(new NoOpCommand()) { Title = "No TablePlus connections found" }
            ];
        }

        return connectionsWithGroups
            .Select(item => {
                var (connection, group) = item;
                var command = new OpenTablePlusConnectionCommand(connection, group);
                
                var title = $"{connection.ConnectionName}";

                var customConnection = connection.IsOverSSH ? "SSH: " : connection.IsSocket ? "SOCKET: " : string.Empty;

                var subtitle = $"🗂️ {group.Name} 🔌{customConnection}{connection.Host}";

                if (connection.Driver == "SQLite")
                {
                    subtitle += $" : ${connection.Database}";
                } else if (connection.Driver == "SQLite" && connection.IsOverSSH)
                {
                    subtitle += $" : {connection.Host}";
                }


                return new ListItem(command)
                {
                    Title = title,
                    Subtitle = subtitle,
                    Tags = new ITag[] {
                        GetDatabaseTag(connection.Driver),
                        GetEnvironmentTag(connection.Environment),
                    },
                    Section = group.Name,
                    Icon = GetDatabaseIcon(connection.Driver)
                };
            })
            .OrderBy(x => x.Subtitle).ThenBy(x => x.Title)
            .ToArray();
    }

    private static Tag GetEnvironmentTag(string environment)
    {
        return environment.ToLowerInvariant() switch
        {
            "local" => new Tag("LOCAL") 
            { 
                Background = ColorHelpers.FromRgb(0, 128, 0), // Dark Green
                Foreground = ColorHelpers.FromRgb(255, 255, 255), // White
                ToolTip = "Local Environment"
            },
            "staging" => new Tag("Staging") 
            { 
                Background = ColorHelpers.FromRgb(255, 165, 0), // Orange
                Foreground = ColorHelpers.FromRgb(255, 255, 255), // White
                ToolTip = "Staging Environment"
            },
            "production" => new Tag("Production") 
            { 
                Background = ColorHelpers.FromArgb(255, 238, 94, 97),
                Foreground = ColorHelpers.FromRgb(255, 255, 255), // White
                ToolTip = "Production Environment - Use with caution!"
            },
            _ => new Tag(environment.ToUpperInvariant()) 
            { 
                Background = ColorHelpers.FromArgb(100, 76, 227, 222),
                Foreground = ColorHelpers.FromRgb(255, 255, 255), // White
                ToolTip = $"{environment} Environment",
            }
        };
    }

    private static Tag GetDatabaseTag(string driver)
    {
        return new Tag(driver)
        {
            ToolTip = $"{driver}"
        };
    }

    private static IconInfo GetDatabaseIcon(string driver)
    {
        return IconHelpers.FromRelativePath($"Assets\\StoreLogo.png");
    }
}
