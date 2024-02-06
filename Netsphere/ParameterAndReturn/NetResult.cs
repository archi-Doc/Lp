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
    Completed,
    InvalidOperation,
    SerializationError,
    DeserializationError,
    BlockSizeLimit,
    StreamLengthLimit,
    NoNodeInformation,
    NoNetwork,
    NoNetService,
    NoTransmission,
    NotAuthorized,
    NotFound,
}
