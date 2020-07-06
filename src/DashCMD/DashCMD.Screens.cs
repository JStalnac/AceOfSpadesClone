using System;

namespace Dash.CMD
{
    public static partial class DashCMD
    {
        /// <summary>
        /// Adds a screen with this CMD.
        /// </summary>
        /// <param name="screen">The screen to register.</param>
        public static void AddScreen(DashCMDScreen screen)
        {
            if (screens.ContainsValue(screen))
                throw new ArgumentException(String.Format("Screen already registered: {0}", screen.Name));
            else
                screens.Add(screen.Name, screen);

        }

        /// <summary>
        /// Removes a screen with this CMD.
        /// </summary>
        /// <param name="screen">The screen to register.</param>
        public static void RemoveScreen(DashCMDScreen screen)
        {
            if (screens.ContainsValue(screen))
                throw new ArgumentException(String.Format("Screen not registered: {0}", screen.Name));
            else
                screens.Remove(screen.Name);
        }

        static void SwitchScreen(DashCMDScreen screen)
        {
            if (!ConsoleHandleExists) return;
            if (ActiveScreen != null)
                ActiveScreen.Stop();

            if (screen != null)
            {
                OnMainScreen = false;
                Console.Clear();
                ActiveScreen = screen;
                Console.CursorVisible = false;
                screen.Start();
            }
            else
            {
                Console.CursorVisible = true;
                OnMainScreen = true;
                ActiveScreen = null;
                Console.ResetColor();

                // Try-catch is for when coming out of a screen back to
                // a lot of messages. It takes CMD so long to actually write
                // them that the lines list changes causing a collection
                // modified exception.
                try { WriteLogScreen(); }
                catch (Exception) { }
            }
        }
    }
}