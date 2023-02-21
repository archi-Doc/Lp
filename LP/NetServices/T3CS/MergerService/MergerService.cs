// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
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

    NetTask<MergerResult> CreateCredit(MergerService.CreateCreditParams param);

    [TinyhandObject]
    public partial record CreateCreditParams([property: Key(0)] PublicKey publicKey);

    // NetTask<NetResult> CreateCredit();
}

[NetServiceFilter(typeof(MergerOrTestFilter))]
[NetServiceObject]
public class MergerServiceImpl : MergerService
{// LPCallContext.Current
    public MergerServiceImpl(Merger.Provider mergerProvider)
    {
        this.merger = mergerProvider.GetOrException();
    }

    public async NetTask<MergerService.InformationResult?> GetInformation()
    {
        return this.merger.Information.ToInformationResult();
    }

    public async NetTask<MergerResult> CreateCredit(MergerService.CreateCreditParams param)
    {
        return this.merger.CreateCredit(param);
    }

    private Merger merger;
}
