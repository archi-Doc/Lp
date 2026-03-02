// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IRelayMergerService : IMergerService
{
    Task<RelayStatus?> GetRelayStatus(Credit relayCredit);
}
