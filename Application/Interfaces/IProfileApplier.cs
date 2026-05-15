namespace LianLiProfileWatcher.Application.Interfaces
{
    public interface IProfileApplier
    {
        /// <summary>
        /// Applique le profil donné de façon asynchrone et annulable.
        /// </summary>
        Task ApplyAsync(string profileName, CancellationToken cancellationToken = default);
    }
}