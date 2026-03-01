// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp;

/// <summary>
/// Provides methods for user interaction via the console or GUI, including input and notifications.
/// </summary>
public interface IUserInterfaceService : IConsoleService
{
    /// <summary>
    /// Enqueues a line of text to be displayed to the user.
    /// </summary>
    /// <param name="message">The message to display. If null, an empty line is enqueued.</param>
    void EnqueueLine(string? message = null);

    /// <summary>
    /// Reads a line of input from the user.
    /// </summary>
    /// <param name="cancelOnEscape">If true, allows the user to cancel input with the Escape key.</param>
    /// <param name="description">An optional description to display to the user.</param>
    /// <returns>A task that represents the asynchronous read operation. The result contains the input and its status.</returns>
    Task<InputResult> ReadLine(bool cancelOnEscape, string? description);

    /// <summary>
    /// Prompts the user for a yes/no response.
    /// </summary>
    /// <param name="cancelOnEscape">If true, allows the user to cancel input with the Escape key.</param>
    /// <param name="description">An optional description to display to the user.</param>
    /// <returns>A task that represents the asynchronous read operation. The result indicates the user's choice.</returns>
    Task<InputResultKind> ReadYesNo(bool cancelOnEscape, string? description);

    /// <summary>
    /// Reads a password input from the user, masking the input.
    /// </summary>
    /// <param name="cancelOnEscape">If true, allows the user to cancel input with the Escape key.</param>
    /// <param name="description">An optional description to display to the user.</param>
    /// <returns>A task that represents the asynchronous read operation. The result contains the password and its status.</returns>
    Task<InputResult> ReadPassword(bool cancelOnEscape, string? description);

    /// <summary>
    /// Notifies the user with a message at the specified log level.
    /// </summary>
    /// <param name="logger">The logger to use for logging the notification.</param>
    /// <param name="level">The log level of the notification.</param>
    /// <param name="message">The message to display.</param>
    /// <returns>A task that represents the asynchronous notification operation.</returns>
    Task Notify(ILogger? logger, LogLevel level, string message);

    void WriteLineDefault(string? message);

    void WriteLineWarning(string? message);

    void WriteLineError(string? message);
}
