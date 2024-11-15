// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Lp;

public partial class Vault
{
    public partial class Data
    {
        internal Data(Vault vault)
        {
            this.vault = vault;
        }

        #region FieldAndProperty

        private readonly Vault vault;
        private readonly OrderedMap<string, byte[]> nameToDecrypted = new();
        private string password = string.Empty;

        #endregion

        // Plaintext, Data(encrypted)
    }
}
