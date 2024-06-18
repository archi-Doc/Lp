// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.Data;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
public class ShortNameAttribute : Attribute
{
    public string Name { get; set; }

    public ShortNameAttribute(string name)
    {
        this.Name = name;
    }
}
