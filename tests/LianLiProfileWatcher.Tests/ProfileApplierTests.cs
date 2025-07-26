using System;
using System.IO;
using Moq;
using Xunit;
using LianLiProfileWatcher.Infrastructure.Appliers;
using LianLiProfileWatcher.Application.Interfaces;
using LianLiProfileWatcher.Models;
using Microsoft.Extensions.Logging;

public class ProfileApplierTests : IDisposable
{
    private readonly string _base;
    private readonly string _dest;
    private readonly AppProfileConfig _cfg;
    private readonly ProfileApplier _applier;

    public ProfileApplierTests()
    {
        _base = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _dest = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(Path.Combine(_base, "device"));
        Directory.CreateDirectory(Path.Combine(_base, "profile"));
        File.WriteAllText(Path.Combine(_base, "device", "a.txt"), "foo");
        File.WriteAllText(Path.Combine(_base, "profile", "b.txt"), "bar");

        _cfg = new AppProfileConfig
        { BaseFolder = _base, Destination = _dest, Default = "", Profiles = new() };
        var cfgMock = new Mock<IConfigurationService>();
        cfgMock.Setup(s => s.Config).Returns(_cfg);

        _applier = new ProfileApplier(cfgMock.Object,
                         new Mock<ILogger<ProfileApplier>>().Object);
    }

    [Fact]
    public void Apply_CopiesDeviceAndProfile()
    {
        _applier.Apply("");  // on ignore profileName ici
        // Vérifier que les dossiers existent et contiennent bien les fichiers
        Assert.True(File.Exists(Path.Combine(_dest, "device", "a.txt")));
        Assert.True(File.Exists(Path.Combine(_dest, "profile", "b.txt")));
    }

    public void Dispose()
    {
        Directory.Delete(_base, recursive: true);
        Directory.Delete(_dest, recursive: true);
    }
}
