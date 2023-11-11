// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Arc.Crypto;
using LP.T3CS;

namespace Netsphere;

/// <summary>
/// Represents ipv4/ipv6 node information.<br/>
/// <see cref="NetNode"/> = <see cref="NetAddress"/> + <see cref="NodePublicKey"/>.
/// </summary>
[TinyhandObject]
public sealed partial class NetNode : IStringConvertible<NetNode>, IValidatable
{
    public static readonly NetNode Default = new();
    private static NetNode? alternative;

    public static NetNode Alternative
    {
        get
        {
            if (alternative is null)
            {
                alternative = new NetNode(NetAddress.Alternative, NodePrivateKey.AlternativePrivateKey.ToPublicKey());
            }

            return alternative;
        }
    }

    public NetNode()
    {
    }

    public NetNode(NetAddress address, NodePublicKey publicKey)
    {
        this.Address = address;
        this.PublicKey = publicKey;
    }

    [Key(0)]
    public NetAddress Address { get; private set; }

    [Key(1)]
    public NodePublicKey PublicKey { get; private set; }

    public static bool TryParseNetNode(ILogger? logger, string source, [MaybeNullWhen(false)] out NetNode node)
    {
        node = default;
        if (string.Compare(source, "alternative", true) == 0)
        {
            node = NetNode.Alternative;
            return true;
        }
        else
        {
            if (!NetNode.TryParse(source, out var address))
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Could not parse: {source.ToString()}");
                return false;
            }

            if (!address.Address.Validate())
            {
                logger?.TryGet(LogLevel.Error)?.Log($"Invalid port: {source.ToString()}");
                return false;
            }

            node = address;
            return true;
        }
    }

    public static bool TryParse(ReadOnlySpan<char> source, [MaybeNullWhen(false)] out NetNode instance)
    {// Ip address (public key)
        source = source.Trim();

        var index = source.IndexOf('(');
        if (index < 0)
        {
            instance = default;
            return false;
        }

        var index2 = source.IndexOf(')');
        if (index2 < 0)
        {
            instance = default;
            return false;
        }

        var sourceAddress = source.Slice(0, index);
        var sourcePublicKey = source.Slice(index + 1, index2 - index - 1);

        if (!NetAddress.TryParse(sourceAddress, out var address))
        {
            instance = default;
            return false;
        }

        if (!NodePublicKey.TryParse(sourcePublicKey, out var publicKey))
        {
            instance = default;
            return false;
        }

        instance = new(address, publicKey);
        return true;
    }

    public static int MaxStringLength
        => NetAddress.MaxStringLength + SignaturePublicKey.MaxStringLength + 2;

    public int GetStringLength()
        => throw new NotImplementedException();

    public bool TryFormat(Span<char> destination, out int written)
    {
        var span = destination;
        written = 0;
        if (span.Length < MaxStringLength)
        {
            return false;
        }

        if (!this.Address.TryFormat(span, out written))
        {
            return false;
        }
        else
        {
            span = span.Slice(written);
        }

        span[0] = '(';
        span = span.Slice(1);

        if (!this.PublicKey.TryFormat(span, out written))
        {
            return false;
        }
        else
        {
            span = span.Slice(written);
        }

        span[0] = ')';
        span = span.Slice(1);

        written = destination.Length - span.Length;
        return true;
    }

    public bool Validate()
        => this.Address.Validate() && this.PublicKey.Validate();

    public override string ToString()
    {
        Span<char> span = stackalloc char[MaxStringLength];
        return this.TryFormat(span, out var written) ? span.Slice(0, written).ToString() : string.Empty;
    }
}
