using System;
using System.Collections.Generic;
using System.Text;

namespace Dash.CMD
{
    public static partial class DashCMD
    {
        /// <summary>
        /// Adds a command with this cmd.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="callback">Method to call when the command is used.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void AddCommand(string command, DashCMDCommand callback)
        {
            if (commands.ContainsKey(command))
                throw new ArgumentException("Command already registered!");

            commands.Add(command, new Command(command,
                "",
                command,
                callback,
                true));
        }

        /// <summary>
        /// Adds a command with this cmd.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="help">The help message to show when the help command is used.</param>
        /// <param name="callback">Method to call when the command is used.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void AddCommand(string command, string help, DashCMDCommand callback)
        {
            if (commands.ContainsKey(command))
                throw new ArgumentException("Command already registered!");

            commands.Add(command, new Command(command,
                !String.IsNullOrWhiteSpace(help) ? help : "",
                command,
                callback,
                String.IsNullOrWhiteSpace(help)));
        }

        /// <summary>
        /// Adds a command with this cmd.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="help">The help message to show when the help command is used.</param>
        /// <param name="syntax">The syntax message to show when the --syntax argument 
        /// is used with this command.</param>
        /// <param name="callback">Method to call when the command is used.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void AddCommand(string command, string help, string syntax, DashCMDCommand callback)
        {
            if (commands.ContainsKey(command))
                throw new ArgumentException(String.Format("Command already registered: {0}", command));

            commands.Add(command, new Command(command,
                !String.IsNullOrWhiteSpace(help) ? help : "",
                !String.IsNullOrWhiteSpace(syntax) ? syntax : command,
                callback,
                String.IsNullOrWhiteSpace(help)));
        }

        /// <summary>
        /// Removes a command.
        /// </summary>
        /// <param name="command">The command to unregister.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public static void RemoveCommand(string command)
        {
            if (commands.ContainsKey(command))
                if (!commands[command].core)
                    commands.Remove(command);
                else
                    throw new ArgumentException("Cannot unregister a core command!");
            else
                throw new ArgumentException(String.Format("Command does not exist: {0}", command));

        }

        /// <summary>
        /// Gets whether or not the specified command is defined.
        /// </summary>
        /// <param name="command">The name of the command</param>
        public static bool IsCommandDefined(string command)
        {
            return commands.ContainsKey(command);
        }

        /// <summary>
        /// Combines the arguments in the list to one string seperate by spaces.
        /// </summary>
        /// <param name="args">The Arguments to combine.</param>
        /// <returns>The combined string.</returns>
        public static string CombineArgs(string[] args)
        {
            return CombineArgs(args, ' ', 0, args.Length);
        }

        /// <summary>
        /// Combines the arguments in the list to one string.
        /// </summary>
        /// <param name="args">The Arguments to combine.</param>
        /// <param name="seperateChar">Character to seperate them with.</param>
        /// <param name="start">Starting position to start combing.</param>
        /// <param name="count">How many to combine.</param>
        /// <returns>The combined string.</returns>
        public static string CombineArgs(string[] args, char seperateChar, int start, int count)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = start; i < count; i++)
            {
                sb.Append(args[i]);
                if (i + 1 < args.Length)
                    sb.Append(seperateChar);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Executes a command.
        /// </summary>
        /// <param name="command">The command (with parameters) to execute.</param>
        public static void ExecuteCommand(string command)
        {
            saveCommandI = -1;

            if (lastCommands.Count == 0 || lastCommands[0] != command)
            {
                lastCommands.Insert(0, command);

                if (lastCommands.Count > MaxSavedCommands)
                    lastCommands.RemoveAt(lastCommands.Count - 1);
            }

            List<string> cmds = new List<string>();
            StringBuilder sb = new StringBuilder();
            bool start = false;
            for (int i = 0; i < command.Length; i++)
            {
                string c = command.Substring(i, 1);
                if (!String.IsNullOrWhiteSpace(c))
                    start = true;

                if (c == ";")
                {
                    cmds.Add(sb.ToString());
                    sb.Clear();
                    start = false;
                }
                else if (start)
                    sb.Append(c);
            }

            cmds.Add(sb.ToString());

            foreach (string cmd in cmds)
                InternalExecuteCommand(cmd);
        }

        static void InternalExecuteCommand(string command)
        {
            // Supress timestamps when in command execution context
            SupressTimestamps = true;

            typingCommand.Clear();
            Command rcmd;

            // Get the actual command with its arguments
            string cmd;
            List<string> args = ParseCommand(command, out cmd);

            // Find the command.
            if (commands.TryGetValue(cmd, out rcmd))
            {
                if (args.Count >= 1 && (args[0].ToLower() == "--syntax" || args[0].ToLower() == "--?" || args[0].ToLower() == "/?"))
                    ShowSyntax(rcmd);
                else
                    try
                    {
                        rcmd.callback(args.ToArray());
                        WriteInputLine();
                    }
                    catch (Exception e) { WriteError(e.ToString()); }
            }
            else
            {
                if (args.Count > 0)
                    if (TryModVar(cmd, args[0]))
                        WriteStandard("{0} -> {1}", cmd, args[0]);
                    else
                        WriteError("Failed to change {0} to {1}. (Wrong datatype?)", cmd, args[0]);
                else if (args.Count == 0 && CVars.ContainsKey(cmd))
                    WriteStandard("{0} = {1}", cmd, CVars[cmd].value);
                else
                    WriteError("Command not defined: {0}", cmd);
            }

            SupressTimestamps = false;
        }

        static bool TryModVar(string cvar, object val)
        {
            if (CVars.ContainsKey(cvar))
            {
                try
                {
                    SetCVar(cvar, val);
                    return true;
                }
                catch (Exception)
                {
                    //LogError(e);
                    return false;
                }
            }
            else
                return false;
        }

        static List<string> ParseCommand(string cmd, out string trimmedCmd)
        {
            List<string> args = new List<string>();
            int fs = cmd.IndexOf(" ");

            if (fs == -1)
                trimmedCmd = cmd; // No args exist, so just set the trimmed cmd.
            else
            {
                // Start searching for all of the arguments.
                trimmedCmd = cmd.Substring(0, fs);
                string pArgs = cmd.Substring(fs + 1);
                int nextSpace = -1;

                while ((nextSpace = pArgs.IndexOf(" ")) != -1)
                {
                    string arg = pArgs.Substring(0, nextSpace);

                    // Strip out "'s if used at the beginning and end
                    //if (arg.Substring(0, 1) == "\"" && arg.Substring(arg.Length - 1, 1) == "\"")
                    //    arg = arg.Remove(0, 1).Remove(arg.Length - 2, 1);

                    args.Add(arg);
                    pArgs = pArgs.Substring(nextSpace + 1);
                }

                // Strip out the final arg's "'s if used at the beginning and end
                // if (pArgs.Substring(0, 1) == "\"" && pArgs.Substring(pArgs.Length - 1, 1) == "\"")
                //    pArgs = pArgs.Remove(0, 1).Remove(pArgs.Length - 2, 1);

                args.Add(pArgs);
            }

            return args;
        }

        public static void ShowSyntax(string commandName)
        {
            Command cmd;
            if (commands.TryGetValue(commandName, out cmd))
                ShowSyntax(cmd);
            else
                WriteError("Failed to display syntax. Command '{0}' is not defined!", commandName);
        }

        static void ShowSyntax(Command rcmd)
        {
            WriteStandard(String.Format("Syntax: {0}", rcmd.syntax));
        }
    }
}