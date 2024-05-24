// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace Netsphere.Generator;

public class ServiceMethod
{
    public const string ByteArrayName = "byte[]";
    public const string MemoryOwnerName = "Arc.Unit.BytePool.RentMemory";
    public const string ReadOnlyMemoryOwnerName = "Arc.Unit.BytePool.RentReadOnlyMemory";
    public const string ReceiveStreamName = "Netsphere.ReceiveStream";
    public const string SendStreamName = "Netsphere.SendStream";
    public const string SendStreamAndReceiveName = "Netsphere.SendStreamAndReceive<TReceive>";
    public const string NetResultName = "Netsphere.NetResult";
    public const string ConnectBidirectionallyName = "Netsphere.INetServiceBidirectional.ConnectBidirectionally(Netsphere.Crypto.CertificateToken<Netsphere.ConnectionAgreement>)";
    public const string UpdateAgreementName = "Netsphere.INetServiceAgreement.UpdateAgreement(Netsphere.Crypto.CertificateToken<Netsphere.ConnectionAgreement>)";

    public enum Type
    {
        Other,
        NetResult,
        ByteArray,
        MemoryOwner,
        ReadOnlyMemoryOwner,
        ReceiveStream,
        SendStream,
        SendStreamAndReceive,
    }

    public enum MethodKind
    {
        Other,
        UpdateAgreement,
        ConnectBidirectionally,
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
            if (serviceMethod.ReturnObject?.Object is { } rt)
            {
                if (rt.Kind.IsReferenceType() &&
                method.IsReturnTypeArgument_NotNullable())
                {
                    method.Body.AddDiagnostic(NetsphereBody.Warning_NullableReferenceType, method.Location, rt.LocalName);
                }

                serviceMethod.ReturnType = NameToType(rt.OriginalDefinition?.FullName);
                if (serviceMethod.ReturnType == Type.SendStreamAndReceive)
                {
                    serviceMethod.StreamTypeArgument = rt.Generics_Arguments[0].FullName;
                }
            }
        }

        if (method.Method_Parameters.Length == 1)
        {
            serviceMethod.ParameterType = NameToType(method.Method_Parameters[0]);
        }

        /*if (serviceMethod.ReturnType == Type.SendStream)
        {
            method.Body.AddDiagnostic(NetsphereBody.Error_SendStreamRemoved, method.Location);
            return null;
        }
        else */if (serviceMethod.ReturnType == Type.SendStream ||
            serviceMethod.ReturnType == Type.SendStreamAndReceive)
        {
            if (/*method.Method_Parameters.Length > 1 || */method.Method_Parameters.Length == 0)
            {
                method.Body.AddDiagnostic(NetsphereBody.Error_SendStreamParam, method.Location);
                return null;
            }
            else if (method.Method_Parameters[method.Method_Parameters.Length - 1] != "long")
            {
                method.Body.AddDiagnostic(NetsphereBody.Error_SendStreamParam, method.Location);
                return null;
            }
        }

        if (method.FullName == UpdateAgreementName)
        {
            serviceMethod.Kind = MethodKind.UpdateAgreement;
        }
        else if (method.FullName == ConnectBidirectionallyName)
        {
            serviceMethod.Kind = MethodKind.ConnectBidirectionally;
        }

        return serviceMethod;
    }

    public ServiceMethod(NetsphereObject method)
    {
        this.method = method;
    }

    public Location Location => this.method.Location;

    public string SimpleName => this.method.SimpleName;

    public string LocalName => this.method.LocalName;

    public int ParameterLength => this.method.Method_Parameters.Length;

    public uint MethodId { get; private set; }

    public ulong Id { get; private set; }

    public string IdString => $"0x{this.Id:x}ul";

    public string MethodString => $"Method_{this.Id:x}";

    public WithNullable<NetsphereObject>? ReturnObject { get; internal set; }

    public Type ParameterType { get; private set; }

    public Type ReturnType { get; private set; }

    public string StreamTypeArgument { get; private set; } = string.Empty;

    public MethodKind Kind { get; private set; }

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

    public string GetParameterNames(string name, int decrement)
    {// string.Empty, a1, (a1, a2)
        var parameters = this.method.Method_Parameters;
        var length = parameters.Length - decrement;
        if (length <= 0)
        {
            return string.Empty;
        }
        else if (length == 1)
        {
            return name + "1";
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < length; i++)
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

    public string GetParameterTypes(int decrement)
    {// (int, string)
        var parameters = this.method.Method_Parameters;
        var length = parameters.Length - decrement;

        if (length <= 0)
        {
            return string.Empty;
        }
        else if (length == 1)
        {
            return parameters[0];
        }
        else
        {
            var sb = new StringBuilder();
            sb.Append("(");
            for (var i = 0; i < length; i++)
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

    public string GetTupleNames(string name, int decrement)
    {// value, value.Item1, value.Item2
        var parameters = this.method.Method_Parameters;
        var length = parameters.Length - decrement;

        if (length <= 0)
        {
            return string.Empty;
        }
        else if (length == 1)
        {
            return name;
        }
        else
        {
            var sb = new StringBuilder();
            for (var i = 0; i < length; i++)
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

    private static Type NameToType(string? name) => name switch
    {
        NetResultName => Type.NetResult,
        ByteArrayName => Type.ByteArray,
        MemoryOwnerName => Type.MemoryOwner,
        ReadOnlyMemoryOwnerName => Type.ReadOnlyMemoryOwner,
        ReceiveStreamName => Type.ReceiveStream,
        SendStreamName => Type.SendStream,
        SendStreamAndReceiveName => Type.SendStreamAndReceive,
        _ => Type.Other,
    };

    private NetsphereObject method;
}
