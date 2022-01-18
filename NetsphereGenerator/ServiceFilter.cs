// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable RS1024 // Compare symbols correctly

namespace Netsphere.Generator;

public class ServiceFilter
{
    public static ServiceFilter? CreateFromObject(NetsphereObject obj)
    {
        List<NetServiceFilterAttributeMock>? filterList = null;
        var errorFlag = false;
        foreach (var x in obj.AllAttributes.Where(a => a.FullName == NetServiceFilterAttributeMock.FullName))
        {
            NetServiceFilterAttributeMock attr;
            try
            {
                attr = NetServiceFilterAttributeMock.FromArray(x.ConstructorArguments, x.NamedArguments, x.Location);
            }
            catch (InvalidCastException)
            {
                obj.Body.AddDiagnostic(NetsphereBody.Error_AttributePropertyError, x.Location);
                errorFlag = true;
                continue;
            }

            if (attr.FilterType == null)
            {
                obj.Body.AddDiagnostic(NetsphereBody.Error_NoFilterType, x.Location);
                errorFlag = true;
                continue;
            }

            filterList ??= new();
            filterList.Add(attr);
        }

        if (errorFlag)
        {
            return null;
        }

        if (filterList == null)
        {// No filter attribute.
            return null;
        }

        // Check for duplicates.
        var checker2 = new HashSet<ISymbol?>();
        foreach (var item in filterList)
        {
            if (!checker2.Add(item.FilterType))
            {
                obj.Body.AddDiagnostic(NetsphereBody.Error_FilterTypeConflicted, item.Location);
                errorFlag = true;
            }
        }

        if (errorFlag)
        {
            return null;
        }

        return new ServiceFilter(obj, filterList);
    }

    public ServiceFilter(NetsphereObject obj, List<NetServiceFilterAttributeMock> filterList)
    {
        this.Object = obj;
        this.FilterList = filterList;
    }

    public ServiceFilter(ServiceFilter service, ServiceFilter method)
    {
        this.Object = method.Object;

        var list = new List<NetServiceFilterAttributeMock>(service.FilterList);
        foreach (var x in method.FilterList)
        {
            if (!list.Contains(x))
            {
                list.Add(x);
            }
        }

        this.FilterList = list;
    }

    public class Item
    {
        public Item(NetsphereObject obj, NetsphereObject filterObject, string identifier)
        {
            this.Object = obj;
            this.FilterObject = filterObject;
            this.Identifier = identifier;
        }

        public NetsphereObject Object { get; private set; }

        public NetsphereObject FilterObject { get; private set; }

        public string Identifier { get; private set; }
    }

    public NetsphereObject Object { get; }

    public List<NetServiceFilterAttributeMock> FilterList { get; private set; }

    public Item[]? Items { get; private set; }

    public void CheckAndPrepare()
    {
        var errorFlag = false;
        var items = new Item[this.FilterList.Count];
        for (var i = 0; i < this.FilterList.Count; i++)
        {
            var obj = this.Object.Body.Add(this.FilterList[i].FilterType!);
            var filterObject = obj == null ? null : this.GetFilterObject(obj);
            if (obj == null || filterObject == null)
            {
                this.Object.Body.AddDiagnostic(NetsphereBody.Error_FilterTypeNotDerived, this.FilterList[i].Location);
                errorFlag = true;
                continue;
            }

            var item = new Item(obj, filterObject, this.Object.Identifier.GetIdentifier());
            items[i] = item;
        }

        if (errorFlag)
        {
            return;
        }

        if (items.Length > 0)
        {
            this.Items = items;
        }
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
            string newInstance;
            if (x.Object.ObjectFlag.HasFlag(NetsphereObjectFlag.HasDefaultConstructor))
            {
                newInstance = $"new {x.Object.FullName}()";
            }
            else
            {
                newInstance = $"{context}.ServiceProvider.GetService(typeof({x.Object.FullName}))!";
            }

            ssb.AppendLine($"this.{x.Identifier} = ({x.Object.FullName}){context}.ServiceFilters.GetOrAdd(typeof({x.Object.FullName}), x => (IServiceFilter){newInstance});");
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
