using RDUILib;

namespace RadaeeWinUI.Models
{
    public class RenderOptions
    {
        public float Scale { get; set; } = 1.0f;
        public RD_RENDER_MODE RenderMode { get; set; } = RD_RENDER_MODE.mode_best;
        public bool ShowAnnotations { get; set; } = true;
        public uint BackgroundColor { get; set; } = 0xFFFFFFFF;
    }
}
