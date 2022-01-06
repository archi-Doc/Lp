// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace Netsphere.Generator;

public class ServiceMethod
{
    public static ServiceMethod? Create(NetsphereObject obj, NetsphereObject method)
    {
        const string taskName = "System.Threading.Tasks.Task";
        var returnObject = method.Method_ReturnObject;
        if (returnObject == null)
        {
            return null;
        }

        if (returnObject.BaseObject?.FullName != taskName && returnObject.FullName != taskName)
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
            serviceMethod.ReturnType = returnObject.Generics_Arguments[0];
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

    public string IdString => $"0x{this.Id:x8}ul";

    public NetsphereObject? ReturnType { get; internal set; }

    public string GetParameters()
    {// int a0, string a1
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
            sb.Append(i);
        }

        return sb.ToString();
    }

    public string GetParameterNames()
    {// (a0, a1)
        var parameters = this.method.Method_Parameters;
        if (parameters.Length == 0)
        {
            return string.Empty;
        }
        else if (parameters.Length == 1)
        {
            return NetsphereBody.ArgumentName + "0";
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

                sb.Append(NetsphereBody.ArgumentName);
                sb.Append(i);
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

    private NetsphereObject method;
}
