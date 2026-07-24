using L1R.Shared;
using Xunit;

namespace L1R.Shared.Tests;

public class AppSettingsTests
{
    [Fact]
    public void SaveLoad_RoundTripsLastClientAndRecentCap()
    {
        string path = Path.Combine(Path.GetTempPath(), "l1r-settings-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            var settings = AppSettings.CreateDefault();
            for (int i = 0; i < 12; i++)
            {
                string client = Path.Combine(Path.GetTempPath(), "client-" + i);
                Directory.CreateDirectory(client);
                settings.RememberClient(client);
            }
            Assert.Equal(AppSettings.MaxRecentClients, settings.RecentClients.Count);
            Assert.NotNull(settings.LastClientPath);

            settings.Save(path);

            var loaded = AppSettings.Load(path);
            Assert.Equal(settings.LastClientPath, loaded.LastClientPath);
            Assert.Equal(AppSettings.MaxRecentClients, loaded.RecentClients.Count);
            Assert.Equal(settings.RecentClients[0].Path, loaded.RecentClients[0].Path);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
