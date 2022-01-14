// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace Netsphere.Generator;

public class ServiceMethod
{
    private const string ByteArrayName = "byte[]";
    private const string MemoryOwnerName = "LP.ByteArrayPool.MemoryOwner";

    public enum Type
    {
        Other,
        ByteArray,
        MemoryOwner,
    }

    public static ServiceMethod? Create(NetsphereObject obj, NetsphereObject method)
    {
        const string taskName = "Netsphere.NetTask";
        const string taskName2 = "Netsphere.NetTask<TResponse>";

        var returnObject = method.Method_ReturnObject;
        if (returnObject == null)
        {
            return null;
        }

        if (returnObject.FullName != taskName &&
            returnObject.OriginalDefinition?.FullName != taskName2)
        {// Invalid return type
            method.Body.ReportDiagnostic(NetsphereBody.Error_MethodReturnType, method.Location);
        }

        if (method.Body.Abort)
        {
            return null;
        }

        var serviceMethod = new ServiceMethod(method);
        serviceMethod.MethodId = (uint)Arc.Crypto.FarmHash.Hash64(method.FullName);
        if (obj.NetServiceInterfaceAttribute == null)
        {
            serviceMethod.Id = serviceMethod.MethodId;
        }
        else
        {
            serviceMethod.Id = (ulong)obj.NetServiceInterfaceAttribute.ServiceId << 32 | serviceMethod.MethodId;
        }

        if (returnObject.Generics_Arguments.Length > 0)
        {
            serviceMethod.ReturnObject = returnObject.TypeObjectWithNullable?.Generics_ArgumentsWithNullable[0];
            if (serviceMethod.ReturnObject is { } rt)
            {
                if (rt.Object.Kind.IsReferenceType() &&
                rt.Nullable == Arc.Visceral.NullableAnnotation.NotAnnotated)
                {
                    method.Body.AddDiagnostic(NetsphereBody.Warning_NullableReferenceType, method.Location, rt.Object.LocalName);
                }

                serviceMethod.ReturnType = NameToType(rt.FullName);
            }
        }

        if (method.Method_Parameters.Length == 1)
        {
            serviceMethod.ParameterType = NameToType(method.Method_Parameters[0]);
        }

        return serviceMethod;
    }

    public ServiceMethod(NetsphereObject method)
    {
        this.method = method;
    }

    public Location Location => this.method.Location;

    public string SimpleName => this.method.SimpleName;

    public uint MethodId { get; private set; }

    public ulong Id { get; private set; }

    public string IdString => $"0x{this.Id:x}ul";

    public string MethodString => $"Method_{this.MethodId:x}";

    public WithNullable<NetsphereObject>? ReturnObject { get; internal set; }

    public Type ParameterType { get; private set; }

    public Type ReturnType { get; private set; }

    public string GetParameters()
    {// int a1, string a2
        var sb = new StringBuilder();
        for (var i = 0; i < this.method.Method_Parameters.Length; i++)
        {
            if (i != 0)
            {
                sb.Append(", ");
            }

            sb.Append(this.method.Method_Parameters[i]);
            sb.Append(" ");
            sb.Append(NetsphereBody.ArgumentName);
            sb.Append(i + 1);
        }

        return sb.ToString();
    }

    public string GetParameterNames(string name)
    {// string.Empty, a1, (a1, a2)
        var parameters = this.method.Method_Parameters;
        if (parameters.Length == 0)
        {
            return string.Empty;
        }
        else if (parameters.Length == 1)
        {
            return name + "1";
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < this.method.Method_Parameters.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(name);
                sb.Append(i + 1);
            }

            sb.Append(")");
            return sb.ToString();
        }
    }

    public string GetParameterTypes()
    {// (int, string)
        var parameters = this.method.Method_Parameters;
        if (parameters.Length == 0)
        {
            return string.Empty;
        }
        else if (parameters.Length == 1)
        {
            return parameters[0];
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < this.method.Method_Parameters.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(parameters[i]);
            }

            sb.Append(")");
            return sb.ToString();
        }
    }

    public string GetTupleNames(string name)
    {// value, value.Item1, value.Item2
        var parameters = this.method.Method_Parameters;
        if (parameters.Length == 0)
        {
            return string.Empty;
        }
        else if (parameters.Length == 1)
        {
            return name;
        }
        else
        {
            var sb = new StringBuilder();
            for (var i = 0; i < this.method.Method_Parameters.Length; i++)
            {
                if (i != 0)
                {
                    sb.Append(", ");
                }

                sb.Append(name);
                sb.Append(".Item");
                sb.Append(i + 1);
            }

            return sb.ToString();
        }
    }

    private static Type NameToType(string name) => name switch
    {
        ByteArrayName => Type.ByteArray,
        MemoryOwnerName => Type.MemoryOwner,
        _ => Type.Other,
    };

    private NetsphereObject method;
}
