// Services/ForegroundProcessService.cs
using System.Diagnostics;
using LianLiProfileWatcher.Application.Interfaces;

namespace LianLiProfileWatcher.Services
{
    public class ForegroundProcessService : IForegroundProcessService
    {
        public string GetForegroundProcessName()
        {
            var hWnd = NativeMethods.GetForegroundWindow();
            NativeMethods.GetWindowThreadProcessId(hWnd, out uint pid);
            return Process.GetProcessById((int)pid)
                          .ProcessName
                          .ToLowerInvariant();
        }
    }
}
