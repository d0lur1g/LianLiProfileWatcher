using System;
using System.IO;
using Xunit;
using LianLiProfileWatcher.Services;
using LianLiProfileWatcher.Models;

public class ConfigurationServiceTests
{
    private const string TempJson = @"{
      ""baseFolder"": ""C:\\Base"",
      ""destination"": ""D:\\Dest"",
      ""scriptPath"": ""E:\\Script.ps1"",
      ""default"": ""profile-foo"",
      ""profiles"": { ""app"": ""profile-foo"" }
    }";

    [Fact]
    public void Load_ValidJson_ReturnsExpectedConfig()
    {
        // Arrange : écrire un fichier temporaire
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, TempJson);

        // Act
        var svc = new ConfigurationService(tmp);
        var cfg = svc.Config;

        // Assert
        Assert.Equal("C:\\Base", cfg.BaseFolder);
        Assert.Equal("D:\\Dest", cfg.Destination);
        Assert.Equal("E:\\Script.ps1", cfg.ScriptPath);
        Assert.Equal("profile-foo", cfg.Default);
        Assert.Single(cfg.Profiles);
        Assert.Equal("profile-foo", cfg.Profiles["app"]);

        // Cleanup
        File.Delete(tmp);
    }

    [Fact]
    public void Constructor_FileNotFound_Throws()
    {
        // Arrange / Act / Assert
        Assert.Throws<FileNotFoundException>(() => new ConfigurationService("no-such.json"));
    }
}
