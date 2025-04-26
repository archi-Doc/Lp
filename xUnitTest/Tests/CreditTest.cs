// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class CreditTest
{
    [Fact]
    public void Test1()
    {
        var seedKey = SeedKey.NewSignature();
        var publicKey = seedKey.GetSignaturePublicKey();
        var creditIdentity = new CreditIdentity(CreditKind.Full, publicKey, [publicKey]);
        Credit.TryCreate(creditIdentity, out var credit).IsTrue();

        var st = credit!.ToString();
        Credit.TryParse(st, out var credit2, out var read).IsTrue();
        credit.Equals(credit2).IsTrue();
        read.Is(st.Length);

        Value.TryCreate(publicKey, 111, credit, out var value).IsTrue();
        st = value!.ToString();
        Value.TryParse(st, out var value2, out read).IsTrue();
        value.Equals(value2).IsTrue();
        read.Is(st.Length);
    }
}
