// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Arc.Unit;

/// <summary>
/// Interface for log output.<br/>
/// Log source is <see cref="DefaultLog"/>.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Try to get <see cref="ILog"/> instance from the log source and <see cref="LogLevel"/>.
    /// </summary>
    /// <param name="logLevel">The log level.</param>
    /// <returns><see cref="ILog"/> instance.</returns>
    public ILog? TryGet(LogLevel logLevel = LogLevel.Information);
}

/// <summary>
/// Interface for log output.<br/>
/// Log source is fixed (TLogSource).
/// </summary>
/// <typeparam name="TLogSource">The type of log source.</typeparam>
public interface ILogger<TLogSource> : ILogger
{
}
