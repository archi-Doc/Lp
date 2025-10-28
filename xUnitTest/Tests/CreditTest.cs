// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc;
using Lp.T3cs;
using Netsphere.Crypto;
using Xunit;

namespace xUnitTest;

public class CreditTest
{
    [Fact]
    public void TestCodeAndCredit()
    {
        var seedKey = SeedKey.NewSignature();
        var publicKey = seedKey.GetSignaturePublicKey();
        var creditIdentity = new CreditIdentity(default, publicKey, [publicKey]);
        Credit.TryCreate(creditIdentity, out var credit).IsTrue();

        var code = "Code";
        var st = $"{code}{credit!.ToString()}";
        var codeAndCredit = new CodeAndCredit(code, credit!);
        codeAndCredit.Validate().IsTrue();
        codeAndCredit.Code.Is(code);
        codeAndCredit.Credit.Is(credit);
        codeAndCredit.ConvertToString().Is(st);
        CodeAndCredit.TryParse(st, out var codeAndCredit2, out var read).IsTrue();
        codeAndCredit2!.Equals(codeAndCredit).IsTrue();

        code = seedKey.UnsafeToString();
        st = $"{code}{credit!.ToString()}";
        codeAndCredit = new CodeAndCredit(code, credit!);
        codeAndCredit.Validate().IsTrue();
        codeAndCredit.Code.Is(code);
        codeAndCredit.Credit.Is(credit);
        codeAndCredit.ConvertToString().Is(st);
        CodeAndCredit.TryParse(st, out codeAndCredit2, out read).IsTrue();
        codeAndCredit2!.Equals(codeAndCredit).IsTrue();
    }

    [Fact]
    public void Test1()
    {
        var seedKey = SeedKey.NewSignature();
        var publicKey = seedKey.GetSignaturePublicKey();
        var creditIdentity = new CreditIdentity(default, publicKey, [publicKey]);
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
