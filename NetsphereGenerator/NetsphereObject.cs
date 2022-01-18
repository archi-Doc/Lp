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

    public NetsphereObject? NetServiceBase { get; private set; } // For NetServiceObjectAttribute; Net service base implemented by this net service object.

    public ServiceFilter? ServiceFilter { get; private set; } // For NetServiceObjectAttribute; Service filters.

    public Dictionary<uint, ServiceMethod>? ServiceMethods { get; private set; } // For NetServiceInterface; Methods included in this net service interface.

    public string ClassName { get; set; } = string.Empty;

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

                        x.Check();
                    }

                    this.ServiceInterfaces.Add(x);
                }
            }

            if (this.ServiceInterfaces.Count == 0)
            {
                return;
            }

            this.ConfigureNetBase();
            this.ServiceFilter = ServiceFilter.CreateFromObject(this);

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

    public void ConfigureNetBase()
    {
        var baseObject = this.BaseObject;
        while (baseObject != null)
        {
            if (baseObject.Generics_IsGeneric)
            {// Generic
                if (baseObject.OriginalDefinition?.FullName == NetsphereBody.NetServiceBaseFullName2)
                {
                    this.NetServiceBase = baseObject;
                    return;
                }
            }
            else
            {// Not generic
                if (baseObject.FullName == NetsphereBody.NetServiceBaseFullName)
                {
                    this.NetServiceBase = baseObject;
                    return;
                }
            }

            baseObject = baseObject.BaseObject;
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
            this.ClassName = NetsphereBody.BackendClassName + Arc.Crypto.FarmHash.Hash32(this.FullName).ToString("x");

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
            this.ClassName = NetsphereBody.FrontendClassName + this.NetServiceInterfaceAttribute.ServiceId.ToString("x");

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
                        this.Body.AddDiagnostic(NetsphereBody.Error_DuplicateServiceMethod, s.Location, serviceMethod.MethodId);
                        this.Body.AddDiagnostic(NetsphereBody.Error_DuplicateServiceMethod, serviceMethod.Location, serviceMethod.MethodId);
                    }
                    else
                    {
                        this.ServiceMethods.Add(serviceMethod.MethodId, serviceMethod);
                    }
                }
            }
        }

        this.ServiceFilter?.CheckAndPrepare();
    }

    internal void GenerateFrontend(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var cls = ssb.ScopeBrace($"private class {this.ClassName} : {this.FullName}")) // {this.AccessibilityName}
        {
            /*ssb.AppendLine("public NetResult Result => this.result;");
            ssb.AppendLine();
            ssb.AppendLine("private NetResult result;");
            ssb.AppendLine();*/

            using (var ctr = ssb.ScopeBrace($"public {this.ClassName}(ClientTerminal clientTerminal)"))
            {
                // ssb.AppendLine("this.result = default;");
                ssb.AppendLine("this.ClientTerminal = clientTerminal;");
            }

            ssb.AppendLine();
            ssb.AppendLine("public ClientTerminal ClientTerminal { get; }");

            if (this.ServiceMethods != null)
            {
                foreach (var x in this.ServiceMethods.Values)
                {
                    ssb.AppendLine();
                    this.GenerateFrontend_Method(ssb, info, x);
                }
            }
        }
    }

    internal void GenerateFrontend_Method(ScopingStringBuilder ssb, GeneratorInformation info, ServiceMethod method)
    {
        var genericString = method.ReturnObject == null ? string.Empty : $"<{method.ReturnObject.FullNameWithNullable}>";
        var taskString = $"NetTask{genericString}";
        var returnTypeIsNetResult = method.ReturnObject?.FullName == NetsphereBody.NetResultFullName;
        var deserializeString = method.ReturnObject == null ? "NetResult" : method.ReturnObject.FullNameWithNullable;

        using (var scopeMethod = ssb.ScopeBrace($"public {taskString} {method.SimpleName}({method.GetParameters()})"))
        {
            ssb.AppendLine($"return new {taskString}(Core());");
            ssb.AppendLine();

            using (var scopeCore = ssb.ScopeBrace($"async Task<ServiceResponse{genericString}> Core()"))
            {
                if (method.ParameterType == ServiceMethod.Type.ByteArray)
                {
                    ssb.AppendLine("var owner = new ByteArrayPool.MemoryOwner(a1);");
                }
                else if (method.ParameterType == ServiceMethod.Type.MemoryOwner)
                {
                    ssb.AppendLine("var owner = a1.IncrementAndShare();");
                }
                else
                {
                    using (var scopeSerialize = ssb.ScopeBrace($"if (!LP.Block.BlockService.TrySerialize({method.GetParameterNames(NetsphereBody.ArgumentName)}, out var owner))"))
                    {
                        AppendReturn("NetResult.SerializationError");
                    }
                }

                ssb.AppendLine();
                ssb.AppendLine($"var response = await this.ClientTerminal.SendAndReceiveServiceAsync({method.IdString}, owner).ConfigureAwait(false);");
                ssb.AppendLine("owner.Return();");
                using (var scopeNoNetService = ssb.ScopeBrace("if (response.Result == NetResult.Success && response.Value.IsEmpty)"))
                {
                    AppendReturn("NetResult.NoNetService");
                }

                using (var scopeNotSuccess = ssb.ScopeBrace("else if (response.Result != NetResult.Success)"))
                {
                    AppendReturn("response.Result");
                }

                ssb.AppendLine();
                if (method.ReturnType == ServiceMethod.Type.ByteArray)
                {
                    ssb.AppendLine("var result = response.Value.Memory.ToArray();");
                    ssb.AppendLine("response.Value.Return();");
                }
                else if (method.ReturnType == ServiceMethod.Type.MemoryOwner)
                {
                    ssb.AppendLine("var result = response.Value;");
                }
                else
                {
                    using (var scopeDeserialize = ssb.ScopeBrace($"if (!Tinyhand.TinyhandSerializer.TryDeserialize<{deserializeString}>(response.Value.Memory, out var result))"))
                    {
                        AppendReturn("NetResult.DeserializationError");
                    }

                    ssb.AppendLine();
                    ssb.AppendLine("response.Value.Return();");
                }

                if (method.ReturnObject == null)
                {
                    ssb.AppendLine($"return default;");
                }
                else
                {
                    ssb.AppendLine($"return new(result);");
                }
            }
        }

        void AppendReturn(string netResult)
        {
            if (method.ReturnObject == null)
            {
                ssb.AppendLine($"return new({netResult});");
            }
            else
            {
                if (returnTypeIsNetResult)
                {
                    ssb.AppendLine($"return new({netResult}, {netResult});");
                }
                else
                {
                    ssb.AppendLine($"return new(default!, {netResult});");
                }
            }
        }
    }

    internal void GenerateBackend(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var cls = ssb.ScopeBrace($"private class {this.ClassName}"))
        {
            this.GenerateBackend_Constructor(ssb, info);

            if (this.ServiceInterfaces != null)
            {
                foreach (var x in this.ServiceInterfaces)
                {
                    this.GenerateBackend_Interface(ssb, info, x);
                }
            }

            ssb.AppendLine();
            ssb.AppendLine($"private {this.FullName} impl;");

            // Service filters
            this.ServiceFilter?.GenerateDefinition(ssb);
        }
    }

    internal void GenerateBackend_Constructor(ScopingStringBuilder ssb, GeneratorInformation info)
    {
        using (var scopeMethod = ssb.ScopeBrace($"public {this.ClassName}(IServiceProvider? serviceProvider, ServiceContext context)"))
        {
            ssb.AppendLine($"var impl = serviceProvider?.GetService(typeof({this.FullName})) as {this.FullName};");
            using (var scopeIf = ssb.ScopeBrace($"if (impl == null)"))
            {
                if (this.ObjectFlag.HasFlag(NetsphereObjectFlag.HasDefaultConstructor))
                {
                    ssb.AppendLine($"impl = new {this.FullName}();");
                }
                else
                {
                    ssb.AppendLine($"throw new InvalidOperationException($\"Could not create an instance of net service {{typeof({this.FullName}).ToString()}}.\");");
                }
            }

            ssb.AppendLine();

            // Service filters
            this.ServiceFilter?.GenerateInitialize(ssb, "context");

            // Set ServiceContext
            if (this.NetServiceBase != null)
            {
                if (this.NetServiceBase.Generics_IsGeneric)
                {
                    ssb.AppendLine($"(({this.NetServiceBase.FullName})impl).Context = ({this.NetServiceBase.Generics_Arguments[0].FullName})context;");
                }
                else
                {
                    ssb.AppendLine($"(({this.NetServiceBase.FullName})impl).Context = context;");
                }
            }

            ssb.AppendLine("this.impl = impl;");
        }
    }

    internal void GenerateBackend_Interface(ScopingStringBuilder ssb, GeneratorInformation info, NetsphereObject serviceInterface)
    {
        if (serviceInterface.ServiceMethods != null)
        {
            foreach (var x in serviceInterface.ServiceMethods.Values)
            {
                ssb.AppendLine();
                this.GenerateBackend_Method(ssb, info, serviceInterface, x);
            }
        }

        ssb.AppendLine();
        this.GenerateBackend_ServiceInfo(ssb, info, serviceInterface);
    }

    internal void GenerateBackend_Method(ScopingStringBuilder ssb, GeneratorInformation info, NetsphereObject serviceInterface, ServiceMethod method)
    {
        using (var scopeMethod = ssb.ScopeBrace($"private static async ValueTask {method.MethodString}(object obj, CallContext context)"))
        {
            if (method.ParameterType == ServiceMethod.Type.ByteArray)
            {
                ssb.AppendLine("var value = context.RentData.Memory.ToArray();");
            }
            else if (method.ParameterType == ServiceMethod.Type.MemoryOwner)
            {
                ssb.AppendLine("var value = context.RentData;");
            }
            else
            {
                using (var scopeDeserialize = ssb.ScopeBrace($"if (!LP.Block.BlockService.TryDeserialize<{method.GetParameterTypes()}>(context.RentData, out var value))"))
                {
                    ssb.AppendLine("context.Result = NetResult.DeserializationError;");
                    ssb.AppendLine("return;");
                }
            }

            ssb.AppendLine();

            // Backend
            ssb.AppendLine($"var backend = (({this.ClassName})obj).impl;");

            // Set ServiceContext
            /*if (this.NetServiceBase != null)
            {
                if (this.NetServiceBase.Generics_IsGeneric)
                {
                    ssb.AppendLine($"(({this.NetServiceBase.FullName})backend).Context = ({this.NetServiceBase.Generics_Arguments[0].FullName})context!;");
                }
                else
                {
                    ssb.AppendLine($"(({this.NetServiceBase.FullName})backend).Context = (ServiceContext)context!;");
                }
            }*/

            var prefix = string.Empty;
            if (method.ReturnObject != null)
            {
                prefix = "var result = ";
            }

            // task
            ssb.AppendLine($"var task = (({serviceInterface.FullName})backend).{method.SimpleName}({method.GetTupleNames("value")});");

            if (this.ServiceFilter == null)
            {
            }

            // ssb.AppendLine($"{prefix}await (({serviceInterface.FullName})backend).{method.SimpleName}({method.GetTupleNames("value")});");
            ssb.AppendLine($"{prefix}await task;");
            if (method.ReturnObject == null)
            {
                ssb.AppendLine("var result = NetResult.Success;");
            }

            ssb.AppendLine("context.RentData.Return();");
            if (method.ReturnType == ServiceMethod.Type.ByteArray)
            {// byte[] result;
                ssb.AppendLine("context.RentData = result != null ? new ByteArrayPool.MemoryOwner(result) : default;");
            }
            else if (method.ReturnType == ServiceMethod.Type.MemoryOwner)
            {// new ByteArrayPool.MemoryOwner result;
                ssb.AppendLine("context.RentData = result;");
            }
            else
            {
                using (var scopeSerialize = ssb.ScopeBrace($"if (!LP.Block.BlockService.TrySerialize(result, out context.RentData))"))
                {
                    ssb.AppendLine("context.Result = NetResult.SerializationError;");
                    ssb.AppendLine("return;");
                }
            }

            ssb.AppendLine("context.Result = NetResult.Success;");
        }
    }

    internal void GenerateBackend_ServiceInfo(ScopingStringBuilder ssb, GeneratorInformation info, NetsphereObject serviceInterface)
    {
        var serviceIdString = serviceInterface.NetServiceInterfaceAttribute!.ServiceId.ToString("x");
        using (var scopeMethod = ssb.ScopeBrace($"public static NetService.ServiceInfo ServiceInfo_{serviceIdString}()"))
        {
            ssb.AppendLine($"var si = new NetService.ServiceInfo(0x{serviceIdString}u, static (x, y) => new {this.ClassName}(x, y));");
            if (serviceInterface.ServiceMethods != null)
            {
                foreach (var x in serviceInterface.ServiceMethods.Values)
                {
                    ssb.AppendLine($"si.AddMethod(new NetService.ServiceMethod({x.IdString}, {x.MethodString}));");
                }
            }

            ssb.AppendLine("return si;");
        }
    }
}
