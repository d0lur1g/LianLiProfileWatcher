using System.Collections.Generic;

namespace LianLiProfileWatcher.Models
{
    public class AppProfileConfig
    {
        public string BaseFolder { get; set; } = "";
        public string Destination { get; set; } = "";
        public string ScriptPath { get; set; } = "";
        public string Default { get; set; } = "";
        public string ServiceName { get; set; } = "LConnectService";
        public string WatcherServiceName { get; set; } = "LConnectServiceWatcher";
        public Dictionary<string, string> Profiles { get; set; } = new();
    }
}
