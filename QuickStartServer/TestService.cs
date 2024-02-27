// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace QuickStart;

[NetServiceInterface]
public interface TestService : INetService
{
    NetTask<string?> DoubleString(string source);
}

internal class TestServiceImpl : TestService
{
    async NetTask<string?> TestService.DoubleString(string source)
        => source + source;
}
