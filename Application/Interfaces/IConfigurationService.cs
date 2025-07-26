using LianLiProfileWatcher.Models;

namespace LianLiProfileWatcher.Application.Interfaces
{
    public interface IConfigurationService
    {
        AppProfileConfig Config { get; }
    }
}
