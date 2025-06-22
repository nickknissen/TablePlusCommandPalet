using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Claunia.PropertyList;
using TablePlusCommandPalet.Models;

namespace TablePlusCommandPalet.Services;

public class TablePlusConnectionService
{
    private readonly string _connectionsPath;
    private readonly string _connectionGroupsPath;

    public TablePlusConnectionService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var tablePlusPath = Path.Combine(appDataPath, "com.tinyapp.TablePlus", "data");
        
        _connectionsPath = Path.Combine(tablePlusPath, "Connections.plist");
        _connectionGroupsPath = Path.Combine(tablePlusPath, "ConnectionGroups.plist");
    }

    public IEnumerable<TablePlusConnection> GetConnections()
    {
        if (!File.Exists(_connectionsPath))
        {
            return Enumerable.Empty<TablePlusConnection>();
        }

        try
        {
            var plistData = PropertyListParser.Parse(_connectionsPath);
            var connections = new List<TablePlusConnection>();

            if (plistData is NSArray plistArray)
            {
                foreach (var item in plistArray)
                {
                    if (item is NSDictionary connectionDict)
                    {
                        var connection = ParseConnection(connectionDict);
                        if (connection != null)
                        {
                            connections.Add(connection);
                        }
                    }
                }
            }

            return connections;
        }
        catch (Exception)
        {
            return Enumerable.Empty<TablePlusConnection>();
        }
    }

    public IEnumerable<TablePlusConnectionGroup> GetConnectionGroups()
    {
        if (!File.Exists(_connectionGroupsPath))
        {
            return Enumerable.Empty<TablePlusConnectionGroup>();
        }

        try
        {
            var plistData = PropertyListParser.Parse(_connectionGroupsPath);
            var groups = new List<TablePlusConnectionGroup>();

            if (plistData is NSArray plistArray)
            {
                foreach (var item in plistArray)
                {
                    if (item is NSDictionary groupDict)
                    {
                        var group = ParseConnectionGroup(groupDict);
                        if (group != null)
                        {
                            groups.Add(group);
                        }
                    }
                }
            }

            return groups;
        }
        catch (Exception)
        {
            return Enumerable.Empty<TablePlusConnectionGroup>();
        }
    }

    public IEnumerable<(TablePlusConnection Connection, TablePlusConnectionGroup Group)> GetConnectionsWithGroups()
    {
        var connections = GetConnections();
        var groups = GetConnectionGroups().ToList();

        var emptyGroup = new TablePlusConnectionGroup
        {
            ID = "__EMPTY__",
            Name = "Ungrouped"
        };

        return connections.Select(connection =>
        {
            var group = groups.FirstOrDefault(g => g.ID == connection.GroupID, emptyGroup);

            return (connection, group);
        });
    }

    private static TablePlusConnection? ParseConnection(NSDictionary connectionDict)
    {
        try
        {
            var connection = new TablePlusConnection();

            if (connectionDict.ContainsKey("ID"))
                connection.ID = connectionDict["ID"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("ConnectionName"))
                connection.ConnectionName = connectionDict["ConnectionName"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("Driver"))
                connection.Driver = connectionDict["Driver"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("Enviroment"))
                connection.Environment = connectionDict["Enviroment"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("GroupID"))
                connection.GroupID = connectionDict["GroupID"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("DatabaseHost"))
                connection.Host = connectionDict["DatabaseHost"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("DatabasePort"))
                connection.Port = connectionDict["DatabasePort"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("DatabaseName"))
                connection.Database = connectionDict["DatabaseName"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("DatabaseUser"))
                connection.User = connectionDict["DatabaseUser"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("DatabasePassword"))
                connection.Password = connectionDict["DatabasePassword"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("StatusColor"))
                connection.StatusColor = connectionDict["StatusColor"]?.ToString() ?? string.Empty;

            if (connectionDict.ContainsKey("isUseSSL") && connectionDict["isUseSSL"] is NSNumber useSSL)
                connection.UseSSL = useSSL.ToBool();

            if (connectionDict.ContainsKey("isUsePrivateKey") && connectionDict["isUsePrivateKey"] is NSNumber usePrivateKey)
                connection.UsePrivateKey = usePrivateKey.ToBool();

            if (connectionDict.ContainsKey("safeModeLevel") && connectionDict["safeModeLevel"] is NSNumber safeModeLevel)
                connection.SafeModeLevel = safeModeLevel.ToInt();

            if (connectionDict.ContainsKey("advancedSafeModeLevel") && connectionDict["advancedSafeModeLevel"] is NSNumber advancedSafeModeLevel)
                connection.AdvancedSafeModeLevel = advancedSafeModeLevel.ToInt();

            if (connectionDict.ContainsKey("driverVersion") && connectionDict["driverVersion"] is NSNumber driverVersion)
                connection.DriverVersion = driverVersion.ToInt();

            if (connectionDict.ContainsKey("isShowSystemSchema") && connectionDict["isShowSystemSchema"] is NSNumber showSystemSchemas)
                connection.ShowSystemSchemas = showSystemSchemas.ToBool();

            if (connectionDict.ContainsKey("isLazyLoading") && connectionDict["isLazyLoading"] is NSNumber lazyLoad)
                connection.LazyLoad = lazyLoad.ToBool();

            if (connectionDict.ContainsKey("tLSMode") && connectionDict["tLSMode"] is NSNumber tlsMode)
                connection.TLSMode = tlsMode.ToInt();

            if (connectionDict.ContainsKey("isOverSSH") && connectionDict["isOverSSH"] is NSNumber isOverSSH)
                connection.IsOverSSH = isOverSSH.ToBool();

            if (connectionDict.ContainsKey("isSocket") && connectionDict["isSocket"] is NSNumber isSocket)
                connection.IsSocket = isSocket.ToBool();

            return connection;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static TablePlusConnectionGroup? ParseConnectionGroup(NSDictionary groupDict)
    {
        try
        {
            var group = new TablePlusConnectionGroup();

            if (groupDict.ContainsKey("ID"))
                group.ID = groupDict["ID"]?.ToString() ?? string.Empty;

            if (groupDict.ContainsKey("Name"))
                group.Name = groupDict["Name"]?.ToString() ?? string.Empty;

            return group;
        }
        catch (Exception)
        {
            return null;
        }
    }
}