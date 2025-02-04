// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

using System.Runtime.Serialization;
using Arc.Collections;

namespace Lp.T3cs;

[TinyhandObject]
public partial class Credentials
{
    #region FieldAndProperty

    [Key(0)]
    public Evidence.GoshujinClass MergerCredentials { get; private set; } = new();

    [Key(1)]
    public Evidence.GoshujinClass RelayCredentials { get; private set; } = new();

    [Key(2)]
    public Evidence.GoshujinClass CreditCredentials { get; private set; } = new();

    #endregion

    public Credentials()
    {
    }

    private static void ValidateCredentials(Evidence.GoshujinClass credentials)
    {
        using (credentials.LockObject.EnterScope())
        {
            TemporaryList<Evidence> toDelete = default;
            foreach (var evidence in credentials)
            {
                if (!evidence.Validate())
                {
                    toDelete.Add(evidence);
                }
            }

            foreach (var evidence in toDelete)
            {
                credentials.Remove(evidence);
            }
        }
    }

    [TinyhandOnDeserialized]
    private void OnDeserialized()
    {
        ValidateCredentials(this.MergerCredentials);
        ValidateCredentials(this.RelayCredentials);
        ValidateCredentials(this.CreditCredentials);
    }
}
