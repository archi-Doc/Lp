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

    HasDefaultConstructor = 1 << 13, // Has default constructor
}

public class NetsphereObject : VisceralObjectBase<NetsphereObject>
{
    public NetsphereObject()
    {
    }

    public new NetsphereBody Body => (NetsphereBody)((VisceralObjectBase<NetsphereObject>)this).Body;

    public NetsphereObjectFlag ObjectFlag { get; private set; }

    public NetServiceObjectAttributeMock? ObjectAttribute { get; private set; }

    public int LoaderNumber { get; private set; } = -1;

    public List<NetsphereObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<NetsphereObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public int GenericsNumber { get; private set; }

    public string GenericsNumberString => this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

    public NetsphereObject? TargetInterface { get; private set; }

    public uint ServiceId { get; private set; }

    public Dictionary<uint, ServiceMethod>? ServiceMethods { get; private set; }

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

        // Generic type is not supported.
        if (this.Generics_Kind != VisceralGenericsKind.NotGeneric)
        {
            return;
        }

        // Check INetService
        if (!this.AllInterfaces.Any(x => x == INetService.FullName))
        {
            return;
        }

        // NetServiceObjectAttribute
        if (this.AllAttributes.FirstOrDefault(x => x.FullName == NetServiceObjectAttributeMock.FullName) is { } objectAttribute)
        {
            this.Location = objectAttribute.Location;
            try
            {
                this.ObjectAttribute = NetServiceObjectAttributeMock.FromArray(objectAttribute.ConstructorArguments, objectAttribute.NamedArguments);
            }
            catch (InvalidCastException)
            {
                this.Body.ReportDiagnostic(NetsphereBody.Error_AttributePropertyError, objectAttribute.Location);
            }
        }

        // Used keywords
        this.Identifier = new VisceralIdentifier("__gen_ns_identifier__");
        foreach (var x in this.AllMembers.Where(a => a.ContainingObject == this))
        {
            this.Identifier.Add(x.SimpleName);
        }

        // Service ID
        this.ServiceId = (uint)Arc.Crypto.FarmHash.Hash64(this.FullName);
        if (this.ObjectAttribute != null && this.ObjectAttribute.ServiceId != 0)
        {
            this.ServiceId = this.ObjectAttribute.ServiceId;
        }

        if (this.Kind == VisceralObjectKind.Class || this.Kind == VisceralObjectKind.Record || this.Kind == VisceralObjectKind.Struct)
        {
            this.TargetInterface = this.FindTargetInterface();
            if (this.TargetInterface != null)
            {
                if (this.Body.Implementations.ContainsKey(this.ServiceId))
                {
                    this.Body.ReportDiagnostic(NetsphereBody.Error_DuplicateTypeId, this.Location, this.ServiceId);
                }
                else
                {
                    this.Body.Implementations.Add(this.ServiceId, this);
                }
            }
        }
        else if (this.Kind == VisceralObjectKind.Interface)
        {
            if (this.Body.Interfaces.ContainsKey(this.ServiceId))
            {
                this.Body.ReportDiagnostic(NetsphereBody.Error_DuplicateTypeId, this.Location, this.ServiceId);
            }
            else
            {
                this.Body.Interfaces.Add(this.ServiceId, this);
            }
        }
    }

    private NetsphereObject? FindTargetInterface()
    {
        /*NetsphereObject current = this;

        while (current != null)
        {
            foreach (var x in current.Interfaces)
            {
                this.Body.Add(x)
            }
        }

        var baseObject = this.BaseObject;
        if (baseObject == null)
        {
            return null;
        }

        */

        return null;
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

        foreach (var x in this.GetMembers(VisceralTarget.Method))
        {
            if (x.Method_IsConstructor && x.ContainingObject == this)
            {// Constructor
                if (x.Method_Parameters.Length == 0)
                {
                    this.ObjectFlag |= NetsphereObjectFlag.HasDefaultConstructor;
                    continue;
                }
            }

            var stateMethod = ServiceMethod.Create(this, x);
            if (stateMethod != null)
            {// Add
                if (this.ServiceMethods == null)
                {
                    this.ServiceMethods = new();
                }

                if (this.ServiceMethods.TryGetValue(stateMethod.MethodId, out var s))
                {// Duplicated
                    stateMethod.DuplicateId = true;
                    s.DuplicateId = true;
                }
                else
                {
                    this.ServiceMethods.Add(stateMethod.MethodId, stateMethod);
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
        var list2 = list.SelectMany(x => x.ConstructedObjects).Where(x => x.ObjectAttribute != null).ToArray();

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
                        ssb.AppendLine($"{x.FullName}.RegisterBM({x.ServiceId});");
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
            if (this.ObjectAttribute != null)
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
