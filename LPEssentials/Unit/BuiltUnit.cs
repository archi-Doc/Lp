// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;
using CrossChannel;
using Microsoft.Extensions.DependencyInjection;

namespace Arc.Unit;

/// <summary>
/// Unit class built by <see cref="UnitBuilder"/>.<br/>
/// <see cref="BuiltUnit"/> has <see cref="UnitContext"/> property.<br/>
/// Unit is an independent unit of function and dependency.
/// </summary>
public class BuiltUnit : UnitBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BuiltUnit"/> class.
    /// </summary>
    /// <param name="context"><see cref="UnitContext"/>.</param>
    public BuiltUnit(UnitContext context)
        : base(context)
    {
        this.Context = context;
    }

    public UnitContext Context { get; }
}
