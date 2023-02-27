// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

public class NetException : Exception
{
    public NetException(NetResult result)
        : base()
    {
        this.Result = result;
    }

    public NetException(string message)
        : base(message)
    {
    }

    public NetException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public NetResult Result { get; }
}
