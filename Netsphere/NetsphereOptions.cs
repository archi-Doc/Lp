// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Netsphere;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial class NetsphereOptions : ILogInformation
{
    [SimpleOption("address", Description = "Global IP address")]
    public string Address { get; set; } = string.Empty;

    [SimpleOption("port", Description = "Port number associated with the address")]
    public int Port { get; set; }

    [SimpleOption("nodename", Description = "Name of this node")]
    public string NodeName { get; set; } = string.Empty;

    [SimpleOption("node", Description = "Node addresses to connect")]
    public string Nodes { get; set; } = string.Empty;

    [SimpleOption("essential", Description = "Enable essential network function")]
    public bool EnableEssential { get; set; } = true;

    [SimpleOption("server", Description = "Enable server function")]
    public bool EnableServer { get; set; } = false;

    [SimpleOption("alternative", Description = "Enable alternative (debug) terminal")]
    public bool EnableAlternative { get; set; } = false;

    [SimpleOption("logger", Description = "Enable network logger")]
    public bool EnableLogger { get; set; } = false;

    public void LogInformation(ILog logger)
    {
        logger.Log($"Address: {this.Address}, Port: {this.Port}, Alternative: {this.EnableAlternative}");
    }
}
