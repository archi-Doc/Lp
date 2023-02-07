// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere;

namespace LP.NetServices;

public class LPServerContext : ServerContext
{
}

public class LPCallContext : CallContext<LPServerContext>
{
    public static new LPCallContext Current => (LPCallContext)CallContext.Current;

    public LPCallContext()
    {
    }
}
