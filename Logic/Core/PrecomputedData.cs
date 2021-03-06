using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;


namespace LTChess.Data
{

    /// <summary>
    /// Precomputes Knight moves, neighboring squares, and the diagonals that each index is a part of
    /// </summary>
    public static class PrecomputedData
    {
        /// <summary>
        /// At each index, contains a ulong with bits set at each neighboring square.
        /// </summary>
        public static ulong[] NeighborsMask = new ulong[64];

        /// <summary>
        /// At each index, contains a mask of squares which neighbor the indices neighbors. So the mask for A1 contains A3, B3, C3, C2, C1.
        /// </summary>
        public static ulong[] OutterNeighborsMask = new ulong[64];

        /// <summary>
        /// At each index, contains a mask of each of the squares that a knight could move to.
        /// </summary>
        public static ulong[] KnightMasks = new ulong[64];
        public static Dictionary<Direction, int[]>[] Diagonals = new Dictionary<Direction, int[]>[64];

        /// <summary>
        /// Contains ulongs for each square with bits set along the A1-H8 diagonal (bottom left to top right, from White's perspective).
        /// So square E4 has bits set at B1, C2, D3, E4, F5... and G1 only has G1 and H2.
        /// </summary>
        public static ulong[] DiagonalMasksA1H8 = new ulong[64];

        /// <summary>
        /// Contains ulongs for each square with bits set along the A8-H1 diagonal (top left to bottom right, from White's perspective).
        /// So square E4 has bits set at A8, B7, C6, D5, E4, F3... and B1 only has B1 and A2.
        /// </summary>
        public static ulong[] DiagonalMasksA8H1 = new ulong[64];


        /// <summary>
        /// Bitboards containing all of the squares that a White pawn on an index attacks. A White pawn on A2 attacks B3 etc.
        /// </summary>
        public static ulong[] WhitePawnAttackMasks = new ulong[64];

        /// <summary>
        /// Bitboards containing all of the squares that a Black pawn on an index attacks. A Black pawn on A7 attacks B6 etc.
        /// </summary>
        public static ulong[] BlackPawnAttackMasks = new ulong[64];

        public static ulong[] WhitePawnMoveMasks = new ulong[64];
        public static ulong[] BlackPawnMoveMasks = new ulong[64];

        /// <summary>
        /// At each index, contains a mask of all of the squares above the index which determine whether or not a pawn is passed.
        /// </summary>
        public static ulong[] WhitePawnPassedMasks = new ulong[64];

        /// <summary>
        /// At each index, contains a mask of all of the squares below the index which determine whether or not a pawn is passed.
        /// </summary>
        public static ulong[] BlackPawnPassedMasks = new ulong[64];

        /// <summary>
        /// At each index, contains a ulong equal to (1UL << index).
        /// </summary>
        public static ulong[] SquareBB = new ulong[64];

        /// <summary>
        /// Bitboards with bits set at every index the exists in a line between two indices.
        /// Index using LineBB[piece1][piece2] where piece1 might be someone's king, and piece2 is another piece.
        /// The bit at index piece2 will always be set, no matter what.
        /// So LineBB[A1][H1] gives 254, or 01111111
        /// </summary>
        public static ulong[][] LineBB = new ulong[64][];

        /// <summary>
        /// Index using BetweenBB[piece1][piece2], this is the same as LineBB, but the index at piece2 is never set.
        /// So BetweenBB[A1][H1] gives 126, or 01111110
        /// </summary>
        public static ulong[][] BetweenBB = new ulong[64][];

        public static DiagonalInfo[,] InfoA1H8 = new DiagonalInfo[64, 64];
        public static DiagonalInfo[,] InfoA8H1 = new DiagonalInfo[64, 64];

        private static bool Initialized = false;

        static PrecomputedData()
        {
            if (!Initialized)
            {
                Initialize();
            }
        }

        public static void Initialize()
        {
            DoSquareBBs();

            DoNeighbors();
            DoOutterNeighbors();
            DoKnightMoves();
            DoDiagonals();
            DoPawnAttacks();

            DoPassedPawns();

            DoBetweenBBs();

            Initialized = true;
        }

        public static void Recalculate()
        {
            Stopwatch sw = Stopwatch.StartNew();
            Initialize();
            sw.Stop();
            Log("PrecomputedData done in " + sw.Elapsed.TotalSeconds + " s");
        }

        private static void DoSquareBBs()
        {
            for (int s = 0; s <= 63; ++s)
            {
                SquareBB[s] = (1UL << s);
            }
        }

