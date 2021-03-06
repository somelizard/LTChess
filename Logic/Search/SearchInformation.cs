using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LTChess.Search
{
    public class SearchInformation
    {
        public Action? OnSearchDone;

        public Position Position;

        /// <summary>
        /// The depth to stop the search at.
        /// </summary>
        public int MaxDepth = 10;

        /// <summary>
        /// The number of nodes the search should stop at.
        /// </summary>
        public ulong MaxNodes = int.MaxValue;

        /// <summary>
        /// The time in milliseconds that the search should stop at.
        /// </summary>
        public double MaxSeachTime = 300000;

        /// <summary>
        /// The best move found.
        /// </summary>
        public Move BestMove = Move.Null;

        /// <summary>
        /// If true, then the search will stop
        /// </summary>
        public bool StopSearching = false;

        public Move[] PV;

        /// <summary>
        /// The evaluation of the best move.
        /// </summary>
        public int BestScore = 0;

        /// <summary>
        /// The time currently spent during the search.
        /// </summary>
        public double SearchTime = 0;

        /// <summary>
        /// The number of nodes/positions evaluated during the search.
        /// </summary>
        public ulong NodeCount = 0;

        public SearchInformation(Position p, int depth = 4)
        {
            this.Position = p;
            this.MaxDepth = depth;
            StopSearching = false;

            PV = new Move[MAX_DEPTH];
        }

        public override string ToString()
        {
            return "MaxDepth: " + MaxDepth + ", " + "MaxNodes: " + MaxNodes + ", " + "MaxSeachTime: " + MaxSeachTime + ", " 
                + "BestMove: " + BestMove.ToString() + ", " + "BestScore: " + BestScore + ", " + "SearchTime: " + SearchTime + ", " 
                + "NodeCount: " + NodeCount + ", " + "QNodeCount: " + SearchStatistics.QuiescenceNodes + ", " + "StopSearching: " + StopSearching;
        }
    }
}
