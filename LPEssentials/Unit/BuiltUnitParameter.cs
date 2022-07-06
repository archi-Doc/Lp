// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System;
using Microsoft.Extensions.DependencyInjection;

namespace LP.Unit;

public class UnitParameter
{
    public UnitParameter()
    {
    }

    public void FromContext(RadioClass radio, UnitBuilderContext context)
{
        this.ServiceProvider = context.ServiceCollection.BuildServiceProvider();
        this.Radio = radio;
        this.CommandTypes = context.CommandList.ToArray();
    }

    public ServiceProvider ServiceProvider { get; private set; } = default!;

    public RadioClass Radio { get; private set; } = default!;

    public Type[] CommandTypes { get; private set; } = default!;
}
