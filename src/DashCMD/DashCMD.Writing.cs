using System;

namespace Dash.CMD
{
    public static partial class DashCMD
    {
        /// <summary>
        /// Writes a line of text.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        /// <param name="color">Color of the text.</param>
        public static void WriteLine(string msg, ConsoleColor color, params object[] args)
        {
            if (!ConsoleHandleExists) return;

            if (PrependTimestamp && !SupressTimestamps)
                msg = string.Format("[{0}] {1}", DateTime.Now.ToString(TimeCulture), msg);

            Console.ForegroundColor = color;
            if (args.Length > 0)
                Console.WriteLine(String.Format(msg, args));
            else
                Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Writes text.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        /// <param name="color">Color of the text.</param>
        public static void Write(string msg, ConsoleColor color, params object[] args)
        {
            if (!ConsoleHandleExists) return;
            Console.ForegroundColor = color;
            Console.Write(String.Format(msg, args));
            Console.ForegroundColor = ConsoleColor.White;
        }

        /// <summary>
        /// Writes a line of text.
        /// </summary>
        public static void WriteLine(object obj)
        {
            if (!ConsoleHandleExists) return;

            string msg;
            if (PrependTimestamp && !SupressTimestamps)
                msg = string.Format("[{0}] {1}", DateTime.Now.ToString(TimeCulture), obj.ToString());
            else
                msg = obj.ToString();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(msg);
        }

        /// <summary>
        /// Writes a line of text.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        public static void WriteLine(string msg, params object[] args)
        {
            if (!ConsoleHandleExists) return;

            if (PrependTimestamp && !SupressTimestamps)
                msg = string.Format("[{0}] {1}", DateTime.Now.ToString(TimeCulture), msg);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(String.Format(msg, args));
        }

        /// <summary>
        /// Writes text.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        public static void Write(string msg, params object[] args)
        {
            if (!ConsoleHandleExists) return;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(String.Format(msg, args));
        }

        /// <summary>
        /// Writes a standard white message.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        public static void WriteStandard(string msg, params object[] args)
        {
            WriteLine(String.Format(msg, args), ConsoleColor.White);
        }

        /// <summary>
        /// Writes an important message.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        public static void WriteImportant(string msg, params object[] args)
        {
            WriteLine(String.Format(msg, args), ConsoleColor.Cyan);
        }

        /// <summary>
        /// Writes a warning message.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        public static void WriteWarning(string msg, params object[] args)
        {
            WriteLine(String.Format(msg, args), ConsoleColor.Yellow);
        }

        /// <summary>
        /// Writes an error message.
        /// </summary>
        /// <param name="msg">Text to write.</param>
        public static void WriteError(string msg, params object[] args)
        {
            WriteLine(String.Format(msg, args), ConsoleColor.Red);
        }

        /// <summary>
        /// Writes an exception error message.
        /// </summary>
        /// <param name="e">Exception to write.</param>
        public static void WriteError(Exception e)
        {
            WriteLine(e.ToString(), ConsoleColor.Red);
        }
    }
}