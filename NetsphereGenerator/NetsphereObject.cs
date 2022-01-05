// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

#pragma warning disable SA1202 // Elements should be ordered by access
#pragma warning disable SA1204 // Static elements should appear before instance elements
#pragma warning disable SA1602 // Enumeration items should be documented

namespace Netsphere.Generator;

public enum DeclarationCondition
{
    NotDeclared, // Not declared
    ImplicitlyDeclared, // declared (implicitly)
    ExplicitlyDeclared, // declared (explicitly interface)
}

[Flags]
public enum NetsphereObjectFlag
{
    Configured = 1 << 0,
    RelationConfigured = 1 << 1,
    Checked = 1 << 2,

    NetServiceInterface = 1 << 10, // NetServiceInterface
    NetServiceObject = 1 << 11, // NetServiceObject
    HasDefaultConstructor = 1 << 12, // Has default constructor
}

public class NetsphereObject : VisceralObjectBase<NetsphereObject>
{
    public NetsphereObject()
    {
    }

    public new NetsphereBody Body => (NetsphereBody)((VisceralObjectBase<NetsphereObject>)this).Body;

    public NetsphereObjectFlag ObjectFlag { get; private set; }

    public NetServiceObjectAttributeMock? NetServiceObjectAttribute { get; private set; }

    public NetServiceInterfaceAttributeMock? NetServiceInterfaceAttribute { get; private set; }

    public int LoaderNumber { get; private set; } = -1;

    public List<NetsphereObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<NetsphereObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public int GenericsNumber { get; private set; }

    public string GenericsNumberString => this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

    public NetsphereObject? Implementation { get; private set; } // For NetServiceInterface; NetsphereObject that implements this net service interface.

    public List<NetsphereObject>? ServiceInterfaces { get; private set; } // For NetServiceObjectAttribute; Net service interfaces implemented by this net service object.

    public Dictionary<uint, ServiceMethod>? ServiceMethods { get; private set; } // For NetServiceInterface; Methods included in this net service interface.

    public Arc.Visceral.NullableAnnotation NullableAnnotationIfReferenceType
    {
        get
        {
            if (this.TypeObject?.Kind.IsReferenceType() == true)
            {
                if (this.symbol is IFieldSymbol fs)
                {
                    return (Arc.Visceral.NullableAnnotation)fs.NullableAnnotation;
                }
                else if (this.symbol is IPropertySymbol ps)
                {
                    return (Arc.Visceral.NullableAnnotation)ps.NullableAnnotation;
                }
            }

            return Arc.Visceral.NullableAnnotation.None;
        }
    }

