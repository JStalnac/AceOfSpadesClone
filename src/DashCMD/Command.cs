namespace Dash.CMD
{
        internal struct Command
        {
            public string command;
            public string help;
            public string syntax;
            public bool hideInHelp;
            public bool core;
            public DashCMDCommand callback;

            internal Command(string command, string help, string syntax, DashCMDCommand callback, bool hideInHelp)
            {
                this.command = command;
                this.help = help;
                this.hideInHelp = hideInHelp;
                this.callback = callback;
                this.syntax = syntax;
                this.core = false;
            }
        }
}