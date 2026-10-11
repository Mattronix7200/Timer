using System;
using System.Runtime.Versioning;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;

namespace TimerSys.Wpf
{
    [SupportedOSPlatform("windows10.0.19041.0")]
    internal sealed class PdfBook : IDisposable
    {
        private PdfDocument? _document;

        public static bool Available => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041);

        public int PageCount => (int)(_document?.PageCount ?? 0);

        public bool IsOpen => _document != null;

        public async Task<bool> OpenAsync(string path)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                _document = await PdfDocument.LoadFromFileAsync(file);
                return _document.PageCount > 0;
            }
            catch
            {
                _document = null;
                return false;
            }
        }

        public async Task<BitmapSource?> RenderAsync(int index, double width)
        {
            if (_document == null || index < 0 || index >= PageCount) return null;

            try
            {
                using var page = _document.GetPage((uint)index);
                using var stream = new InMemoryRandomAccessStream();

                await page.RenderToStreamAsync(stream, new PdfPageRenderOptions
                {
                    DestinationWidth = (uint)Math.Max(200, Math.Min(4000, width))
                });

                byte[] bytes = await ReadAllAsync(stream);

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public double AspectRatio(int index)
        {
            if (_document == null || index < 0 || index >= PageCount) return 1.414;
            using var page = _document.GetPage((uint)index);
            var size = page.Size;
            return size.Width > 0 ? size.Height / size.Width : 1.414;
        }

        private static async Task<byte[]> ReadAllAsync(IRandomAccessStream stream)
        {
            uint size = (uint)stream.Size;
            using var reader = new DataReader(stream.GetInputStreamAt(0));
            await reader.LoadAsync(size);
            var bytes = new byte[size];
            reader.ReadBytes(bytes);
            return bytes;
        }

        public void Dispose()
        {
            _document = null;
        }
    }
}
