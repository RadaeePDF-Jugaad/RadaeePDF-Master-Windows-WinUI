namespace RadaeeWinUI.Models
{
    public class TileRenderResult
    {
        public TileInfo Tile { get; set; }
        public byte[]? PixelData { get; set; }
        public bool Success { get; set; }

        public TileRenderResult(TileInfo tile, byte[]? pixelData, bool success)
        {
            Tile = tile;
            PixelData = pixelData;
            Success = success;
        }
    }
}
