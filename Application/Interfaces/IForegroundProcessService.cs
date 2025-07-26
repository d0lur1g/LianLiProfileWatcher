namespace LianLiProfileWatcher.Application.Interfaces
{
    /// <summary>
    /// Fournit le nom du processus actuellement en premier plan.
    /// </summary>
    public interface IForegroundProcessService
    {
        string GetForegroundProcessName();
    }
}
