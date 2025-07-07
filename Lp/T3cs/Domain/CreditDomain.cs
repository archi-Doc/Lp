// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Lp.Net;
using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class CreditDomain : IStringConvertible<CreditDomain>, IDomainService
{
    #region FieldAndProperty

    [Key(0)]
    public Credit Credit { get; init; } = Credit.UnsafeConstructor();

    [Key(1)]
    public NetNode NetNode { get; init; } = new();

    [Key(2)]
    [MaxLength(LpConstants.MaxUrlLength)]
    public partial string Url { get; init; } = string.Empty;

    public static int MaxStringLength => Credit.MaxStringLength + 1 + NetNode.MaxStringLength + 1 + LpConstants.MaxUrlLength;

    private SeedKey? seedKey;
    private DomainData? domainData;

    #endregion

    public CreditDomain(Credit credit, NetNode netNode, string url)
    {
        this.Credit = credit;
        this.NetNode = netNode;
        this.Url = url;
    }

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out CreditDomain @object, out int read, IConversionOptions? conversionOptions = null)
    {
        @object = default;
        read = 0;
        var s = source.Split(LpConstants.SeparatorSymbol);
        if (!s.MoveNext())
        {
            return false;
        }

        if (!Credit.TryParse(source[s.Current], out var credit, out _, conversionOptions))
        {
            return false;
        }

        if (!s.MoveNext())
        {
            return false;
        }

        if (!NetNode.TryParse(source[s.Current], out var netNode, out _, conversionOptions))
        {
            return false;
        }

        string url = string.Empty;
        if (s.MoveNext())
        {
            url = source[s.Current].ToString();
        }

        @object = new(credit, netNode, url);
        read = s.Current.End.Value;
        return true;
    }

    public int GetStringLength()
    {
        // return -1;
        var urlLength = string.IsNullOrEmpty(this.Url) ? 0 : 1 + this.Url.Length;
        return this.Credit.GetStringLength() + 1 + this.NetNode.GetStringLength() + urlLength;
    }

    public bool TryFormat(Span<char> destination, out int written, IConversionOptions? conversionOptions = null)
    {
        if (!this.Credit.TryFormat(destination, out written, conversionOptions))
        {
            return false;
        }

        destination = destination.Slice(written);
        if (!BaseHelper.TryAppend(ref destination, ref written, LpConstants.SeparatorSymbol))
        {
            return false;
        }

        if (!this.NetNode.TryFormat(destination, out var w, conversionOptions))
        {
            return false;
        }

        written += w;
        destination = destination.Slice(w);

        if (string.IsNullOrEmpty(this.Url))
        {
            return true;
        }
        else
        {
            if (!BaseHelper.TryAppend(ref destination, ref written, LpConstants.SeparatorSymbol))
            {
                return false;
            }

            if (!BaseHelper.TryAppend(ref destination, ref written, this.Url.AsSpan()))
            {
                return false;
            }

            return true;
        }
    }

    public bool Initialize(SeedKey seedKey, DomainData domainData)
    {
        if (!this.Credit.PrimaryMerger.Equals(seedKey.GetSignaturePublicKey()))
        {
            return false;
        }

        this.seedKey = seedKey;
        this.domainData = domainData;
        return true;
    }

    async NetTask<NetResult> INetServiceWithOwner.Authenticate(OwnerToken token)
    {
        return NetResult.Success;
    }

    async NetTask<NetResult> IDomainService.RegisterNode(NodeProof nodeProof)
    {
        if (this.domainData is null)
        {
            return NetResult.NoNetService;
        }

        if (!nodeProof.ValidateAndVerify())
        {
            return NetResult.InvalidData;
        }

        using (this.domainData.Nodes.LockObject.EnterScope())
        {
            if (this.domainData.Nodes.PublicKeyChain.TryGetValue(nodeProof.PublicKey, out var proof))
            {// Found
                if (proof.SignedMics >= nodeProof.SignedMics)
                {// Existing proof is newer or equal
                    return NetResult.Success;
                }
            }

            this.domainData.Nodes.Add(nodeProof);

            while (this.domainData.Nodes.Count > DomainData.MaxNodeCount)
            {// Remove the oldest NodeProofs if the count exceeds the maximum.
                if (this.domainData.Nodes.SignedMicsChain.First is { } node)
                {
                    node.Goshujin = default;
                }
            }
        }

        return NetResult.Success;
    }

    async NetTask<NetResultValue<NetNode>> IDomainService.GetNode(SignaturePublicKey publicKey)
    {
        if (this.domainData is null)
        {
            return new(NetResult.NoNetService);
        }

        using (this.domainData.Nodes.LockObject.EnterScope())
        {
            if (this.domainData.Nodes.PublicKeyChain.TryGetValue(publicKey, out var nodeProof))
            {
                return new(NetResult.Success, nodeProof.NetNode);
            }
            else
            {
                return new(NetResult.NotFound);
            }
        }
    }
}
