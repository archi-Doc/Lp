// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.T3CS;
using Netsphere;

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

        [Key(0, AddProperty = "Name")]
        [MaxLength(16)]
        private string name = default!;
    }

    NetTask<MergerResult> CreateCredit(Merger.CreateCreditParams param);
}

[NetServiceFilter(typeof(MergerOrTestFilter))]
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

    public async NetTask<MergerResult> CreateCredit(Merger.CreateCreditParams param)
    {
        if (!this.AuthorizedKey.Validate())
        {
            return MergerResult.NotAuthorized;
        }

        return this.merger.CreateCredit(LPCallContext.Current.ServerContext, param);
    }

    public new NetTask<NetResult> Authorize(Token token)
        => base.Authorize(token);

    private Merger merger;
}
