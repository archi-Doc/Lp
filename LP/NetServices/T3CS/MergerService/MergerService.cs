// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using static LP.NetServices.T3CS.MergerService;

namespace LP.NetServices.T3CS;

[NetServiceInterface]
public interface MergerService : INetService
{
    NetTask<InformationResult?> Information();

    public record InformationResult(string Name);

    // NetTask<NetResult> CreateCredit();
}

[NetServiceFilter(typeof(MergerOrTestFilter))]
[NetServiceObject]
public class MergerServiceImpl : MergerService
{// LPCallContext.Current
    public MergerServiceImpl()
    {
    }

    public async NetTask<InformationResult?> Information()
    {
        var callContext = LPCallContext.Current;

        return new("Merger1");
    }
}
