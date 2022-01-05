// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Generator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
public sealed class NetServiceObjectAttributeMock : Attribute
{
    public static readonly string SimpleName = "NetServiceObject";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "Netsphere." + StandardName;

    public NetServiceObjectAttributeMock()
    {
    }

    public static NetServiceObjectAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments)
    {
        var attribute = new NetServiceObjectAttributeMock();

        /*object? val;
        val = AttributeHelper.GetValue(-1, nameof(ServiceId), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.ServiceId = (uint)val;
        }*/

        return attribute;
    }
}
