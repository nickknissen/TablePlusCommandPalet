using System;
using System.Diagnostics;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using TablePlusCommandPalet.Models;

namespace TablePlusCommandPalet.Commands;

internal sealed partial class OpenTablePlusConnectionCommand : InvokableCommand
{
    private readonly TablePlusConnection _connection;
    private readonly TablePlusConnectionGroup? _group;

    public OpenTablePlusConnectionCommand(TablePlusConnection connection, TablePlusConnectionGroup? group = null)
    {
        _connection = connection;
        _group = group;
    }

    public override string Name => $"Open {_connection.ConnectionName}";

    public override CommandResult Invoke()
    {
        try
        {
            var databaseUrl = _connection.GenerateDatabaseUrl();
            
            var startInfo = new ProcessStartInfo
            {
                FileName = "tableplus",
                Arguments = $"\"{databaseUrl}\"",
                UseShellExecute = true,
                CreateNoWindow = true
            };

            Process.Start(startInfo);

            return CommandResult.Dismiss();
        }
        catch (Exception ex)
        {
            var errorStartInfo = new ProcessStartInfo
            {
                FileName = "cmd",
                Arguments = $"/c echo Error opening TablePlus connection: {ex.Message} & pause",
                UseShellExecute = true
            };
            
            Process.Start(errorStartInfo);
            
            return CommandResult.KeepOpen();
        }
    }
}