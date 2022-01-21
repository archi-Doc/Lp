// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace xUnitTest.Netsphere;

[NetServiceInterface]
public interface IFilterTestService : INetService
{
    public NetTask<int> NoFilter(int x);
}

[NetServiceObject]
public class FilterTestServiceImpl : IFilterTestService
{
    public async NetTask<int> NoFilter(int x) => x;
}
