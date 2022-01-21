﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Netsphere.Generator;

public class ServiceFilterGroup
{
    public ServiceFilterGroup(NetsphereObject obj, ServiceFilter serviceFilter)
    {
        this.Object = obj;
        this.ServiceFilter = serviceFilter;
    }

    public class Item
    {
        public Item(NetsphereObject obj, NetsphereObject? callContextObject, string identifier, string? argument)
        {
            this.Object = obj;
            this.CallContextObject = callContextObject;
            this.Identifier = identifier;
            this.Arguments = argument;
        }

        public NetsphereObject Object { get; private set; }

        public NetsphereObject? CallContextObject { get; private set; }

        public string Identifier { get; private set; }

        public string? Arguments { get; private set; }
    }

    public NetsphereObject Object { get; }

    public ServiceFilter ServiceFilter { get; }

    public Item[]? Items { get; private set; }

    public Dictionary<NetServiceFilterAttributeMock, Item>? AttributeToItem { get; private set; }

    public void CheckAndPrepare()
    {
        var errorFlag = false;
        var filterList = this.ServiceFilter.FilterList;
        var items = new Item[filterList.Count];
        var dictionary = new Dictionary<NetServiceFilterAttributeMock, Item>();
        for (var i = 0; i < filterList.Count; i++)
        {
            var obj = this.Object.Body.Add(filterList[i].FilterType!);
            var filterObject = obj == null ? null : this.GetFilterObject(obj);
            if (obj == null || filterObject == null)
            {
                this.Object.Body.AddDiagnostic(NetsphereBody.Error_FilterTypeNotDerived, filterList[i].Location);
                errorFlag = true;
                continue;
            }

            NetsphereObject? callContextObject = null;
            if (filterObject.Generics_Arguments.Length > 0)
            {
                callContextObject = filterObject.Generics_Arguments[0];
            }

            string? argument = null;
            if (!string.IsNullOrEmpty(filterList[i].Arguments))
            {
                argument = filterList[i].Arguments;
            }

            var item = new Item(obj, callContextObject, this.Object.Identifier.GetIdentifier(), argument);
            items[i] = item;

            dictionary[filterList[i]] = item;
        }

        if (errorFlag)
        {
            return;
        }

        if (items.Length > 0)
        {
            this.Items = items;
            this.AttributeToItem = dictionary;
        }
    }

    public Item? GetIdentifier(NetServiceFilterAttributeMock? filterAttribute)
    {
        if (this.AttributeToItem == null || filterAttribute == null)
        {
            return null;
        }

        if (this.AttributeToItem.TryGetValue(filterAttribute, out var identifier))
        {
            return identifier;
        }

        return null;
    }

    public void GenerateDefinition(ScopingStringBuilder ssb)
    {
        if (this.Items == null)
        {
            return;
        }

        foreach (var x in this.Items)
        {
            ssb.AppendLine($"private {x.Object.FullName} {x.Identifier};");
        }
    }

    public void GenerateInitialize(ScopingStringBuilder ssb, string context)
    {
        if (this.Items == null)
        {
            return;
        }

        foreach (var x in this.Items)
        {
            var hasDefaultConstructor = false;
            foreach (var a in x.Object.GetMembers(VisceralTarget.Method))
            {
                if (a.Method_IsConstructor && a.ContainingObject == x.Object)
                {// Constructor
                    if (a.Method_Parameters.Length == 0)
                    {
                        hasDefaultConstructor = true;
                        break;
                    }
                }
            }

            string newInstance;
            if (hasDefaultConstructor)
            {
                newInstance = $"new {x.Object.FullName}()";
            }
            else
            {
                newInstance = $"{context}.ServiceProvider.GetService(typeof({x.Object.FullName}))!";
            }

            ssb.AppendLine($"this.{x.Identifier} = ({x.Object.FullName}){context}.ServiceFilters.GetOrAdd(typeof({x.Object.FullName}), x => (IServiceFilter){newInstance});");
            using (var scopeNull = ssb.ScopeBrace($"if (this.{x.Identifier} == null)"))
            {
                ssb.AppendLine($"throw new InvalidOperationException($\"Could not create an instance of the net filter '{x.Object.FullName}'.\");");
            }

            if (x.Arguments != null)
            {
                ssb.AppendLine($"(({NetsphereBody.ServiceFilterBaseName})this.{x.Identifier}).{NetsphereBody.ServiceFilterSetArgumentsName}({x.Arguments});");
            }
        }
    }

    private NetsphereObject? GetFilterObject(NetsphereObject obj)
    {
        foreach (var x in obj.AllInterfaceObjects)
        {
            if (x.Generics_IsGeneric)
            {// Generic
                if (x.OriginalDefinition?.FullName == NetsphereBody.ServiceFilterFullName2)
                {
                    return x;
                }
            }
            else
            {// Not generic
                if (x.FullName == NetsphereBody.ServiceFilterFullName)
                {
                    return x;
                }
            }
        }

        return null;
    }
}
