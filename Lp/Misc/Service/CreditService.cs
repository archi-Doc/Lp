// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;
using Lp.T3cs;

namespace Lp.Services;

public class CreditService
{
    private readonly CreditPoint.GoshujinClass creditPoints;

    public CreditService(CreditPoint.GoshujinClass creditPoints)
    {
        this.creditPoints = creditPoints;
    }

    public async ValueTask<DataScope<EquityCredit>> CreateEquityCredit(CreditIdentity creditIdentity)
    {
        var credit = creditIdentity.ToCredit();
        if (credit is null)
        {
            return new DataScope<EquityCredit>(DataScopeResult.Obsolete);
        }

        var dataScope = await this.creditPoints.TryLock(credit, AcquisitionMode.GetOrCreate, LpParameters.LockTimeout, default, x =>
        {
            var obj = new EquityCredit();
            obj.Initialize(credit, creditIdentity);
            return obj;
        }).ConfigureAwait(false);

        return Unsafe.As<DataScope<CreditBase>, DataScope<EquityCredit>>(ref dataScope);
    }
}
