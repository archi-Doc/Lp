// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Threading;

namespace Arc.Unit;

public static class UnitMessage
{// Create instance -> Prepare -> LoadAsync -> RunAsync -> TerminateAsync, SaveAsync (after Configure)
    /// <summary>
    /// Prepare objects.<br/>
    /// </summary>
    public record Prepare();

    /// <summary>
    /// Deserialize unit objects.<br/>
    /// Called once after <see cref="Prepare()"/>.<br/>
    /// Throw <see cref="PanicException"/> to abort the procedure.
    /// </summary>
    public record LoadAsync();

    /// <summary>
    /// Start unit objects.<br/>
    /// Called once after <see cref="LoadAsync()"/>.
    /// </summary>
    /// <param name="ParentCore">ParentCore.</param>
    public record RunAsync(ThreadCoreBase ParentCore);

    /// <summary>
    /// Terminate unit objects.<br/>
    /// Called only once at the beginning of the termination process.
    /// </summary>
    public record TerminateAsync();

    /// <summary>
    ///  Serialize objects.<br/>
    ///  Called multiple times after <see cref="Prepare()"/>.
    /// </summary>
    public record SaveAsync();
}
