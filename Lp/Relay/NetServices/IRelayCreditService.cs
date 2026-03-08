// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

[NetService]
public partial interface IRelayCreditService : INetService
{
    Task<RelayStatus?> SetRelayStatus(Credit relayCredit, NetAddress address);
}
