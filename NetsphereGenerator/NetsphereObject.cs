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
public enum BigMachinesObjectFlag
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

    public BigMachinesObjectFlag ObjectFlag { get; private set; }

    public NetServiceObjectAttributeMock? ObjectAttribute { get; private set; }

    public NetsphereObject? MachineObject { get; private set; }

    public NetsphereObject? IdentifierObject { get; private set; }

    public string StateName { get; private set; } = string.Empty;

    public string? LoaderIdentifier { get; private set; }

    public int LoaderNumber { get; private set; } = -1;

    public bool IsAbstractOrInterface => this.Kind == VisceralObjectKind.Interface || (this.symbol is INamedTypeSymbol nts && nts.IsAbstract);

    public List<NetsphereObject>? Children { get; private set; } // The opposite of ContainingObject

    public List<NetsphereObject>? ConstructedObjects { get; private set; } // The opposite of ConstructedFrom

    public VisceralIdentifier Identifier { get; private set; } = VisceralIdentifier.Default;

    public int GenericsNumber { get; private set; }

    public string GenericsNumberString => this.GenericsNumber > 1 ? this.GenericsNumber.ToString() : string.Empty;

    public NetsphereObject? ClosedGenericHint { get; private set; }

    public string? GroupType { get; private set; }

    public string NewIfDerived { get; private set; } = string.Empty;

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
        if (this.ObjectFlag.HasFlag(BigMachinesObjectFlag.Configured))
        {
            return;
        }

        this.ObjectFlag |= BigMachinesObjectFlag.Configured;

        // Generic type is not supported.
        if (this.Generics_Kind != VisceralGenericsKind.NotGeneric)
        {
            return;
        }

        // MachineObjectAttribute
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

        if (this.ObjectAttribute != null)
        {
            this.ConfigureObject();
        }
    }

    private void ConfigureObject()
    {
        // Used keywords
        this.Identifier = new VisceralIdentifier("__gen_bm_identifier__");
        foreach (var x in this.AllMembers.Where(a => a.ContainingObject == this))
        {
            this.Identifier.Add(x.SimpleName);
        }
    }

    public void ConfigureRelation()
    {// Create an object tree.
        if (this.ObjectFlag.HasFlag(BigMachinesObjectFlag.RelationConfigured))
        {
            return;
        }

        this.ObjectFlag |= BigMachinesObjectFlag.RelationConfigured;

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

    public void CheckObject()
    {
        // partial class required.
        /*if (!this.IsPartial)
        {
            this.Body.ReportDiagnostic(NetsphereBody.Error_NotPartial, this.Location, this.FullName);
        }

        // Parent class also needs to be a partial class.
        var parent = this.ContainingObject;
        while (parent != null)
        {
            if (!parent.IsPartial)
            {
                this.Body.ReportDiagnostic(NetsphereBody.Error_NotPartialParent, parent.Location, parent.FullName);
            }

            parent = parent.ContainingObject;
        }*/

        var id = this.ObjectAttribute!.ServiceId;
        if (this.Body.Machines.ContainsKey(id))
        {
            this.Body.ReportDiagnostic(NetsphereBody.Error_DuplicateTypeId, this.Location, id);
        }
        else
        {
            this.Body.Machines.Add(id, this);
        }

        // Machine<TIdentifier>
        var machineObject = this.BaseObject;
        var derivedMachine = false;
        while (machineObject != null)
        {
            if (machineObject.OriginalDefinition?.FullName == "BigMachines.Machine<TIdentifier>")
            {
                break;
            }
            else if (machineObject.ObjectAttribute != null)
            {
                derivedMachine = true;
            }

            machineObject = machineObject.BaseObject;
        }

        if (derivedMachine)
        {
            this.NewIfDerived = "new ";
        }

        if (machineObject == null)
        {
            this.Body.ReportDiagnostic(NetsphereBody.Error_NotDerived, this.Location);
            return;
        }
        else
        {
            if (machineObject.Generics_Arguments.Length == 1)
            {
                this.MachineObject = machineObject;
                this.IdentifierObject = machineObject.Generics_Arguments[0];
                this.StateName = this.FullName + ".State";
            }
        }

        if (this.IdentifierObject != null && this.IdentifierObject.Kind != VisceralObjectKind.TypeParameter)
        {
            if (this.IdentifierObject.Location.IsInSource)
            {
                if (!this.IdentifierObject.AllAttributes.Any(x => x.FullName == "Tinyhand.TinyhandObjectAttribute"))
                {
                    this.Body.AddDiagnostic(NetsphereBody.Error_IdentifierIsNotSerializable, this.IdentifierObject.Location, this.IdentifierObject.FullName);
                }
            }
        }

        var idToStateMethod = new Dictionary<uint, ServiceMethod>();
        foreach (var x in this.GetMembers(VisceralTarget.Method))
        {
            if (x.AllAttributes.FirstOrDefault(x => x.FullName == StateMethodAttributeMock.FullName) is { } attribute)
            {
                var stateMethod = ServiceMethod.Create(this, x, attribute);
                if (stateMethod != null)
                {// Add
                    this.StateMethodList.Add(stateMethod);

                    if (idToStateMethod.TryGetValue(stateMethod.Id, out var s))
                    {// Duplicated
                        stateMethod.DuplicateId = true;
                        s.DuplicateId = true;
                    }
                    else
                    {
                        idToStateMethod.Add(stateMethod.Id, stateMethod);
                    }
                }
            }
            else if (x.Method_IsConstructor && x.ContainingObject == this)
            {// Constructor
                if (x.Method_Parameters.Length == 0)
                {
                    this.ObjectFlag |= BigMachinesObjectFlag.HasDefaultConstructor;
                }
            }
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
        if (this.ObjectFlag.HasFlag(BigMachinesObjectFlag.Checked))
        {
            return;
        }

        if (this.Generics_Kind == VisceralGenericsKind.ClosedGeneric)
        {// Close generic is not necessary.
            return;
        }

        this.ObjectFlag |= BigMachinesObjectFlag.Checked;

        this.Body.DebugAssert(this.ObjectAttribute != null, "this.ObjectAttribute != null");
        this.CheckObject();
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
                        ssb.AppendLine($"{x.FullName}.RegisterBM({x.ObjectAttribute!.MachineTypeId});");
                    }
                }
            }
        }

        using (var m = ssb.ScopeBrace("internal static void RegisterBM()"))
        {
            foreach (var x in list2)
            {
                if (x.ObjectAttribute == null || x.IdentifierObject == null)
                {
                    continue;
                }

                if (x.Generics_Kind != VisceralGenericsKind.OpenGeneric)
                {// Register fixed types.
                    ssb.AppendLine($"{x.FullName}.RegisterBM({x.ObjectAttribute.MachineTypeId});");
                }
            }

            foreach (var x in list.Where(a => a.ObjectFlag.HasFlag(BigMachinesObjectFlag.HasRegisterBM)))
            {// Children
                ssb.AppendLine($"{x.FullName}.RegisterBM();");
            }

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
        this.Generate_State(ssb, info);
        this.Generate_Interface(ssb, info);
        this.Generate_CreateInterface(ssb, info);
        this.Generate_InternalRun(ssb, info);
        this.Generate_ChangeStateInternal(ssb, info);

        return;
    }

    internal void Generate_State(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.StateMethodList == null)
        {
            return;
        }

        using (var scope = ssb.ScopeBrace($"public {this.NewIfDerived}enum State"))
        {
            foreach (var x in this.StateMethodList)
            {
                ssb.AppendLine($"{x.Name} = {x.Id},");
            }
        }

        ssb.AppendLine();
    }

    internal void Generate_Interface(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null)
        {
            return;
        }

        var identifierName = this.IdentifierObject!.FullName;
        using (var scope = ssb.ScopeBrace($"public {this.NewIfDerived}class Interface : ManMachineInterface<{identifierName}, {this.StateName}>"))
        {
            using (var scope2 = ssb.ScopeBrace($"public Interface(IMachineGroup<{identifierName}> group, {identifierName} identifier) : base(group, identifier)"))
            {
            }
        }

        ssb.AppendLine();
    }

    internal void Generate_CreateInterface(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null)
        {
            return;
        }

        var identifierName = this.IdentifierObject!.FullName;
        using (var scope = ssb.ScopeBrace($"protected override void CreateInterface({identifierName} identifier)"))
        {
            using (var scope2 = ssb.ScopeBrace("if (this.InterfaceInstance == null)"))
            {
                ssb.AppendLine("this.Identifier = identifier;");
                ssb.AppendLine("this.InterfaceInstance = new Interface(this.Group, identifier);");
            }
        }

        ssb.AppendLine();
    }

    internal void Generate_InternalRun(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null || this.StateMethodList == null)
        {
            return;
        }

        using (var scope = ssb.ScopeBrace("protected override StateResult InternalRun(StateParameter parameter)"))
        {
            ssb.AppendLine($"var state = Unsafe.As<int, {this.StateName}>(ref this.CurrentState);");
            ssb.AppendLine("return state switch");
            ssb.AppendLine("{");
            ssb.IncrementIndent();

            foreach (var x in this.StateMethodList)
            {
                ssb.AppendLine($"State.{x.Name} => this.{x.Name}(parameter),");
            }

            ssb.AppendLine("_ => StateResult.Terminate,");
            ssb.DecrementIndent();
            ssb.AppendLine("};");
        }

        ssb.AppendLine();
    }

    internal void Generate_ChangeStateInternal(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        if (this.MachineObject == null || this.StateMethodList == null)
        {
            return;
        }

        using (var scope = ssb.ScopeBrace("protected override bool InternalChangeState(int state, bool rerun)"))
        {
            using (var scopeTerminated = ssb.ScopeBrace("if (this.Status == MachineStatus.Terminated)"))
            {
                ssb.AppendLine("return false;");
            }

            using (var scopeElse = ssb.ScopeBrace("else if (this.CurrentState == state)"))
            {
                ssb.AppendLine("return true;");
            }

            ssb.AppendLine();
            ssb.AppendLine($"var current = Unsafe.As<int, {this.StateName}>(ref this.CurrentState);");
            ssb.AppendLine("bool canExit = current switch");
            ssb.AppendLine("{");
            ssb.IncrementIndent();
            foreach (var x in this.StateMethodList)
            {
                if (x.CanExit)
                {
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}{ServiceMethod.CanExitName}(),");
                }
            }

            ssb.AppendLine("_ => true,");
            ssb.DecrementIndent();
            ssb.AppendLine("};");
            ssb.AppendLine();

            ssb.AppendLine($"var next = Unsafe.As<int, {this.StateName}>(ref state);");
            ssb.AppendLine("bool canEnter = next switch");
            ssb.AppendLine("{");
            ssb.IncrementIndent();
            foreach (var x in this.StateMethodList)
            {
                if (x.CanEnter)
                {
                    ssb.AppendLine($"State.{x.Name} => this.{x.Name}{ServiceMethod.CanEnterName}(),");
                }
                else
                {
                    ssb.AppendLine($"State.{x.Name} => true,");
                }
            }

            ssb.AppendLine("_ => false,");
            ssb.DecrementIndent();
            ssb.AppendLine("};");
            ssb.AppendLine();

            using (var scope2 = ssb.ScopeBrace("if (canExit && canEnter)"))
            {
                ssb.AppendLine($"this.CurrentState = state;");
                ssb.AppendLine("this.RequestRerun = rerun;");
                ssb.AppendLine("return true;");
            }

            using (var scope2 = ssb.ScopeBrace("else"))
            {
                ssb.AppendLine("return false;");
            }
        }

        ssb.AppendLine();
        ssb.AppendLine($"protected bool ChangeState({this.StateName} state, bool rerun = true) => this.InternalChangeState(Unsafe.As<{this.StateName}, int>(ref state), rerun);");
        ssb.AppendLine();
        ssb.AppendLine($"protected {this.NewIfDerived}{this.StateName} GetCurrentState() => Unsafe.As<int, {this.StateName}>(ref this.CurrentState);");
        ssb.AppendLine();

        /*if (this.DefaultStateMethod != null)
        {
            ssb.AppendLine();
            ssb.AppendLine($"protected override void IntInitState() => this.CurrentState = {this.DefaultStateMethod.Id};");
        }*/
    }
}
