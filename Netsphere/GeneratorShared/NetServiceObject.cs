// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class NetServiceObjectAttribute : Attribute
{
    /// <summary>
    /// Gets or sets an identifier of the net service [0 : auto-generated from the class name].
    /// </summary>
    public uint ServiceId { get; set; } = 0;

    public NetServiceObjectAttribute()
    {
    }
}
