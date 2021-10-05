// Copyright (c) All contributors. All rights reserved. Licensed under the MIT license.

namespace LP;

[TinyhandUnion(0, typeof(Piece_Punch))]
public partial interface IPiece
{
}

[TinyhandObject]
public partial class Piece_Punch : IPiece
{
}
