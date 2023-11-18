// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace Netsphere.Responder;

internal class DefaultResponder
{
    public static void Register(Terminal terminal)
    {
        terminal.AddResponder(TestBlockResponder.Instance);
    }
}
