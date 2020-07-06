namespace Dash.CMD
{
    public static partial class DashCMD
    {
        /// <summary>
        /// Is it listening for input?
        /// </summary>
        public static bool Listening { get; private set; }
        /// <summary>
        /// Is it started?
        /// </summary>
        public static bool Started { get; private set; }
        /// <summary>
        /// Is it drawing the main screen?
        /// </summary>
        public static bool OnMainScreen { get; private set; }
        /// <summary>
        /// The active screen, null if main.
        /// </summary>
        public static DashCMDScreen ActiveScreen { get; private set; }
        /// <summary>
        /// Is the cmd-history command allowed?
        /// </summary>
        public static bool AllowCMDHistory
        {
            get { return allowCMDHistory; }
            set
            {
                string cmd = "cmd-history";
                if (commands.ContainsKey(cmd))
                {
                    Command rcmd = commands [cmd];
                    rcmd.hideInHelp = !value;
                    commands [cmd] = rcmd;
                }

                allowCMDHistory = value;
            }
        }
        private static bool allowCMDHistory = true;

        /// <summary>
        /// Is the cmd-exit command allowed?
        /// </summary>
        public static bool AllowCMDExit
        {
            get { return allowCMDExit; }
            set
            {
                string cmd = "cmd-exit";
                if (commands.ContainsKey(cmd))
                {
                    Command rcmd = commands [cmd];
                    rcmd.hideInHelp = !value;
                    commands[cmd] = rcmd;
                }

                allowCMDExit = value;
            }
        }
        private static bool allowCMDExit = true;
    }
}