        /// <summary>
        /// Calculates values for LineBB and BetweenBB, must be done after the diagonals have been calculated.
        /// </summary>
        private static void DoBetweenBBs()
        {
            for (int s1 = 0; s1 < 64; s1++)
            {
                LineBB[s1] = new ulong[64];
                BetweenBB[s1] = new ulong[64];
                int f1 = GetIndexFile(s1);
                int r1 = GetIndexRank(s1);
                for (int s2 = 0; s2 < 64; s2++)
                {
                    int f2 = GetIndexFile(s2);
                    int r2 = GetIndexRank(s2);
                    LineBB[s1][s2] |= SquareBB[s2];

                    if (OnSameDiagonal(s1, s2, out DiagonalInfo info))
                    {
                        int[] arr = Diagonals[s1][info.direction];
                        for (int i = Math.Max(info.i1, info.i2) - 1; i > Math.Min(info.i1, info.i2); i--)
                        {
                            LineBB[s1][s2] |= SquareBB[arr[i]];
                            BetweenBB[s1][s2] |= SquareBB[arr[i]];
                        }
                    }
                    
                    if (f1 == f2)
                    {
                        if (s1 > s2)
                        {
                            for (int i = s1 - 8; i > s2; i -= 8)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                        else
                        {
                            for (int i = s1 + 8; i < s2; i += 8)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                    }
                    else if (r1 == r2)
                    {
                        if (s1 > s2)
                        {
                            for (int i = s1 - 1; i > s2; i--)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                        else
                        {
                            for (int i = s1 + 1; i < s2; i++)
                            {
                                LineBB[s1][s2] |= SquareBB[i];
                                BetweenBB[s1][s2] |= SquareBB[i];
                            }
                        }
                    }

                }
            }
        }

        private static void DoPawnAttacks()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong whiteAttack = 0;
                ulong whiteMove = (1UL << (i + 8));

                ulong blackAttack = 0;
                ulong blackMove = (1UL << (i - 8));

                IndexToCoord(i, out int x, out int y);

                int wy = (y + 1);
                int by = (y - 1);

                if (y == 1)
                {
                    whiteMove |= (1UL << (i + 16));
                }

                if (y == 6)
                {
                    blackMove |= (1UL << (i - 16));
                }

                if (x > 0)
                {
                    if (i < A2)
                    {
                        BlackPawnAttackMasks[i] = 0;

                        whiteAttack |= (1UL << CoordToIndex(x - 1, wy));
                    }
                    else if (i > H7)
                    {
                        WhitePawnAttackMasks[i] = 0;

                        blackAttack |= (1UL << CoordToIndex(x - 1, by));
                    }
                    else
                    {
                        whiteAttack |= (1UL << CoordToIndex(x - 1, wy));
                        blackAttack |= (1UL << CoordToIndex(x - 1, by));
                    }
                    
                }
                if (x < 7)
                {
                    if (i < A2)
                    {
                        //  Set this to 0 since pawns don't attack squares that are outside of the bounds of the board.
                        BlackPawnAttackMasks[i] = 0;

                        whiteAttack |= (1UL << CoordToIndex(x + 1, wy));
                    }
                    else if (i > H7)
                    {
                        WhitePawnAttackMasks[i] = 0;

                        blackAttack |= (1UL << CoordToIndex(x + 1, by));
                    }
                    else
                    {
                        whiteAttack |= (1UL << CoordToIndex(x + 1, wy));
                        blackAttack |= (1UL << CoordToIndex(x + 1, by));
                    }
                }

                WhitePawnAttackMasks[i] = whiteAttack;
                BlackPawnAttackMasks[i] = blackAttack;

                WhitePawnMoveMasks[i] = whiteMove;
                BlackPawnMoveMasks[i] = blackMove;
            }
        } 

        private static void DoNeighbors()
        {
            for (int i = 0; i < 64; i++)
            {
                List<int> list = new List<int>();
                IndexToCoord(i, out int x, out int y);

                if (InBounds(x - 1, y + 1))
                {
                    list.Add(CoordToIndex(x - 1, y + 1));
                }
                if (InBounds(x, y + 1))
                {
                    list.Add(CoordToIndex(x, y + 1));
                }
                if (InBounds(x + 1, y + 1))
                {
                    list.Add(CoordToIndex(x + 1, y + 1));
                }

                if (InBounds(x - 1, y))
                {
                    list.Add(CoordToIndex(x - 1, y));
                }
                if (InBounds(x + 1, y))
                {
                    list.Add(CoordToIndex(x + 1, y));
                }

                if (InBounds(x - 1, y - 1))
                {
                    list.Add(CoordToIndex(x - 1, y - 1));
                }
                if (InBounds(x, y - 1))
                {
                    list.Add(CoordToIndex(x, y - 1));
                }
                if (InBounds(x + 1, y - 1))
                {
                    list.Add(CoordToIndex(x + 1, y - 1));
                }

                ulong mask = 0UL;
                foreach (int mv in list)
                {
                    mask |= (1UL << mv);
                }
                NeighborsMask[i] = mask;
            }
        }

        private unsafe static void DoOutterNeighbors()
        {
            for (int i = 0; i < 64; i++)
            {
                ulong mask = 0UL;
                ulong temp = NeighborsMask[i];
                while (temp != 0)
                {
                    mask |= NeighborsMask[lsb(temp)];
                    temp = poplsb(temp);
                }

                //  Mask out the original square.
                mask &= ~SquareBB[i];

                OutterNeighborsMask[i] = mask;
            }
        }

        private static void DoDiagonals()
        {
            for (int i = 0; i < 64; i++)
            {
                IndexToCoord(i, out int x, out int y);
                List<int> bltr = new List<int>();
                List<int> tlbr = new List<int>();

                int ix = x - 1;
                int iy = y - 1;
                while (ix >= 0 && iy >= 0)
                {
                    bltr.Insert(0, CoordToIndex(ix, iy));
                    ix--;
                    iy--;
                }

                ix = x;
                iy = y;
                while (ix <= 7 && iy <= 7)
                {
                    bltr.Add(CoordToIndex(ix, iy));
                    ix++;
                    iy++;
                }


                ix = x - 1;
                iy = y + 1;
                while (ix >= 0 && iy <= 7)
                {
                    tlbr.Insert(0, CoordToIndex(ix, iy));
                    ix--;
                    iy++;
                }

                ix = x;
                iy = y;
                //index2 = tlbr.Count;
                while (ix <= 7 && iy >= 0)
                {
                    tlbr.Add(CoordToIndex(ix, iy));
                    ix++;
                    iy--;
                }

                ulong maskA1 = 0UL;
                foreach (int mv in bltr)
                {
                    maskA1 |= (1UL << mv);
                }
                DiagonalMasksA1H8[i] = maskA1;

                ulong maskA8 = 0UL;
                foreach (int mv in tlbr)
                {
                    maskA8 |= (1UL << mv);
                }
                DiagonalMasksA8H1[i] = maskA8;

                Diagonals[i] = new Dictionary<Direction, int[]>
                {
                    { Direction.D_A1H8, bltr.ToArray() },
                    { Direction.D_A8H1, tlbr.ToArray() }
                };
            }
            
            for (int i = 0; i < 64; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    bool onSame = DetOnSameDiagonal(i, j, Direction.D_A1H8, out int a, out int b);
                    DiagonalInfo d1 = new DiagonalInfo(i, j, Direction.D_A1H8, onSame, a, b);
                    InfoA1H8[i, j] = d1;

                    bool onSame1 = DetOnSameDiagonal(i, j, Direction.D_A8H1, out int c, out int d);
                    DiagonalInfo d2 = new DiagonalInfo(i, j, Direction.D_A8H1, onSame1, c, d);
                    InfoA8H1[i, j] = d2;
                }
            }
        }

