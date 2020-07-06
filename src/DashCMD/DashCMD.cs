using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

/* DashCMD.cs
 * Author: Ethan Lafrenais
 * Last Update: 12/12/2015
 */

namespace Dash.CMD
{
    /// <summary>
    /// A command method called when a command is used in DashCMD.
    /// </summary>
    /// <param name="args">Arguments that were passed with the command.</param>
    public delegate void DashCMDCommand(string [] args);

    /// <summary>
    /// A wrapper for the console that provides a managed way of use.
    /// </summary>
    public static partial class DashCMD
    {
        /// <summary>
        /// Current version of DashCMD.
        /// </summary>
        public static readonly string Version = "1.5";

        /// <summary>
        /// Is there a console handle active?
        /// </summary>
        public static bool ConsoleHandleExists { get; private set; }

        /// <summary>
        /// Safley sets the console title
        /// </summary>
        public static string Title
        {
            get { return ConsoleHandleExists ? Console.Title : "Console"; }
            set { if (ConsoleHandleExists)Console.Title = value; }
        }

        public static bool PrependTimestamp = true;
        public static CultureInfo TimeCulture = new CultureInfo("en-US");

        static bool SupressTimestamps;
        static bool TryStarted;

        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        static void TryStart(bool allocConsole)
        {
            if (TryStarted)return;
            TryStarted = true;

            // Only attempt the kernel32 method if this is windows.
            if (allocConsole && Environment.OSVersion.Platform == PlatformID.Win32NT)
                AllocConsole();

            try
            {
                // Check if the console has a handle
                Console.Clear();
                // If we didn't crash their, the handle exists.
                ConsoleHandleExists = true;

                outCs = new ConsoleStream(new MemoryStream());

                sw = new StreamWriter(outCs);
                sw.AutoFlush = true;

                outCs.OnConsoleWrite += outCs_OnConsoleWrite;

                OnMainScreen = true;

                #region Commands
                // Setup default commands
                AddCommand("help", "Displays basic help for all commands.",
                    delegate(string [] args)
                    {
                        Console.WriteLine("Defined Commands ({0}):\n", commands.Count);

                        foreach (var pair in commands)
                            if (!pair.Value.hideInHelp)
                                Console.WriteLine(String.Format("{0}{1}", pair.Value.command.PadRight(20), pair.Value.help));

                        WriteImportant("\nYou can also use the argument --syntax or --? for each command, to view that commands syntax.\n");
                    });

                AddCommand("screens", "Displays all registered screens.",
                    delegate(string [] args)
                    {
                        Console.WriteLine("Defined Screens ({0}):\n", screens.Count);

                        foreach (var pair in screens)
                            Console.WriteLine(String.Format("{0}{1}", pair.Value.Name.PadRight(20), pair.Value.Description));

                        Console.WriteLine();
                    });

                AddCommand("screen",
                    "Switches to a screen.", "screen [screen name] (when name not provided, it goes back to main screen)",
                    delegate(string [] args)
                    {
                        string screenName = CombineArgs(args);
                        if (screens.ContainsKey(screenName))
                        {
                            OnMainScreen = false;
                            SwitchScreen(screens [screenName]);
                        }
                        else
                            WriteError(string.Format("Screen '{0}' is not defined.", screenName));
                    });

                AddCommand("cls", "Clears the screen.",
                    delegate(string [] args)
                    {
                        Console.Clear();
                        logLines.Clear();
                        top = 0;
                        WriteInputLine();
                    });

                AddCommand("allvars", "Prints a list of all available CVars.", "allvars [page]",
                    delegate(string [] args)
                    {
                        // Grab page number
                        int page = 0;
                        if (args.Length > 0)
                            int.TryParse(args [0], out page);

                        // Calculate number of pages
                        int maxLines = (Console.BufferHeight - 2);
                        double _numpages = (double)CVars.Count / (double)maxLines;
                        int numpages = (int)Math.Ceiling(_numpages) - 1;

                        // Check page number
                        if (page > numpages)
                            page = numpages;
                        if (page < 0)
                            page = 0;

                        // Start log
                        WriteImportant("-- -- CVars (Page: {0} of {1}) -- --", page, numpages);

                        if (CVars.Count != 0)
                        {
                            // Grab enumerator
                            var e = CVars.OrderBy(x => x.Key).GetEnumerator();
                            e.MoveNext();

                            // Skip pages if necessary
                            for (int i = 0; i < maxLines * page; i++)
                                e.MoveNext();

                            // Log current page
                            for (int i = maxLines * page; i < maxLines * (page + 1); i++)
                            {
                                KeyValuePair<string, CVar> var = e.Current;
                                WriteLine("{0}= {1}", var.Key.PadRight(20), var.Value.value);

                                if (!e.MoveNext())
                                    break;
                            }
                        }
                        else
                            WriteLine("There are no CVars defined!");

                        WriteLine("");
                    });

                AddCommand("cmd-history", "Sets the length of the history.",
                    "cmd-history <length [current window height - 300]>",
                    delegate(string [] args)
                    {
                        if (isLinux)
                        {
                            WriteError("Command not available in Linux terminals.");
                            return;
                        }

                        if (!AllowCMDHistory)
                        {
                            WriteError("Command not allowed.");
                            return;
                        }

                        if (args.Length < 1)
                        {
                            WriteError("Wrong number of arguments");
                            return;
                        }

                        int length = Convert.ToInt32(args [0]);

                        if (length <= Console.WindowHeight || length > 300)
                        {
                            WriteError("Invalid length: must be between the current window height and 300");
                            return;
                        }
                        else
                        {
                            Console.WindowTop = 0;
                            Console.BufferHeight = length;
                            WriteInputLine();
                        }
                    });

                AddCommand("cmd-exit", "Exits DashCMD.",
                    delegate(string [] args)
                    {
                        if (!AllowCMDExit)
                        {
                            WriteError("Command not allowed.");
                            return;
                        }

                        StopListening();
                    });
                #endregion

                // Mark all commands are core.
                List<string> keys = new List<string>(commands.Keys);
                foreach (string key in keys)
                {
                    Command rcmd = commands [key];
                    if (isLinux && key == "cmd-history")
                        rcmd.hideInHelp = true;
                    rcmd.core = true;
                    commands [key] = rcmd;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        #region Initialization/Stopping
        /// <summary>
        /// Attaches the wrapper to the Console, redirecting it's input and output.
        /// </summary>
        public static void Start(bool createConsole = false)
        {
            if (!TryStarted)
                TryStart(createConsole);

            if (!ConsoleHandleExists)return;
            //throw new Exception(
            //    "Cannot start DashCMD, no console handle exists! (Use DashCMD.ConsoleHandleExists to check this.)");

            sw.NewLine = Environment.NewLine;
            Console.SetOut(sw);
            Console.Clear();
            WriteImportant("Started DashCMD v{0}", Version);
            WriteInputLine();
        }

        /// <summary>
        /// Stops the wrapper completely.
        /// </summary>
        public static void Stop()
        {
            if (!ConsoleHandleExists)return;

            StopListening();
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));

            //keyboardEvent(0x0058, 0, 0x0001, 0); // Send the final key press to end immediatly.
            stdWriteLine("Press any key to exit...");
        }

        /// <summary>
        /// Starts listening for input, blocks the calling thread until stopped.
        /// </summary>
        public static void Listen(bool async)
        {
            if (Listening)
                return;

            if (async)
            {
                consoleThread = new Thread(new ThreadStart(Listen));
                consoleThread.Name = "DashCMD Thread";
                consoleThread.IsBackground = true;
                consoleThread.Start();

                Listening = true;
            }
            else
                Listen();
        }

        /// <summary>
        /// Starts listening for input, blocks the calling thread until stopped.
        /// </summary>
        static void Listen()
        {
            if (!ConsoleHandleExists)return;

            Listening = true;

            while (Listening)
            {
                ConsoleKeyInfo keyinfo = Console.ReadKey(true);
                if (!OnMainScreen)
                {
                    if (keyinfo.Key == ConsoleKey.Escape)
                        SwitchScreen(null);
                    continue;
                }
                if (keyinfo.Key == ConsoleKey.Enter)
                {
                    string cmd = typingCommand.ToString();
                    typingCommandI = 0;
                    if (!string.IsNullOrWhiteSpace(cmd))
                        ExecuteCommand(typingCommand.ToString());
                    else
                    {
                        SupressTimestamps = true;
                        WriteStandard("");
                        SupressTimestamps = false;
                        typingCommand.Clear();
                    }
                }
                else if (keyinfo.Key == ConsoleKey.Backspace && typingCommand.Length > 0 && typingCommandI > 0)
                {
                    typingCommand.Remove(typingCommandI - 1, 1);
                    typingCommandI--;
                    WriteInputLine();
                }
                else if (keyinfo.Key == ConsoleKey.Delete && typingCommand.Length > 0 && typingCommandI < typingCommand.Length)
                {
                    typingCommand.Remove(typingCommandI, 1);
                    WriteInputLine();
                }
                else if (keyinfo.Key == ConsoleKey.UpArrow) // Cycle Up Last Commands
                {
                    if (saveCommandI != -1 && (lastCommands.Count == 0 || typingCommand.ToString() != lastCommands [saveCommandI]))
                        saveCommandI = Math.Max(-1, saveCommandI - 1);

                    if (saveCommandI + 1 < lastCommands.Count)
                        saveCommandI++;

                    if (saveCommandI >= 0 && saveCommandI < lastCommands.Count)
                    {
                        //stdWrite(String.Format("{0}{1}", new string('\b', typingCommand.Length), lastCommands[saveCommandI]));
                        typingCommand.Clear();
                        typingCommand.Append(lastCommands [saveCommandI]);
                        typingCommandI = typingCommand.Length;
                        WriteInputLine();
                    }
                }
                else if (keyinfo.Key == ConsoleKey.DownArrow) // Cycle Down Last Commands
                {
                    if (saveCommandI - 1 >= 0)
                        saveCommandI--;

                    if (saveCommandI >= 0 && saveCommandI < lastCommands.Count)
                    {
                        //stdWrite(String.Format("{0}{1}", new string('\b', typingCommand.Length), lastCommands[saveCommandI]));
                        typingCommand.Clear();
                        typingCommand.Append(lastCommands [saveCommandI]);
                        typingCommandI = typingCommand.Length;
                        WriteInputLine();
                    }

                    if (saveCommandI == 0)
                    {
                        typingCommand.Clear();
                        typingCommandI = 0;
                        WriteInputLine();
                    }
                }
                else if (keyinfo.Key == ConsoleKey.LeftArrow)
                {
                    if (typingCommandI > 0)
                    {
                        typingCommandI--;
                        SetCursorPos();
                    }
                }
                else if (keyinfo.Key == ConsoleKey.RightArrow)
                {
                    if (typingCommandI < typingCommand.Length)
                    {
                        typingCommandI++;
                        SetCursorPos();
                    }
                }
                else if (keyinfo.Key == ConsoleKey.Tab)
                {
                    SupressTimestamps = true;

                    string currentCommand = typingCommand.ToString();
                    if (!String.IsNullOrWhiteSpace(currentCommand))
                    {
                        var cvarSuggestions = CVars.Keys.Where(s => s.StartsWith(currentCommand));
                        var cmdSuggestions = commands.Keys.Where(s => s.StartsWith(currentCommand));
                        var suggestions = cvarSuggestions.Concat(cmdSuggestions);

                        if (suggestions.Count() == 1)
                        {
                            string suggestion = suggestions.First();

                            stdWrite(String.Format("{0}{1}", new string('\b', typingCommand.Length), suggestion));
                            typingCommand.Clear();
                            typingCommand.Append(suggestion);
                            typingCommandI = typingCommand.Length;
                        }
                        else if (suggestions.Count() != 0)
                        {
                            IEnumerator<string> e = suggestions.GetEnumerator();
                            int count = suggestions.Count();
                            StringBuilder sb = new StringBuilder();
                            for (int i = 0; i < count;)
                            {
                                sb.Clear();

                                for (int k = 0; k < 5 && i < count; k++, i++)
                                {
                                    if (e.MoveNext())
                                    {
                                        sb.Append(e.Current);
                                        sb.Append("\t");
                                    }
                                }

                                WriteStandard(sb.ToString());
                            }

                            WriteStandard("");
                        }
                    }

                    SupressTimestamps = false;
                }
                else if (!char.IsControl(keyinfo.KeyChar))
                {
                    typingCommand.Insert(typingCommandI++, keyinfo.KeyChar);
                    if (typingCommandI == typingCommand.Length)
                        stdWrite(keyinfo.KeyChar.ToString());
                    else
                        // Only rewrite input line if necessary
                        WriteInputLine();
                }
            }
        }

        /// <summary>
        /// Stops listening for input.
        /// </summary>
        public static void StopListening()
        {
            if (!ConsoleHandleExists)return;
            Listening = false;
        }
        #endregion

        #region Core Console Handling
        static void outCs_OnConsoleWrite(byte [] buffer, int offset, int count)
        {
            if (isLinux && top == 0)
                top = 1;

            string strnl = Console.OutputEncoding.GetString(buffer, offset, count);
            string str = Regex.Replace(strnl, "\n|\r", "");

            logLines.Add(new CLine(strnl, Console.ForegroundColor, Console.BackgroundColor));
            if (logLines.Count > MaxLogLines)
                logLines.RemoveAt(0);

            if (OnMainScreen)
            {
                int newlines = 0;

                for (int i = offset; i < count; i++)
                    if (i < count - (Environment.NewLine.Length - 1))
                        if (Console.OutputEncoding.GetString(buffer, i, Environment.NewLine.Length) == Environment.NewLine)
                        {
                            newlines++;
                            i += (Environment.NewLine.Length - 1);
                        }
                else if (Console.OutputEncoding.GetString(buffer, i, 1) == "\n")
                    newlines++;

                int t = SafePosSet(0, top++);

                if (isLinux && t == top - 1)
                    ClearLine(' ', false);
                else if (!isLinux && t == top - 1)
                    ClearLine(' ', false);
                Console.OpenStandardOutput().Write(buffer, offset, count);

                newlines += (int)Math.Floor(str.Length / (double)Console.BufferWidth);
                top += newlines - 1;
                WriteInputLine();
            }
            else
                top++;
        }

        static void WriteLogScreen()
        {
            if (!ConsoleHandleExists)return;
            Console.Clear();
            foreach (CLine line in logLines)
            {
                Console.ForegroundColor = line.fcolor;
                Console.BackgroundColor = line.bcolor;
                stdWrite(line.text);
            }

            if (!isLinux)
            {
                top = Console.CursorTop;
                WriteInputLine();
            }
        }

        internal static void ClearLine(char clearChar = ' ', bool isLog = true)
        {
            if (!ConsoleHandleExists)return;
            if (isLog)
                logLines.RemoveAt(Console.CursorTop);

            Console.SetCursorPosition(0, Console.CursorTop);
            stdWrite(new string(clearChar, Console.BufferWidth));

            if (Console.CursorTop > 0 && !isLinux)
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            else if (Console.CursorTop > 0 && isLinux)
                Console.SetCursorPosition(0, Console.CursorTop - 1);
        }

        internal static void stdWrite(string text)
        {
            if (!ConsoleHandleExists)return;
            byte [] bytes = Console.OutputEncoding.GetBytes(text);
            Console.OpenStandardOutput().Write(bytes, 0, bytes.Length);
        }

        internal static void stdWriteLine(string text)
        {
            if (!ConsoleHandleExists)return;
            byte [] bytes = Console.OutputEncoding.GetBytes(String.Format("{0}{1}", text, Environment.NewLine));
            Console.OpenStandardOutput().Write(bytes, 0, bytes.Length);
        }

        internal static int SafePosSet(int x, int y)
        {
            if ((!isLinux && y >= (Console.BufferHeight - 2)) || (isLinux && y >= (Console.BufferHeight - 1)))
            {
                if (!isLinux)
                    y = (Console.BufferHeight - 2);
                else
                    y = (Console.BufferHeight - 1);
            }

            Console.SetCursorPosition(x, y);

            return y;
        }

        internal static void SafePosBottomSet(int x, int y)
        {
            if (!ConsoleHandleExists)return;
            if (y >= Console.BufferHeight)
            {
                y = (Console.BufferHeight - 1);
            }

            Console.SetCursorPosition(x, y);
        }

        static void WriteInputLine()
        {
            if (!ConsoleHandleExists)return;
            Console.ForegroundColor = ConsoleColor.White;
            int t = SafePosSet(0, top);
            bool shifted = false;

            if (t != top)
            {
                SafePosBottomSet(0, top + 1);
                top = t;
                shifted = true;
            }
            else
                SafePosBottomSet(0, top);

            if (!isLinux)
                ClearLine(' ', false);
            else if (isLinux && shifted)
            {
                ClearLine(' ', false);
                SafePosBottomSet(0, top - 1);
            }
            else
            {
                ClearLine(' ', false);
                SafePosBottomSet(0, top);
            }

            if (isLinux && shifted)
                stdWriteLine("\n");

            promptAnchor = Console.CursorTop;
            stdWrite(String.Format("#> {0}", typingCommand));
            SetCursorPos();
        }

        static void SetCursorPos()
        {
            int promptLength = 3 + typingCommand.Length;
            int bufferWidth = Console.BufferWidth;

            int x = (typingCommandI + 3) % bufferWidth;
            int y = (typingCommandI + 3) / bufferWidth;

            SafePosSet(x, promptAnchor + y);
        }
        #endregion
    }
}