// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Microsoft.CodeAnalysis;

namespace Netsphere.Generator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public class NetServiceFilterAttributeMock : Attribute
{
    public static readonly string SimpleName = "NetServiceFilter";
    public static readonly string StandardName = SimpleName + "Attribute";
    public static readonly string FullName = "Netsphere." + StandardName;

    public NetServiceFilterAttributeMock(Location location)
    {
        this.Location = location;
    }

    public ISymbol? FilterType { get; private set; }

    public int Order { get; set; } = int.MaxValue;

    public Location Location { get; set; } = Location.None;

    public static NetServiceFilterAttributeMock FromArray(object?[] constructorArguments, KeyValuePair<string, object?>[] namedArguments, Location location)
    {
        var attribute = new NetServiceFilterAttributeMock(location);
        object? val;
        if (constructorArguments.Length > 0)
        {
            val = constructorArguments[0];
            if (val is ISymbol subType)
            {
                attribute.FilterType = subType;
            }
        }

        val = AttributeHelper.GetValue(-1, nameof(Order), constructorArguments, namedArguments);
        if (val != null)
        {
            attribute.Order = (int)val;
        }

        return attribute;
    }
}