        private static void DoKnightMoves()
        {
            for (int i = 0; i < 64; i++)
            {
                int x = i % 8;
                int y = i / 8;

                List<int> temp = new List<int>();
                if (InBounds(x - 1, y + 2))
                {
                    temp.Add(CoordToIndex(x - 1, y + 2));
                }
                if (InBounds(x - 2, y + 1))
                {
                    temp.Add(CoordToIndex(x - 2, y + 1));
                }

                if (InBounds(x - 1, y - 2))
                {
                    temp.Add(CoordToIndex(x - 1, y - 2));
                }
                if (InBounds(x - 2, y - 1))
                {
                    temp.Add(CoordToIndex(x - 2, y - 1));
                }

                if (InBounds(x + 1, y + 2))
                {
                    temp.Add(CoordToIndex(x + 1, y + 2));
                }
                if (InBounds(x + 2, y + 1))
                {
                    temp.Add(CoordToIndex(x + 2, y + 1));
                }

                if (InBounds(x + 1, y - 2))
                {
                    temp.Add(CoordToIndex(x + 1, y - 2));
                }
                if (InBounds(x + 2, y - 1))
                {
                    temp.Add(CoordToIndex(x + 2, y - 1));
                }

                ulong mask = 0UL;
                foreach(int mv in temp)
                {
                    mask |= (1UL << mv);
                }
                KnightMasks[i] = mask;
            }
        }

