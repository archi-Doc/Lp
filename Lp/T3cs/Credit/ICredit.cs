// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

#pragma warning disable SA1401

public interface ICredit
{
    Credit Credit { get; }

    CreditInformation CreditInformation { get; }
}
