namespace L1R.Shared;

/// <summary>
/// Builds consistent 錯誤/原因/建議 messages for CLI stderr and GUI.
/// </summary>
public static class OperatorMessage
{
    public static string Format(string error, string reason, string suggestion)
        => string.Join(Environment.NewLine, new[]
        {
            $"錯誤：{error}",
            $"原因：{reason}",
            $"建議：{suggestion}"
        });
}
