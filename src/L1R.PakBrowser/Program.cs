using System;
using System.Linq;
using System.Text;
using Eto;
using Eto.Forms;

namespace PakViewer
{
    internal static class Program
    {
        /// <summary>
        /// 寫入/匯入功能開關。預設 false（唯讀瀏覽）;需 --enable-edit 才開放 import/寫回。
        /// </summary>
        public static bool EnableEdit { get; private set; } = false;

        [STAThread]
        private static void Main(string[] args)
        {
            // Register Big5, GB2312, Shift_JIS, EUC-KR etc. encoding support
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var list = args.ToList();
            if (list.Contains("--enable-edit"))
            {
                EnableEdit = true;
                list.RemoveAll(a => a == "--enable-edit");
                args = list.ToArray();
            }

            // CLI mode - not implemented in cross-platform version
            // Use the AnalyzeMTil tool or Lin.Helper.Core library directly for CLI operations

            // GUI mode - use Eto.Forms for cross-platform
            new Application(Platform.Detect).Run(new MainForm());
        }
    }
}
