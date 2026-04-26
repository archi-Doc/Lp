// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.CompilerServices;

namespace Lp;

#pragma warning disable SA1203 // Constants should appear before fields

/// <summary>
/// Centralizes the parameters used by Lp.<br/>
/// They may be changed for performance improvements or resource savings.
/// </summary>
public static class LpParameters
{
    public const int DomainRadiantQueueCapacity = 32;
    public static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(3);
    public static readonly int ExitDelayMilliseconds = 500;

    static LpParameters()
    {
    }

#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    [ModuleInitializer]
#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
    public static void Initialize()
    {
    }
}
