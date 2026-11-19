using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;

namespace Island.App.Services;

internal sealed record MediaArtwork(BitmapSource Image, Color Dominant)
{
    public static async Task<MediaArtwork?> ReadAsync(IRandomAccessStreamReference? reference, CancellationToken token)
    {
        if (reference is null) return null;
        using var random = await reference.OpenReadAsync().AsTask(token);
        if (random.Size is 0 or > 8 * 1024 * 1024) return null;
        using var input = random.AsStreamForRead();
        using var bytes = new MemoryStream((int)random.Size);
        await input.CopyToAsync(bytes, token);
        return await Task.Run(() =>
        {
            bytes.Position = 0;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.DecodePixelWidth = 192;
            bitmap.StreamSource = bytes;
            bitmap.EndInit();
            bitmap.Freeze();
            var small = new TransformedBitmap(bitmap, new ScaleTransform(24d / bitmap.PixelWidth, 24d / bitmap.PixelHeight));
            var pixels = new FormatConvertedBitmap(small, PixelFormats.Bgra32, null, 0);
            var data = new byte[pixels.PixelWidth * pixels.PixelHeight * 4];
            pixels.CopyPixels(data, pixels.PixelWidth * 4, 0);
            var buckets = new (int Count, int R, int G, int B)[512];
            for (int i = 0; i < data.Length; i += 4)
            {
                if (data[i + 3] < 128) continue;
                int r = data[i + 2], g = data[i + 1], b = data[i];
                ref var bucket = ref buckets[(r >> 5) * 64 + (g >> 5) * 8 + (b >> 5)];
                bucket.Count++;
                bucket.R += r; bucket.G += g; bucket.B += b;
            }
            var winner = buckets.MaxBy(x => x.Count);
            var color = winner.Count == 0 ? Colors.Transparent : Color.FromRgb(
                (byte)(winner.R / winner.Count), (byte)(winner.G / winner.Count), (byte)(winner.B / winner.Count));
            return new MediaArtwork(bitmap, color);
        }, token);
    }
}
