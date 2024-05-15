// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace LP.T3CS;

[NetServiceInterface]
public partial interface IRelayCreditService : INetService
{
    NetTask<RelayStatus?> SetRelayStatus(Credit relayCredit, NetAddress address);
}
