// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP.T3CS;

/// <summary>
/// Immutable linkage object.
/// </summary>
/// <typeparam name="TProof">The type of a linkage target.</typeparam>
[TinyhandObject(ReservedKeys = 4)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class Linkage<TProof> : IValidatable
    where TProof : Proof
{
    [Link(Primary = true, TargetMember = "ProofMics", Type = ChainType.Ordered)]
    public Linkage(TProof proof)
    {
        this.Proof = proof;
    }

    private Linkage()
    {
        this.Proof = default!;
    }

    [Key(0)]
    public TProof Proof { get; private set; }

    [Key(1, Level = 0)]
    public byte[]? Signature0 { get; private set; }

    [Key(2, Level = 1)]
    public byte[]? Signature1 { get; private set; }

    public long ProofMics
        => this.Proof.ProofMics;

    public bool Validate()
    {
        return this.Proof.Validate();
    }
}

/// <summary>
/// Immutable linkage object.
/// </summary>
[TinyhandObject(ReservedKeys = 4)]
[ValueLinkObject(Isolation = IsolationLevel.Serializable)]
public sealed partial class Linkage2 : IValidatable
{
    [Link(Primary = true, TargetMember = "ProofMics", Type = ChainType.Ordered)]
    public Linkage2(Proof proof)
    {
        this.Proof = proof;
    }

    private Linkage2()
    {
        this.Proof = default!;
    }

    [Key(0)]
    public Proof Proof { get; private set; }

    [Key(1, Level = 0)]
    public byte[]? Signature0 { get; private set; }

    [Key(2, Level = 1)]
    public byte[]? Signature1 { get; private set; }

    public long ProofMics
        => this.Proof.ProofMics;

    public bool Validate()
    {
        return this.Proof.Validate();
    }
}

/*
/// <summary>
/// Immutable linkage object.
/// </summary>
/// <typeparam name="T">The type of a linkage target.</typeparam>
[TinyhandObject(ReservedKeys = 4)]
public partial class Linkage<T> : IValidatable
    where T : IValidatable, ITinyhandSerialize<T>
{
    public Linkage(T target)
    {
        this.Target = target;
    }

    private Linkage()
    {
        this.Target = default!;
    }

    [Key(0)]
    public T Target { get; private set; }

    [Key(1, Level = 0)]
    public byte[]? Signature0 { get; private set; }

    [Key(2, Level = 1)]
    public byte[]? Signature1 { get; private set; }

    public bool Validate()
    {
        return this.Target.Validate();
    }
}

[TinyhandObject]
[ValueLinkObject]
public partial class Credential2 : Linkage<CreditPolicy>
{
    public Credential2(CreditPolicy target)
        : base(target)
    {
    }

    private Credential2()
        : base(default!)
    {
    }

    [Key(5)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public int Id { get; set; }
}

[TinyhandObject]
[ValueLinkObject]
public partial class Transaction : Linkage<Proof>
{
    public Transaction(Proof target)
        : base(target)
    {
    }

    private Transaction()
        : base(default!)
    {
    }

    [Key(5)]
    [Link(Primary = true, Type = ChainType.Unordered)]
    public int Id { get; set; }
}*/
