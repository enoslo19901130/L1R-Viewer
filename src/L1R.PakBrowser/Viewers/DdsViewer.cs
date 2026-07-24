using System;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using PakViewer.Localization;
using Pfim;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using IsImage = SixLabors.ImageSharp.Image;

namespace PakViewer.Viewers
{
    /// <summary>
    /// DDS (DirectDraw Surface) 預覽器，透過 Pfim 解碼後轉 PNG 載入。
    /// 支援 BC1/BC2/BC3/BC7 與未壓縮 RGB/BGRA 格式。
    /// </summary>
    public class DdsViewer : BaseViewer
    {
        private ImageView _imageView;

        public override string[] SupportedExtensions => new[] { ".dds" };

        public override void LoadData(byte[] data, string fileName)
        {
            _data = data;
            _fileName = fileName;

            try
            {
                byte[] pngBytes = ConvertDdsToPng(data);
                using var pngStream = new MemoryStream(pngBytes);
                _imageView = new ImageView { Image = new Bitmap(pngStream) };
                _control = _imageView;
            }
            catch (Exception ex)
            {
                _control = new Label { Text = $"{I18n.T("Error.LoadImage")}: {ex.Message}" };
            }
        }

        private static byte[] ConvertDdsToPng(byte[] dds)
        {
            using var input = new MemoryStream(dds);
            using var ddsImage = Pfimage.FromStream(input);

            // Pfim 的 stride 可能含對齊 padding，ImageSharp.LoadPixelData 需要 tight packing
            int bpp = ddsImage.BitsPerPixel / 8;
            int tightStride = ddsImage.Width * bpp;
            byte[] pixels = ddsImage.Data;
            if (ddsImage.Stride != tightStride)
            {
                pixels = new byte[tightStride * ddsImage.Height];
                for (int y = 0; y < ddsImage.Height; y++)
                {
                    Buffer.BlockCopy(ddsImage.Data, y * ddsImage.Stride,
                                     pixels, y * tightStride, tightStride);
                }
            }

            IsImage imageSharp;
            switch (ddsImage.Format)
            {
                case Pfim.ImageFormat.Rgba32:
                    imageSharp = IsImage.LoadPixelData<Bgra32>(
                        pixels, ddsImage.Width, ddsImage.Height);
                    break;
                case Pfim.ImageFormat.Rgb24:
                    imageSharp = IsImage.LoadPixelData<Bgr24>(
                        pixels, ddsImage.Width, ddsImage.Height);
                    break;
                default:
                    throw new NotSupportedException(
                        $"DDS pixel format {ddsImage.Format} 尚未支援");
            }

            using (imageSharp)
            using (var pngStream = new MemoryStream())
            {
                imageSharp.Save(pngStream, new PngEncoder());
                return pngStream.ToArray();
            }
        }

        public override void Dispose()
        {
            _imageView?.Image?.Dispose();
            _imageView = null;
            base.Dispose();
        }
    }
}
