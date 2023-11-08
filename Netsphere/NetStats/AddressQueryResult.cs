// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.NetStats;

public record struct AddressQueryResult(bool Ipv6, string? Uri, IPAddress? Address);
