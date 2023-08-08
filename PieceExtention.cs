﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCsSystem;

public static class PieceExtention
{
    public static List<Piece> GetAllPiecesInRadius(Vector3 position, float radius)
    {
        var result = new List<Piece>();
        foreach (Piece allPiece in Piece.s_allPieces)
        {
            if (allPiece.gameObject.layer != Piece.s_ghostLayer &&
                (double)Vector3.Distance(position, allPiece.transform.position) < (double)radius)
                result.Add(allPiece);
        }

        return result;
    }
}