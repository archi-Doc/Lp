// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace CrystalData;

[TinyhandObject(ExplicitKeyOnly = true)]
public partial class Data
{
    public Data()
    {
    }

    #region FieldAndProperty

    public ICrystal Crystal { get; private set; } = default!;

    protected uint JournalToken { get; set; }

    #endregion

    protected internal void Initialize(ICrystal crystal, uint journalToken)
    {
        this.Crystal = crystal;
        this.JournalToken = journalToken;
    }
}
