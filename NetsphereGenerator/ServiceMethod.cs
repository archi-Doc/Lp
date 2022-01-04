// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Arc.Visceral;
using Microsoft.CodeAnalysis;

namespace Netsphere.Generator;

public class ServiceMethod
{
    public static ServiceMethod? Create(NetsphereObject machine, NetsphereObject method, VisceralAttribute attribute)
    {
        var flag = false;
        if (method.Method_ReturnObject?.FullName != NetsphereBody.StateResultFullName)
        {// Invalid return type
            flag = true;
        }

        if (flag)
        {
            method.Body.ReportDiagnostic(NetsphereBody.Error_MethodFormat, attribute.Location);
        }

        if (method.Body.Abort)
        {
            return null;
        }

        var stateMethod = new ServiceMethod();
        stateMethod.Location = attribute.Location;
        stateMethod.Name = method.SimpleName;

        return stateMethod;
    }

    public Location Location { get; private set; } = Location.None;

    public string Name { get; private set; } = string.Empty;

    public bool DuplicateId { get; internal set; }
}
