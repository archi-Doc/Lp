// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;
using Netsphere.Stats;

namespace Lp.T3cs;

/// <summary>
/// Represents a proof object (authentication between merger and public key).<br/>
/// </summary>
[TinyhandUnion(0, typeof(MergerState))]
[TinyhandUnion(1, typeof(LinkerState))]
[TinyhandObject(ReservedKeyCount = CredentialState.ReservedKeyCount)]
public abstract partial class CredentialState
{
    /// <summary>
    /// The number of reserved keys.
    /// </summary>
    public const int ReservedKeyCount = 2;

    /// <summary>
    /// Initializes a new instance of the <see cref="CredentialState"/> class.
    /// </summary>
    public CredentialState()
    {
    }

    #region FieldAndProperty

    [Key(0)]
    public NetNode? NetNode { get; set; }

    [Key(1)]
    public string Name { get; set; } = string.Empty;

    [IgnoreMember]
    public bool IsActive { get; set; }

    public bool IsValid =>
        this.NetNode is not null &&
        this.NetNode.Address.IsValidIpv4AndIpv6 &&
        Alias.IsValid(this.Name);

    #endregion

    public override string ToString()
        => $"CredentialState: {this.Name} {this.NetNode}";
}
