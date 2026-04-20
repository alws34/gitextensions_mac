using Avalonia.Media.Imaging;

namespace GitUI.Avalonia.RevisionGrid.Graph;

internal sealed class GraphCache : IDisposable
{
    private readonly Dictionary<int, RenderTargetBitmap> _cache = new();

    public bool TryGet(int rowIndex, out RenderTargetBitmap? bitmap)
        => _cache.TryGetValue(rowIndex, out bitmap);

    public void Store(int rowIndex, RenderTargetBitmap bitmap)
    {
        if (_cache.TryGetValue(rowIndex, out var old))
        {
            old.Dispose();
        }

        _cache[rowIndex] = bitmap;
    }

    public void Invalidate() => _cache.Clear();

    public void Dispose()
    {
        foreach (var bmp in _cache.Values)
        {
            bmp.Dispose();
        }

        _cache.Clear();
    }
}
