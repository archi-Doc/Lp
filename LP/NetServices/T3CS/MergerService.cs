// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.NetServices;
using Netsphere;

namespace LP.T3CS;

[NetServiceInterface]
public interface MergerService : INetService
{
    NetTask<string?> Information();

    NetTask<NetResult> CreateCredit();
}

[NetServiceFilter(typeof(MergerOrTestFilter))]
[NetServiceObject]
public class MergerServiceImpl : MergerService
{
    public MergerServiceImpl()
    {
    }

    public async NetTask<string?> Information()
    {
        return "Merger1";
    }
}
