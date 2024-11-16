// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using Arc.Collections;

namespace Lp;

public partial class VaultControl
{
    public partial class Data
    {
        internal Data(VaultControl vault)
        {
            this.vault = vault;
        }

        #region FieldAndProperty

        private readonly VaultControl vault;
        private readonly OrderedMap<string, byte[]> nameToDecrypted = new();
        private string password = string.Empty;

        #endregion

        // Plaintext, Data(encrypted)
    }
}
