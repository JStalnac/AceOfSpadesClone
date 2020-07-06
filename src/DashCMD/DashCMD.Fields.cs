using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Dash.CMD
{
    public static partial class DashCMD
    {
        public static Dictionary<string, CVar> CVars = new Dictionary<string, CVar>();

        static Dictionary<string, Command> commands = new Dictionary<string, Command>();
        static Dictionary<string, DashCMDScreen> screens = new Dictionary<string, DashCMDScreen>();

        static StreamWriter sw;
        static ConsoleStream outCs;
        static Thread consoleThread;

        public static int MaxSavedCommands = 30;
        static List<string> lastCommands = new List<string>();
        static int saveCommandI = 0;

        static int top = 0;
        static List<CLine> logLines = new List<CLine>();
        static StringBuilder typingCommand = new StringBuilder();
        static int typingCommandI;
        static bool isLinux = Environment.OSVersion.Platform == PlatformID.Unix;

        static int promptAnchor;

        static int MaxLogLines
        {
            get { return Console.BufferHeight - 10; }
        }
    }
}