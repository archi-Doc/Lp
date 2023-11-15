﻿// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Netsphere.Block;

namespace Netsphere.Responder;

internal class TestBlockResponder : NetResponder<TestBlock, TestBlock>
{
    public static readonly NetResponder<TestBlock, TestBlock> Instance = new TestBlockResponder();

    public override TestBlock? Respond(TestBlock value)
    {
        return value;
    }
}
