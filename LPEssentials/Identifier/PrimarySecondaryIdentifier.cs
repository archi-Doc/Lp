// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

[TinyhandObject]
public partial struct PrimarySecondaryIdentifier : IEquatable<PrimarySecondaryIdentifier>
{
    public PrimarySecondaryIdentifier(Identifier primaryId, Identifier? secondaryId = null)
    {
        this.PrimaryId = primaryId;
        this.SecondaryId = secondaryId;
    }

    public PrimarySecondaryIdentifier()
    {
        this.PrimaryId = Identifier.Zero;
        this.SecondaryId = Identifier.Zero;
    }

    [Key(0)]
    public Identifier PrimaryId;

    [Key(1)]
    public Identifier? SecondaryId;

    public bool Equals(PrimarySecondaryIdentifier other)
    {
        if (!this.PrimaryId.Equals(other.PrimaryId))
        {
            return false;
        }

        if (this.SecondaryId == null)
        {
            if (other.SecondaryId == null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            if (other.SecondaryId == null)
            {
                return false;
            }
        }

        return this.SecondaryId.Equals(other.SecondaryId);
    }

    public override int GetHashCode()
    {
        if (this.SecondaryId != null)
        {
            return (int)(this.PrimaryId.Id0 ^ this.SecondaryId.Id0);
        }
        else
        {
            return (int)this.PrimaryId.Id0;
        }
    }

    public override string ToString()
    {
        if (this.SecondaryId != null)
        {
            return $"Primary {this.PrimaryId.Id0:D16} Secondary {this.SecondaryId.Id0:D16} ";
        }
        else
        {
            return $"Primary {this.PrimaryId.Id0:D16}";
        }
    }
}
