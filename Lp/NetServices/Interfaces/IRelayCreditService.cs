// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IRelayCreditService : INetService
{
    NetTask<RelayStatus?> SetRelayStatus(Credit relayCredit, NetAddress address);
}
