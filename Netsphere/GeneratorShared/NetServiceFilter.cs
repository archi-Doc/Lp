// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class NetServiceFilterAttribute : Attribute
{
    public Type FilterType { get; set; }

    public int Order { get; set; } = int.MaxValue;

    public NetServiceFilterAttribute(Type filterType)
    {
        this.FilterType = filterType;
    }
}
