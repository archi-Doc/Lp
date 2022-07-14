// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Reflection;

namespace LP.Data;

public static class LPFlagsHelper
{
    private class Item
    {
        public Item(MemberInfo memberInfo, MethodInfo? getMethod, MethodInfo? setMethod, FieldInfo? fieldInfo)
        {
            this.MemberInfo = memberInfo;
            this.GetMethod = getMethod;
            this.SetMethod = setMethod;
        }

        public MemberInfo MemberInfo { get; private set; }

        public MethodInfo? GetMethod { get; private set; }

        public MethodInfo? SetMethod { get; private set; }

        public FieldInfo? FieldInfo { get; private set; }
    }

    static LPFlagsHelper()
    {
        var type = typeof(LPFlags);
        foreach (var x in type.GetMembers(BindingFlags.Public | BindingFlags.Instance))
        {
            Item? item = null;
            if (x is PropertyInfo propertyInfo)
            {
                item = new(x, propertyInfo.GetGetMethod(), propertyInfo.GetSetMethod(), null);
            }
            else if (x is FieldInfo fieldInfo)
            {
                item = new Item(x, null, null, fieldInfo);
            }

            if (item != null)
            {
                var name = x.Name.ToLower();
                if (!nameToItem.ContainsKey(name))
                {
                    nameToItem[name] = item;
                }
            }
        }
    }

    public static string[] GetNames() => nameToItem.Keys.ToArray();

    public static bool TrySet(LPFlags flags, string name, bool flag)
    {
        if (!nameToItem.TryGetValue(name, out var item))
        {
            return false;
        }

        if (item.FieldInfo is { } fieldInfo)
        {
            fieldInfo.SetValue(flags, flag);
            return true;
        }
        else if (item.SetMethod is { } setMethod)
        {
            setMethod.Invoke(flags, new object[] { flag, });
            return true;
        }
        else
        {
            return false;
        }
    }

    public static bool? TryGet(LPFlags flags, string name)
    {
        if (!nameToItem.TryGetValue(name, out var item))
        {
            return false;
        }

        if (item.FieldInfo is { } fieldInfo)
        {
            return (bool?)fieldInfo.GetValue(flags);
        }
        else if (item.GetMethod is { } getMethod)
        {
            return (bool?)getMethod.Invoke(flags, Array.Empty<object>());
        }
        else
        {
            return null;
        }
    }

    private static Dictionary<string, Item> nameToItem = new();
}
