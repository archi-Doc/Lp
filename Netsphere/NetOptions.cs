// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using SimpleCommandLine;

namespace Netsphere;

[TinyhandObject(ImplicitKeyAsName = true)]
public partial record NetOptions
{
    [SimpleOption("NodeName", Description = "Node name")]
    public string NodeName { get; set; } = string.Empty;

    // [SimpleOption("address", Description = "Global IP address")]
    // public string Address { get; set; } = string.Empty;

    [SimpleOption("Port", Description = "Port number associated with the address")]
    public int Port { get; set; }

    [SimpleOption("NodePrivatekey", Description = "Node private key")]
    public string NodePrivateKey { get; set; } = string.Empty;

    [SimpleOption("NodeList", Description = "Node addresses to connect")]
    public string NodeList { get; set; } = string.Empty;

    [SimpleOption("Phase", Description = "Network phase")]
    public int Phase { get; set; }

    [SimpleOption("Ping", Description = "Enable ping function")]
    public bool EnablePing { get; set; } = true;

    [SimpleOption("Server", Description = "Enable server function")]
    public bool EnableServer { get; set; } = false;

    [SimpleOption("Alternative", Description = "Enable alternative (debug) terminal")]
    public bool EnableAlternative { get; set; } = false;
}
