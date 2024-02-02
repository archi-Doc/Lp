// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Netsphere.Crypto;

namespace LP.NetServices.T3CS;

[NetServiceInterface]
public partial interface IMergerService : IAuthorizedService
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

        [Key(0, AddProperty = "MergerName")]
        [MaxLength(16)]
        private string mergerName = default!;
    }

    NetTask<MergerResult> CreateCredit(Merger.CreateCreditParams param);
}

[NetServiceFilter<MergerOrTestFilter>]
[NetServiceObject]
public class MergerServiceImpl : AuthorizedService, IMergerService
{// LPCallContext.Current
    public MergerServiceImpl(Merger.Provider mergerProvider)
    {
        this.merger = mergerProvider.GetOrException();
    }

    public async NetTask<IMergerService.InformationResult?> GetInformation()
    {
        return this.merger.Information.ToInformationResult();
    }

    public NetTask<MergerResult> CreateCredit(Merger.CreateCreditParams param)
    {
        if (!this.Authenticated)
        {
            return new(MergerResult.NotAuthorized);
        }

        return this.merger.CreateCredit(param);
    }

    public new NetTask<NetResult> Authenticate(AuthenticationToken token)
        => base.Authenticate(token);

    private Merger merger;
}
