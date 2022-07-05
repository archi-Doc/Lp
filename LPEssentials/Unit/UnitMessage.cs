// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace LP.Unit;

public static class UnitMessage
{// Create instance -> Configure -> LoadAsync -> StartAsync -> StopAsync -> SaveAsync (multiple)
    /// <summary>
    /// Configure objects.<br/>
    /// </summary>
    public record Configure();

    /// <summary>
    /// Start unit objects.<br/>
    /// </summary>
    /// <param name="ParentCore">ParentCore.</param>
    public record RunAsync(ThreadCoreBase ParentCore);

    /// <summary>
    /// Terminate unit objects.<br/>
    /// </summary>
    public record TerminateAsync();

    /// <summary>
    /// Deserialize unit objects.<br/>
    /// </summary>
    public record LoadAsync();

    /// <summary>
    ///  Serialize objects.<br/>
    /// </summary>
    public record SaveAsync();
}
