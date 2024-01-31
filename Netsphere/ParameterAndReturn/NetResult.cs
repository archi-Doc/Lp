// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere;

/// <summary>
/// Represents a result of network transmission.
/// </summary>
public enum NetResult
{
    Success,
    UnknownException,
    Timeout,
    Canceled,
    Closed,
    SerializationError,
    DeserializationError,
    BlockSizeLimit,
    StreamLengthLimit,
    NoNodeInformation,
    NoNetwork,
    NoNetService,
    NoCallContext,
    NotAuthorized,
    NoTransmission,
    Completed,
    AlreadySent,
}
