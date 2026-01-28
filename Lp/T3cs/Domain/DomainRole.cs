// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Lp.T3cs;

/// <summary>
/// Represents the role within a domain.
/// </summary>
public enum DomainRole : byte
{
    /// <summary>
    /// Participates as a general user.
    /// </summary>
    User,

    /// <summary>
    /// Located directly under the Root; shares and distributes domain information.
    /// </summary>
    Peer,

    /// <summary>
    /// Serves as the foundation of trust for the domain and manages and distributes domain information.
    /// </summary>
    Root,
}
