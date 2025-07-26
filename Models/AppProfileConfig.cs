using System.Collections.Generic;

namespace LianLiProfileWatcher.Models
{
    public class AppProfileConfig
    {
        public string BaseFolder { get; set; } = "";
        public string Destination { get; set; } = "";
        public string ScriptPath { get; set; } = "";
        public string Default { get; set; } = "";
        public Dictionary<string, string> Profiles { get; set; } = new();
    }
}
