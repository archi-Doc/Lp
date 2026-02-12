// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Lp.T3cs;
using Netsphere.Crypto;

namespace Lp;

public class Example
{
    public const string Name = "ExName";
    public const string Code = "ExCode";
    public const string IpAddressString = "198.51.100.42";
    public const string NetNodeSeed = "!!!UGqDfUxXyDmKGnCknfgiLZoRX2mJ1yPAWPQ1Uvz9MOMCCJWf!!!(e!eh9RSW5Xgbe4vDgdMh4IH8VGMB2jfGR6_o2Utz-UU8Oq4-Mw)";
    public const string OriginatorSeed = "!!!g0ln5DnNBBCVKtCKpvBCOchl5os70-_2CAnwRyl_iepMJv3n!!!(s!4_Kqg8G9rRqNqFTwRVtc-w1VCQUJK6s-tauAAzZpTFTaTn-E)";
    public const string MergerSeed = "!!!CWcnjiqmfJ91guWz37cdfSUq8ZJQjh3TPdgZa4aEnFno4_qc!!!(s!GnE_O27bHiCz3LjFQU8tOf2-evrPiLi8PPs7_1FR6Fyy8z0Q)";

    public static readonly IPAddress IpAddress;
    public static readonly NetNode NetNode;
    public static readonly SeedKey NetNodeSeedKey;
    public static readonly EncryptionPublicKey NetNodePublicKey;

    public static readonly SeedKey OriginatorSeedKey;
    public static readonly SeedKey MergerSeedKey;
    public static readonly SignaturePublicKey OriginatorPublicKey;
    public static readonly SignaturePublicKey MergerPublicKey;
    public static readonly CreditIdentity CreditIdentity;
    public static readonly Credit Credit;
    public static readonly DomainAssignment DomainAssignment;

    static Example()
    {
        SeedKey.TryParse(NetNodeSeed, out NetNodeSeedKey!);
        NetNodePublicKey = NetNodeSeedKey.GetEncryptionPublicKey();
        IpAddress = IPAddress.Parse(IpAddressString);
        NetNode = new(new(IpAddress, NetConstants.EphemeralPort + 1), NetNodePublicKey);

        SeedKey.TryParse(OriginatorSeed, out OriginatorSeedKey!);
        OriginatorPublicKey = OriginatorSeedKey.GetSignaturePublicKey();
        SeedKey.TryParse(MergerSeed, out MergerSeedKey!);
        MergerPublicKey = MergerSeedKey.GetSignaturePublicKey();

        CreditIdentity = new(default, OriginatorPublicKey, [MergerPublicKey,]);
        Credit = new Credit(CreditIdentity.GetIdentifier(), [MergerPublicKey,]);
        DomainAssignment = new(Name, Code, CreditIdentity, NetNode);
    }
}
