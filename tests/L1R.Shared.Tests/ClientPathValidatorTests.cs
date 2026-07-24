using L1R.Shared;
using Xunit;

namespace L1R.Shared.Tests;

public class ClientPathValidatorTests
{
    [Fact]
    public void Validate_EmptyPath_FailsWithStructuredFields()
    {
        var r = ClientPathValidator.Validate("");
        Assert.False(r.Ok);
        Assert.False(string.IsNullOrEmpty(r.Error));
        Assert.False(string.IsNullOrEmpty(r.Reason));
        Assert.False(string.IsNullOrEmpty(r.Suggestion));
        var msg = r.FormatOperatorMessage();
        Assert.Contains("錯誤：", msg);
        Assert.Contains("原因：", msg);
        Assert.Contains("建議：", msg);
    }

    [Fact]
    public void Validate_MissingMapAndIdx_ReportsMissing()
    {
        string dir = Path.Combine(Path.GetTempPath(), "l1r-val-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var r = ClientPathValidator.Validate(dir);
            Assert.False(r.Ok);
            Assert.Contains("map", r.Missing);
            Assert.Contains("*.idx", r.Missing);
            Assert.Contains("map", r.FormatOperatorMessage(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Validate_MinimalFixture_WithMapAndTileIdx_Succeeds()
    {
        string dir = Path.Combine(Path.GetTempPath(), "l1r-val-" + Guid.NewGuid().ToString("N"));
        string mapDir = Path.Combine(dir, "map", "1");
        Directory.CreateDirectory(mapDir);
        File.WriteAllBytes(Path.Combine(dir, "Tile.idx"), new byte[] { 0 });
        File.WriteAllText(Path.Combine(mapDir, "dummy.s32"), "x");
        try
        {
            var r = ClientPathValidator.Validate(dir);
            Assert.True(r.Ok, r.FormatOperatorMessage());
            Assert.True(r.HasMap);
            Assert.True(r.HasTileIdx);
            Assert.True(r.HasAnyIdx);
            Assert.Equal(1, r.MapCount);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
