// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Relay;

[TinyhandObject(ReservedKeyCount = 2)]
public partial class SetupRelayBlock
{
    public const ulong DataId = 0xD9BB_F628_2326_43CA;

    public SetupRelayBlock()
    {
    }

    public SetupRelayBlock(NetEndpoint outerEndpoint)
    {
        this.OuterEndpoint = outerEndpoint;
    }

    [Key(0)]
    public NetEndpoint OuterEndpoint { get; set; }
}

[TinyhandObject]
public partial class SetupRelayResponse
{
    public SetupRelayResponse()
    {
    }

    public SetupRelayResponse(RelayResult result)
    {
        this.Result = result;
    }

    [Key(0)]
    public RelayResult Result { get; protected set; }
}
