using System;
using System.Collections.Generic;

namespace TablePlusCommandPalet.Models;

public class TablePlusConnection
{
    public string ID { get; set; } = string.Empty;
    public string ConnectionName { get; set; } = string.Empty;
    public string Driver { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string GroupID { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public string Port { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public bool UseSSL { get; set; }
    public bool UsePrivateKey { get; set; }
    public int SafeModeLevel { get; set; }
    public int AdvancedSafeModeLevel { get; set; }
    public int DriverVersion { get; set; }
    public bool ShowSystemSchemas { get; set; }
    public bool LazyLoad { get; set; }
    public int TLSMode { get; set; }
    public bool IsOverSSH { get; set; }
    public bool IsSocket { get; set; }

    public string GenerateDatabaseUrl()
    {
        return $"tableplus://?id={ID}";
    }
}