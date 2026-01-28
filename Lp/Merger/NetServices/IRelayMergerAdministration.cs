// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp.T3cs;

[NetServiceInterface]
public partial interface IRelayMergerAdministration : IMergerAdministration
{
}

[NetServiceObject]
internal class RelayMergerAdministrationAgent : MergerAdministrationAgent
{
    public RelayMergerAdministrationAgent(LpBase lpBase, RelayMerger merger)
        : base(lpBase, merger)
    {
    }
}
