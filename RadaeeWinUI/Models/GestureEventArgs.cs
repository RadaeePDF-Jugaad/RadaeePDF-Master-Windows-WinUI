using Microsoft.UI.Input;
using System;

namespace RadaeeWinUI.Models
{
    public class PinchGestureEventArgs : EventArgs
    {
        public float Scale { get; set; }
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public int PageIndex { get; set; }
    }

    public class PanGestureEventArgs : EventArgs
    {
        public float DeltaX { get; set; }
        public float DeltaY { get; set; }
        public float TotalX { get; set; }
        public float TotalY { get; set; }
        public int PageIndex { get; set; }
    }

    public class SingleTapEventArgs : EventArgs
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int PageIndex { get; set; }
    }

    public class DoubleTapEventArgs : EventArgs
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int PageIndex { get; set; }
    }

    public class LongPressEventArgs : EventArgs
    {
        public float X { get; set; }
        public float Y { get; set; }
        public int PageIndex { get; set; }
    }
}
