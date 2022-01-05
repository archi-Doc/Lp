// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class NetServiceInterfaceAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an identifier of the net service [0: auto-generated from the interface name].
    /// </summary>
    public uint ServiceId { get; set; } = 0;

    public NetServiceInterfaceAttribute()
    {
    }
}
