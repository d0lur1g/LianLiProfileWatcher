// Services/NativeMethods.cs
using System;
using System.Runtime.InteropServices;

namespace LianLiProfileWatcher.Services
{
    /// <summary>
    /// Déclarations des fonctions Win32 utilisées pour récupérer la fenêtre et le PID.
    /// </summary>
    internal static class NativeMethods
    {
        /// <summary>
        /// Renvoie le handle de la fenêtre actuellement au premier plan.
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        /// <summary>
        /// Récupère l’ID de processus associé à une fenêtre.
        /// </summary>
        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(
            IntPtr hWnd,
            out uint lpdwProcessId);
    }
}