        private static void DoPassedPawns()
        {
            for (int idx = 0; idx < 64; idx++)
            {
                IndexToCoord(idx, out int x, out int y);
                ulong whiteRanks = 0UL;
                for (int rank = y; rank < 7; rank++)
                {
                    whiteRanks |= (Rank1BB << (8 * rank));
                }

                ulong blackRanks = 0UL;
                for (int rank = y; rank > 0; rank--)
                {
                    blackRanks |= (Rank1BB << (8 * rank));
                }

                //  files includes idx's file, and the files to idx's left and right if they are on the same rank still (between B and G)
                ulong files = GetFileBB(idx);
                if (GetIndexRank(idx - 1) == y)
                {
                    files |= GetFileBB(idx - 1);
                }
                if (GetIndexRank(idx + 1) == y)
                {
                    files |= GetFileBB(idx - 1);
                }

                WhitePawnPassedMasks[idx] = whiteRanks & files;
                BlackPawnPassedMasks[idx] = blackRanks & files;
            }
        }

        /// <summary>
        /// Returns true if <paramref name="index1"/> and <paramref name="index2"/> exist on the same diagonal.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="diagonal">Set to the Direction that the two indicies share, or Direction.D_A1H8 if they don't.</param>
        /// <param name="iIndex1">The index that <paramref name="index1"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        /// <param name="iIndex2">The index that <paramref name="index2"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        [MethodImpl(Inline)]
        private static unsafe bool DetOnSameDiagonal(int index1, int index2, Direction direction, out int iIndex1, out int iIndex2)
        {
            if (direction == Direction.D_A1H8)
            {
                iIndex1 = iIndex2 = 8;
                ulong d1 = DiagonalMasksA1H8[index1];
                if ((d1 & (SquareBB[index2])) != 0)
                {
                    int pops = 0;
                    while (d1 != 0)
                    {
                        int idx = lsb(d1);
                        d1 = poplsb(d1);

                        if (idx == index1)
                        {
                            iIndex1 = pops;
                        }
                        if (idx == index2)
                        {
                            iIndex2 = pops;
                        }
                        if (iIndex1 != 8 && iIndex2 != 8)
                        {
                            return true;
                        }

                        pops++;
                    }
                }
            }
            else
            {
                iIndex1 = iIndex2 = 8;
                ulong d2 = DiagonalMasksA8H1[index1];
                if ((d2 & (SquareBB[index2])) != 0)
                {
                    int pops = 0;
                    while (d2 != 0)
                    {
                        int idx = msb(d2);

                        if (idx == index1)
                        {
                            iIndex1 = pops;
                        }
                        if (idx == index2)
                        {
                            iIndex2 = pops;
                        }
                        if (iIndex1 != 8 && iIndex2 != 8)
                        {
                            return true;
                        }

                        d2 = popmsb(d2);
                        pops++;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if <paramref name="index1"/> and <paramref name="index2"/> exist on the same diagonal.
        /// </summary>
        /// <param name="index1">The first index.</param>
        /// <param name="index2">The second index.</param>
        /// <param name="diagonal">Set to the Direction that the two indicies share, or Direction.D_A1H8 if they don't.</param>
        /// <param name="iIndex1">The index that <paramref name="index1"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        /// <param name="iIndex2">The index that <paramref name="index2"/> exists at in <paramref name="diagonal"/>, or 0 if it doesn't.</param>
        [MethodImpl(Inline)]
        public static unsafe bool OnSameDiagonal(int index1, int index2, out DiagonalInfo info)
        {
            info = InfoA1H8[index1, index2];
            if (info.onSame)
            {
                return true;
            }

            info = InfoA8H1[index1, index2];
            if (info.onSame)
            {
                return true;
            }

            return false;
        }

    }

    public readonly struct DiagonalInfo
    {
        public readonly int index1;
        public readonly int index2;
        public readonly Direction direction;
        public readonly bool onSame;
        public readonly int i1;
        public readonly int i2;

        public DiagonalInfo(int index1, int index2, Direction direction, bool onSame, int i1, int i2)
        {
            this.index1 = index1;
            this.index2 = index2;
            this.direction = direction;
            this.onSame = onSame;
            this.i1 = i1;
            this.i2 = i2;
        }
    }


}
