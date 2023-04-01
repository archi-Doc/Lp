// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using CrystalData.Filer;

namespace CrystalData;

internal interface IFilerToCrystal : IDisposable
{
    public IRawFiler Filer { get; }
}
