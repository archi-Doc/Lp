// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using SimpleCommandLine;

namespace LP.Net;

public class NetsphereOptions
{
    [SimpleOption("address", null, "Global IP address")]
    public string Address { get; set; } = string.Empty;

    [SimpleOption("port", null, "Port number associated with the address")]
    public int Port { get; set; }

    public override string ToString()
    {
        return $"Address: {this.Address}, Port: {this.Port}";
    }
}
