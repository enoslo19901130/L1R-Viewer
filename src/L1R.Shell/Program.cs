using System.Text;
using Eto;
using Eto.Forms;
using L1R.Shared;

namespace L1R.Shell;

static class Program
{
    public static bool EnableEdit { get; private set; }

    [STAThread]
    static void Main(string[] args)
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var list = args.ToList();
        if (list.RemoveAll(a => a.Equals("--enable-edit", StringComparison.OrdinalIgnoreCase)) > 0)
            EnableEdit = true;

        // Optional: client path as first non-flag arg
        string? clientArg = list.FirstOrDefault(a => !a.StartsWith('-') && Directory.Exists(a));
        if (!string.IsNullOrEmpty(clientArg))
        {
            var settings = AppSettings.Load();
            var v = ClientPathValidator.Validate(clientArg);
            if (v.Ok && !string.IsNullOrEmpty(v.Path))
            {
                settings.RememberClient(v.Path);
                settings.Save();
            }
        }

        // Settings may request edit mode (dangerous) — CLI flag wins if both set
        if (!EnableEdit)
        {
            var s = AppSettings.Load();
            EnableEdit = s.Ui.EnableEdit;
        }

        new Application(Platform.Detect).Run(new MainForm());
    }
}
