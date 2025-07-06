// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Crypto;

namespace Lp.T3cs;

[TinyhandObject]
public partial record class DomainOption(
        [property: Key(0)] Credit DomainCredit,
        [property: Key(1)] NetNode NetNode,
        [property: Key(2)] string? Url)
{
}
