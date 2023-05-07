// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData.UserInterface;

internal class CrystalDataQueryContinue : ICrystalDataQuery
{
    Task<AbortOrContinue> ICrystalDataQuery.NoCheckFile()
        => Task.FromResult(AbortOrContinue.Continue);
}
