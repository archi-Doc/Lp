// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using LP.NetServices;
using Netsphere;

namespace LP.T3CS;

[NetServiceInterface]
public interface IMergerService : INetService
{
}

[NetServiceFilter(typeof(MergerOrTestFilter))]
[NetServiceObject]
public class MergerServiceImpl : IMergerService
{
}
