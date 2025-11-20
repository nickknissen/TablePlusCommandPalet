using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Claunia.PropertyList;
using TablePlusCommandPalet.Models;

namespace TablePlusCommandPalet.Services;

public class TablePlusConnectionService
{
    private readonly string _connectionsPath;
    private readonly string _connectionGroupsPath;

    private readonly object _connectionsLock = new();
    private readonly object _connectionGroupsLock = new();
    private IReadOnlyList<TablePlusConnection> _connectionsCache = Array.Empty<TablePlusConnection>();
    private IReadOnlyList<TablePlusConnectionGroup> _connectionGroupsCache = Array.Empty<TablePlusConnectionGroup>();
    private DateTime _connectionsLastWriteTimeUtc = DateTime.MinValue;
    private DateTime _connectionGroupsLastWriteTimeUtc = DateTime.MinValue;
    private bool _connectionsCacheInitialized;
    private bool _connectionGroupsCacheInitialized;

    public TablePlusConnectionService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var tablePlusPath = Path.Combine(appDataPath, "com.tinyapp.TablePlus", "data");
        
        _connectionsPath = Path.Combine(tablePlusPath, "Connections.plist");
        _connectionGroupsPath = Path.Combine(tablePlusPath, "ConnectionGroups.plist");

        // Warm up caches on the thread pool so the UI thread does not wait the first time.
        Task.Run(() =>
        {
            try
            {
                _ = GetConnections();
                _ = GetConnectionGroups();
            }
            catch
            {
                // Ignore warm-up failures; GetConnections/GetConnectionGroups will retry on demand.
            }
        });
    }

    public IReadOnlyList<TablePlusConnection> GetConnections()
    {
        var fileTimestamp = GetLastWriteTimeUtcSafe(_connectionsPath);

        lock (_connectionsLock)
        {
            if (_connectionsCacheInitialized && fileTimestamp == _connectionsLastWriteTimeUtc)
            {
                return _connectionsCache;
            }
        }

        var connections = LoadConnectionsFromDisk();

        lock (_connectionsLock)
        {
            _connectionsCache = connections;
            _connectionsLastWriteTimeUtc = fileTimestamp;
            _connectionsCacheInitialized = true;
            return _connectionsCache;
        }
    }

    public IReadOnlyList<TablePlusConnectionGroup> GetConnectionGroups()
    {
        var fileTimestamp = GetLastWriteTimeUtcSafe(_connectionGroupsPath);

        lock (_connectionGroupsLock)
        {
            if (_connectionGroupsCacheInitialized && fileTimestamp == _connectionGroupsLastWriteTimeUtc)
            {
                return _connectionGroupsCache;
            }
        }

        var groups = LoadGroupsFromDisk();

        lock (_connectionGroupsLock)
        {
            _connectionGroupsCache = groups;
            _connectionGroupsLastWriteTimeUtc = fileTimestamp;
            _connectionGroupsCacheInitialized = true;
            return _connectionGroupsCache;
        }
    }

    public IReadOnlyList<(TablePlusConnection Connection, TablePlusConnectionGroup Group)> GetConnectionsWithGroups()
    {
        var connections = GetConnections();
        if (connections.Count == 0)
        {
            return Array.Empty<(TablePlusConnection, TablePlusConnectionGroup)>();
        }

        var groups = GetConnectionGroups();
        var groupMap = groups.Count == 0
            ? new Dictionary<string, TablePlusConnectionGroup>(StringComparer.Ordinal)
            : groups
                .Where(group => !string.IsNullOrEmpty(group.ID))
                .GroupBy(group => group.ID)
                .ToDictionary(g => g.Key!, g => g.First(), StringComparer.Ordinal);

        var emptyGroup = new TablePlusConnectionGroup
        {
            ID = "__EMPTY__",
            Name = "Ungrouped"
        };

        var result = new List<(TablePlusConnection, TablePlusConnectionGroup)>(connections.Count);

        foreach (var connection in connections)
        {
            if (!string.IsNullOrEmpty(connection.GroupID) &&
                groupMap.TryGetValue(connection.GroupID, out var group))
            {
                result.Add((connection, group));
            }
            else
            {
                result.Add((connection, emptyGroup));
            }
        }

        return result;
    }

    private IReadOnlyList<TablePlusConnection> LoadConnectionsFromDisk()
    {
        if (!File.Exists(_connectionsPath))
        {
            return Array.Empty<TablePlusConnection>();
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
            return Array.Empty<TablePlusConnection>();
        }
    }

    private IReadOnlyList<TablePlusConnectionGroup> LoadGroupsFromDisk()
    {
        if (!File.Exists(_connectionGroupsPath))
        {
            return Array.Empty<TablePlusConnectionGroup>();
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
            return Array.Empty<TablePlusConnectionGroup>();
        }
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

    private static DateTime GetLastWriteTimeUtcSafe(string path)
    {
        try
        {
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}
