// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Generator;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class NetServiceInterfaceAttributeMock : Attribute
{
    public static readonly string SimpleName = "NetServiceInterface";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "Netsphere." + StandardName;

    public NetServiceInterfaceAttributeMock()
    {
    }

    /// <summary>
    /// Gets or sets an identifier of the net service [0 : auto-generated from the interface name].
    /// </summary>
    public uint ServiceId { get; set; } = 0;

    public static NetServiceInterfaceAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new NetServiceInterfaceAttributeMock();
        object? val;

        val = AttributeHelper.GetValue(-1, nameof(ServiceId), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ServiceId = (uint)val;
        }

        return attribute;
    }
}
