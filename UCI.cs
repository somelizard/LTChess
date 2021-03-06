using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static LTChess.Data.RunOptions;
using LTChess.Search;
using LTChess.Magic;
using LTChess.Data;
using static LTChess.Data.Squares;
using System.Diagnostics;

namespace LTChess.Core
{
    public class UCI
    {
        public Position position;
        public SearchInformation info;

        public const string Filename = "ucilog.txt";
        public const string FilenameLast = "ucilog_last.txt";

        public UCI()
        {
            position = new Position();
            info = new SearchInformation(position, 10);
            info.OnSearchDone += OnSearchDone;
            if (File.Exists(Filename))
            {
                File.Move(Filename, FilenameLast, true);
            }
        }

        public void SendString(string s)
        {
            Console.WriteLine(s);
            LogString("[OUT]: " + s);
        }

        public static void LogString(string s)
        {
            using StreamWriter file = new(Filename, append: true);
            file.WriteLineAsync(s);
        }

        public string[] ReceiveString(out string cmd)
        {
            string input = Console.ReadLine();
            if (input == null || input.Length == 0)
            {
                cmd = "uh oh";
                return new string[0];
            }

            string[] splits = input.Split(" ");
            cmd = splits[0].ToLower();
            string[] param = splits.ToList().GetRange(1, splits.Length - 1).ToArray();

            LogString("[IN]: " + input);

            return param;
        }

        public void Run()
        {
            SendString("id name LTChess 3.0");
            SendString("id author Liam McGuire");
            SendString("option name hello type spin default 2 min 1 max 3");
            SendString("uciok");
            InputLoop();
        }

        private void InputLoop()
        {
            while (true)
            {
                string[] param = ReceiveString(out string cmd);

                if (cmd == "quit")
                {
                    LogString("[INFO]: Exiting with code " + 1001);
                    Environment.Exit(1001);
                }
                else if (cmd == "isready")
                {
                    SendString("readyok");
                }
                else if (cmd == "ucinewgame")
                {
                    position = new Position();
                }
                else if (cmd == "position")
                {
                    if (param[0] == "startpos")
                    {
                        LogString("Set position to " + InitialFEN);
                        position = new Position();
                    }
                    else
                    {
                        Debug.Assert(param[0] == "fen");
                        string fen = param[1];
                        for (int i = 2; i < param.Length; i++)
                        {
                            fen += " " + param[i];
                        }
                        LogString("Set position to " + fen);
                        position = new Position(fen);
                    }
                }
                else if (cmd == "go")
                {
                    info.StopSearching = false;
                    Go(param);
                }
                else if (cmd == "stop")
                {
                    info.StopSearching = true;
                    LogString("[INFO]: Stopping search");
                }
                else if (cmd == "leave")
                {
                    LogString("[INFO]: Leaving");
                    return;
                }
            }
        }

        private void OnSearchDone()
        {
            SendEval(info);
        }

        private void SendEval(SearchInformation info)
        {
            SendString(FormatSearchInformation(info));
        }

        private void Go(string[] param)
        {
            for (int i = 0; i < param.Length; i++)
            {
                if (param[i] == "movetime")
                {
                    info.SearchTime = double.Parse(param[i + 1]);
                    LogString("[INFO]: SearchTime is set to " + info.SearchTime);
                }
                else if (param[i] == "depth")
                {
                    if (i + 1 >= param.Length)
                    {
                        break;
                    }
                    if (int.TryParse(param[i + 1], out int reqDepth))
                    {
                        info.MaxDepth = reqDepth;
                        LogString("[INFO]: MaxDepth is set to " + info.MaxDepth);
                    }
                }
            }

            Task.Run(DoSearch);
        }

        private void DoSearch()
        {
            NegaMax.IterativeDeepen(ref info);
            SendString("bestmove " + info.BestMove.ToString());
        }
    }
}
