// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[TinyhandObject(ReservedKeyCount = 2)]
public partial class SetupRelayBlock
{
    public SetupRelayBlock()
    {
    }

    [Key(0)]
    public NetEndpoint OuterEndPoint { get; set; }
}

[TinyhandObject]
public partial class SetupRelayResponse
{
    public SetupRelayResponse(RelayResult result)
    {
        this.Result = result;
    }

    protected SetupRelayResponse()
    {
    }

    [Key(0)]
    public RelayResult Result { get; protected set; }
}
