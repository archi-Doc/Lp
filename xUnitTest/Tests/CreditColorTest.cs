// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Lp.T3cs;
using Xunit;

namespace xUnitTest;

public class CreditColorTest
{
    [Fact]
    public void EqualColorsHaveEqualHashCodes()
    {// A record hashes its members, so the fees must hash their values like their Equals().
        var color1 = CreditColor.NewBoard() with { OwnerFee = new OwnerFee(), OrderFee = new OrderFee(), };
        var color2 = CreditColor.NewBoard() with { OwnerFee = new OwnerFee(), OrderFee = new OrderFee(), };

        Assert.Equal(color1, color2);
        Assert.Equal(color1.GetHashCode(), color2.GetHashCode());
        Assert.True(new OwnerFee().Equals((object)new OwnerFee()));
        Assert.True(new OrderFee().Equals((object)new OrderFee()));
    }
}
