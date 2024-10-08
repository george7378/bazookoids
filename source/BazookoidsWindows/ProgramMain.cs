﻿using BazookoidsCore;
using System;

namespace BazookoidsWindows
{
    /// <summary>
    /// The main class.
    /// </summary>
    public static class ProgramMain
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (var game = new BazookoidsGame())
            {
                game.Run();
            }
        }
    }
}
