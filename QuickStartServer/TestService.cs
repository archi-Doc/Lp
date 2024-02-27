// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace QuickStart;

[NetServiceInterface]
public interface ITestService : INetService
{
    NetTask<string?> DoubleString(string input);
}

[NetServiceObject]
internal class TestServiceImpl : ITestService
{
    async NetTask<string?> ITestService.DoubleString(string input)
        => input + input;
}