    public string QuestionMarkIfReferenceType
    {
        get
        {
            if (this.Kind.IsReferenceType())
            {
                return "?";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public void Configure()
    {
        if (this.ObjectFlag.HasFlag(NetsphereObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= NetsphereObjectFlag.Configured;

        if (this.AllAttributes.FirstOrDefault(x => x.FullName == NetServiceObjectAttributeMock.FullName) is { } objectAttribute)
        {// NetServiceObjectAttribute
            try
            {
                this.NetServiceObjectAttribute = NetServiceObjectAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
                this.NetServiceObjectAttribute.Location = objectAttribute.Location;
                this.ObjectFlag |= NetsphereObjectFlag.NetServiceObject;
            }
            catch (InvalidCastException)
            {
                this.Body.AddDiagnostic(NetsphereBody.Error_AttributePropertyError, objectAttribute.Location);
            }
        }
        else if (TryGetNetServiceInterfaceAttribute(this))
        {// NetServiceInterfaceAttribute
        }
        else
        {
            return;
        }

        // Generic type is not supported.
        if (this.Generics_Kind != VisceralGenericsKind.NotGeneric)
        {
            this.Body.AddDiagnostic(NetsphereBody.Error_GenericType, this.Location);
            return;
        }

        // Must be derived from INetService
        if (!this.AllInterfaces.Any(x => x == INetService.FullName))
        {
            this.Body.AddDiagnostic(NetsphereBody.Error_INetService, this.Location);
            return;
        }

        // Used keywords
        this.Identifier = new VisceralIdentifier("__gen_ns_identifier__");
        foreach (var x in this.AllMembers.Where(a => a.ContainingObject == this))
        {
            this.Identifier.Add(x.SimpleName);
        }

        if (this.NetServiceInterfaceAttribute != null)
        {// NetServiceInterface
            if (this.Body.IdToNetInterface.TryGetValue(this.NetServiceInterfaceAttribute.ServiceId, out var obj))
            {
                this.Body.AddDiagnostic(NetsphereBody.Error_DuplicateServiceId, this.NetServiceInterfaceAttribute.Location, this.NetServiceInterfaceAttribute.ServiceId);
                this.Body.AddDiagnostic(NetsphereBody.Error_DuplicateServiceId, obj.NetServiceInterfaceAttribute!.Location, obj.NetServiceInterfaceAttribute!.ServiceId);
            }
            else
            {
                this.Body.IdToNetInterface.Add(this.NetServiceInterfaceAttribute.ServiceId, this);
            }
        }
        else if (this.NetServiceObjectAttribute != null)
        {// NetServiceObject
            var accessibility = this.AccessibilityName;
            if (accessibility != "public" && accessibility != "internal")
            {
                this.Body.AddDiagnostic(NetsphereBody.Error_Accessibility, this.Location);
                return;
            }

            this.ServiceInterfaces = new();
            foreach (var x in this.AllInterfaceObjects)
            {
                if (x.AllInterfaces.Any(x => x == INetService.FullName))
                {
                    if (x.NetServiceInterfaceAttribute == null)
                    {
                        if (!TryGetNetServiceInterfaceAttribute(x))
                        {
                            continue;
                        }
                    }

                    this.ServiceInterfaces.Add(x);
                }
            }

            if (this.ServiceInterfaces.Count == 0)
            {
                return;
            }

            this.Body.NetObjects.Add(this);
        }

        static bool TryGetNetServiceInterfaceAttribute(NetsphereObject obj)
        {
            if (obj.AllAttributes.FirstOrDefault(x => x.FullName == NetServiceInterfaceAttributeMock.FullName) is { } interfaceAttribute)
            {// NetServiceInterfaceAttribute
                try
                {
                    obj.NetServiceInterfaceAttribute = NetServiceInterfaceAttributeMock.FromArray(interfaceAttribute.ConstructorArguments, interfaceAttribute.NamedArguments);
                    obj.NetServiceInterfaceAttribute.Location = interfaceAttribute.Location;
                    obj.ObjectFlag |= NetsphereObjectFlag.NetServiceInterface;

                    // Service ID
                    if (obj.NetServiceInterfaceAttribute.ServiceId == 0)
                    {
                        obj.NetServiceInterfaceAttribute.ServiceId = (uint)Arc.Crypto.FarmHash.Hash64(obj.FullName);
                    }

                    return true;
                }
                catch (InvalidCastException)
                {
                    obj.Body.AddDiagnostic(NetsphereBody.Error_AttributePropertyError, interfaceAttribute.Location);
                    return false;
                }
            }

            return false;
        }
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlag.HasFlag(NetsphereObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= NetsphereObjectFlag.RelationConfigured;

        if (!this.Kind.IsType())
        {// Not type
            return;
        }

        var cf = this.OriginalDefinition;
        if (cf == null)
        {
            return;
        }
        else if (cf != this)
        {
            cf.ConfigureRelation();
        }

        if (cf.ContainingObject == null)
        {// Root object
            List<NetsphereObject>? list;
            if (!this.Body.Namespaces.TryGetValue(this.Namespace, out list))
            {// Create a new namespace.
                list = new();
                this.Body.Namespaces[this.Namespace] = list;
            }

            if (!list.Contains(cf))
            {
                list.Add(cf);
            }
        }
        else
        {// Child object
            var parent = cf.ContainingObject;
            parent.ConfigureRelation();
            if (parent.Children == null)
            {
                parent.Children = new();
            }

            if (!parent.Children.Contains(cf))
            {
                parent.Children.Add(cf);
            }
        }

        if (cf.ConstructedObjects == null)
        {
            cf.ConstructedObjects = new();
        }

        if (!cf.ConstructedObjects.Contains(this))
        {
            cf.ConstructedObjects.Add(this);
            this.GenericsNumber = cf.ConstructedObjects.Count;
        }
    }

    public bool CheckKeyword(string keyword, Location? location = null)
    {
        if (!this.Identifier.Add(keyword))
        {
            this.Body.AddDiagnostic(NetsphereBody.Error_KeywordUsed, location ?? Location.None, this.SimpleName, keyword);
            return false;
        }

        return true;
    }

    public void Check()
    {
        if (this.ObjectFlag.HasFlag(NetsphereObjectFlag.Checked))
        {
            return;
        }

        this.ObjectFlag |= NetsphereObjectFlag.Checked;

        if (this.NetServiceObjectAttribute != null)
        {// NetServiceObject
            if (this.ServiceInterfaces != null)
            {
                foreach (var x in this.ServiceInterfaces)
                {
                    if (x.NetServiceInterfaceAttribute != null)
                    {
                        if (this.Body.IdToNetObject.TryGetValue(x.NetServiceInterfaceAttribute.ServiceId, out var obj))
                        {
                            var serviceInterface = x.ToString();
                            this.Body.AddDiagnostic(NetsphereBody.Error_DuplicateServiceObject, obj.Location, serviceInterface);
                            this.Body.AddDiagnostic(NetsphereBody.Error_DuplicateServiceObject, this.Location, serviceInterface);
                        }
                        else
                        {
                            this.Body.IdToNetObject.Add(x.NetServiceInterfaceAttribute.ServiceId, this);
                        }
                    }
                }
            }

            foreach (var x in this.GetMembers(VisceralTarget.Method))
            {
                if (x.Method_IsConstructor && x.ContainingObject == this)
                {// Constructor
                    if (x.Method_Parameters.Length == 0)
                    {
                        this.ObjectFlag |= NetsphereObjectFlag.HasDefaultConstructor;
                        break;
                    }
                }
            }
        }
        else if (this.NetServiceInterfaceAttribute != null)
        {// NetServiceInterface
            foreach (var x in this.GetMembers(VisceralTarget.Method))
            {
                var serviceMethod = ServiceMethod.Create(this, x);
                if (serviceMethod != null)
                {// Add
                    if (this.ServiceMethods == null)
                    {
                        this.ServiceMethods = new();
                    }

                    if (this.ServiceMethods.TryGetValue(serviceMethod.MethodId, out var s))
                    {// Duplicated
                    }
                    else
                    {
                        this.ServiceMethods.Add(serviceMethod.MethodId, serviceMethod);
                    }
                }
            }
        }
    }

    public static void GenerateLoader(ScopingStringBuilder ssb, GeneratorInformation info, NetsphereObject? parent, List<NetsphereObject> list)
    {
        if (parent?.Generics_Kind == VisceralGenericsKind.OpenGeneric)
        {
            return;
        }

        var classFormat = "__gen__bm__{0:D4}";
        var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.NetServiceObjectAttribute != null).ToArray();

        string? loaderIdentifier = null;
        var list3 = list2.ToArray();
        if (list3.Length > 0)
        {
            ssb.AppendLine();
            if (parent == null)
            {
                loaderIdentifier = string.Format(classFormat, 0);
            }
            else
            {
                parent.LoaderNumber = info.FormatterCount++;
                loaderIdentifier = string.Format(classFormat, parent.LoaderNumber);
            }

            ssb.AppendLine($"public class {loaderIdentifier}<TIdentifier> : IMachineLoader<TIdentifier>");
            using (var scope = ssb.ScopeBrace($"    where TIdentifier : notnull"))
            {
                using (var scope2 = ssb.ScopeBrace("public void Load()"))
                {
                    foreach (var x in list3)
                    {
                        ssb.AppendLine($"{x.FullName}.RegisterBM({x.NetServiceInterfaceAttribute!.ServiceId});");
                    }
                }
            }
        }

        using (var m = ssb.ScopeBrace("internal static void RegisterBM()"))
        {
            /*foreach (var x in list2)
            {
                if (x.ObjectAttribute == null)
                {
                    continue;
                }

                if (x.Generics_Kind != VisceralGenericsKind.OpenGeneric)
                {// Register fixed types.
                    ssb.AppendLine($"{x.FullName}.RegisterBM({x.ObjectAttribute.MachineTypeId});");
                }
            }

            foreach (var x in list.Where(a => a.ObjectFlag.HasFlag(NetsphereObjectFlag.HasRegisterBM)))
            {// Children
                ssb.AppendLine($"{x.FullName}.RegisterBM();");
            }*/

            if (loaderIdentifier != null)
            {// Loader
                ssb.AppendLine($"MachineLoader.Add(typeof({loaderIdentifier}<>));");
            }
        }
    }

    internal void Generate(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.ConstructedObjects == null)
        {
            return;
        }

        /*else if (this.IsAbstractOrInterface)
        {
            return;
        }*/

        using (var cls = ssb.ScopeBrace($"{this.AccessibilityName} partial {this.KindName} {this.LocalName}"))
        {
            if (this.NetServiceObjectAttribute != null)
            {
                this.Generate2(ssb, info);
            }

            if (this.Children?.Count > 0)
            {// Generate children and loader.
                ssb.AppendLine();
                foreach (var x in this.Children)
                {
                    x.Generate(ssb, info);
                }

                ssb.AppendLine();
                GenerateLoader(ssb, info, this, this.Children);
            }
        }
    }

    internal void Generate2(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        return;
    }
}
