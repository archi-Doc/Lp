// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Options;

public class NetsphereOptions
{
    [SimpleOption("address", null, "Global IP address")]
    public string Address { get; set; } = string.Empty;

    [SimpleOption("port", null, "Port number associated with the address")]
    public int Port { get; set; }

    [SimpleOption("node", null, "Node addresses to connect")]
    public string Nodes { get; set; } = string.Empty;

    [SimpleOption("alternative", null, "Enable alternative (debug) terminal")]
    public bool EnableAlternative { get; set; } = false;

    [SimpleOption("logger", null, "Enable loggerl")]
    public bool EnableLogger { get; set; } = false;

    [SimpleOption("test", null, "Enable test functions")]
    public bool EnableTest { get; set; } = false;

    public override string ToString()
    {
        return $"Address: {this.Address}, Port: {this.Port}";
    }
}
