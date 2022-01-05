// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace Netsphere.Generator;

public class ServiceMethod
{
    public static ServiceMethod? Create(NetsphereObject machine, NetsphereObject method)
    {
        const string taskName = "System.Threading.Tasks.Task";
        var returnObject = method.Method_ReturnObject;
        if (returnObject?.BaseObject?.FullName != taskName && returnObject?.FullName != taskName)
        {// Invalid return type
            method.Body.ReportDiagnostic(NetsphereBody.Error_MethodReturnType, method.Location);
        }

        if (method.Body.Abort)
        {
            return null;
        }

        var serviceMethod = new ServiceMethod();
        serviceMethod.Location = method.Location;
        serviceMethod.Name = method.SimpleName;
        serviceMethod.MethodId = (uint)Arc.Crypto.FarmHash.Hash64(method.FullName);

        return serviceMethod;
    }

    public Location Location { get; private set; } = Location.None;

    public string Name { get; private set; } = string.Empty;

    public uint MethodId { get; private set; }

    public bool DuplicateId { get; internal set; }
}
