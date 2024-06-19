// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;

namespace Lp.Data;

internal class VisceralOperator<T>
{
    internal VisceralOperator()
    {
        var type = typeof(T);
        var flags = BindingFlags.Public | BindingFlags.Instance;
        foreach (var x in type.GetProperties(flags))
        {
            var item = new Item(x, x.GetGetMethod(true), x.GetSetMethod(true), null);
            this.nameToItem.TryAdd(item.Name, item);
        }

        foreach (var x in type.GetFields(flags))
        {
            var item = new Item(x, null, null, x);
            this.nameToItem.TryAdd(item.Name, item);
        }
    }

    private class Item
    {
        public Item(MemberInfo memberInfo, MethodInfo? getMethod, MethodInfo? setMethod, FieldInfo? fieldInfo)
        {
            this.MemberInfo = memberInfo;
            if (this.MemberInfo.GetCustomAttribute<ShortNameAttribute>() is { } attribute)
            {
                this.Name = attribute.Name;
            }
            else
            {
                this.Name = this.MemberInfo.Name;
            }

            this.GetMethod = getMethod;
            this.SetMethod = setMethod;
        }

        public string Name { get; private set; }

        public MemberInfo MemberInfo { get; private set; }

        public MethodInfo? GetMethod { get; private set; }

        public MethodInfo? SetMethod { get; private set; }

        public FieldInfo? FieldInfo { get; private set; }
    }

    public string[] GetNames() => this.nameToItem.Keys.ToArray();

    public bool TrySet<TValue>(T instance, string name, object? value)
    {
        if (!this.nameToItem.TryGetValue(name, out var item))
        {
            return false;
        }

        try
        {
            if (item.FieldInfo is { } fieldInfo)
            {
                fieldInfo.SetValue(instance, value); // Slow...
                return true;
            }
            else if (item.SetMethod is { } setMethod)
            {
                setMethod.Invoke(instance, new object?[] { value, }); // Slow...
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public bool TryGet(T instance, string name, out object? value)
    {
        if (!this.nameToItem.TryGetValue(name, out var item))
        {
            value = null;
            return false;
        }

        try
        {
            if (item.FieldInfo is { } fieldInfo)
            {
                value = fieldInfo.GetValue(instance); // Slow...
                return true;
            }
            else if (item.GetMethod is { } getMethod)
            {
                value = getMethod.Invoke(instance, Array.Empty<object>()); // Slow...
                return true;
            }
        }
        catch
        {
        }

        value = null;
        return false;
    }

    private Dictionary<string, Item> nameToItem = new();
}
