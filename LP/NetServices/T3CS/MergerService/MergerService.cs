// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace LP.NetServices.T3CS;

[NetServiceInterface]
public partial interface MergerService : INetService
{
    NetTask<InformationResult?> GetInformation();

    // [TinyhandObject]
    // public partial record InformationResult([property: Key(0)] string Name);

    [TinyhandObject]
    public partial record InformationResult
    {
        public InformationResult()
        {
        }

        [Key(0, PropertyName = "Name")]
        [MaxLength(16)]
        private string name = default!;
    }

    // NetTask<NetResult> CreateCredit();
}

[NetServiceFilter(typeof(MergerOrTestFilter))]
[NetServiceObject]
public class MergerServiceImpl : MergerService
{// LPCallContext.Current
    public MergerServiceImpl(Merger merger)
    {
        this.merger = merger;
    }

    public async NetTask<MergerService.InformationResult?> GetInformation()
    {
        return this.merger.Information.ToInformationResult();
    }

    private Merger merger;
}
