// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;
using Tinyhand;

namespace Sandbox;

[NetServiceInterface]
public interface TestService : INetService
{
    public NetTask<byte[]?> Pingpong(byte[] data);
}

[NetServiceObject]
public class TestServiceImpl : TestService
{
    public async NetTask<byte[]?> Pingpong(byte[] data)
        => data;
}
