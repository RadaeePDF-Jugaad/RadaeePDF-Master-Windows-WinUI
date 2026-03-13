using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using RadaeeWinUI.Models;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace RadaeeWinUI.Helpers
{
    public class GestureRecognizer
    {
        private readonly UIElement _target;
        private readonly Dictionary<uint, PointerPoint> _activePointers = new();
        private Point _lastSinglePointerPosition;
        private Point _lastTapPosition;
        private Point _pendingTapPosition;
        private DateTime _pressStartTime;
        private const double DoubleTapThreshold = 300; // milliseconds
        private const double LongPressThreshold = 500; // milliseconds
        private const double MovementThreshold = 10; // pixels
        private bool _isLongPressTriggered = false;
        private bool _isPendingTap = false;
        private DispatcherTimer? _longPressTimer = null;
        private DispatcherTimer? _singleTapTimer = null;

        public event EventHandler<PinchGestureEventArgs>? PinchGesture = null;
        public event EventHandler<PanGestureEventArgs>? PanGesture = null;
        public event EventHandler<SingleTapEventArgs>? SingleTap = null;
        public event EventHandler<DoubleTapEventArgs>? DoubleTap = null;
        public event EventHandler<LongPressEventArgs>? LongPress = null;

        public GestureRecognizer(UIElement target)
        {
            _target = target;
            _target.PointerPressed += OnPointerPressed;
            _target.PointerMoved += OnPointerMoved;
            _target.PointerReleased += OnPointerReleased;
            _target.PointerCanceled += OnPointerCanceled;
            _target.ManipulationMode = ManipulationModes.All;
            _target.ManipulationDelta += OnManipulationDelta;
        }

        private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_target);
            _activePointers[point.PointerId] = point;
            _pressStartTime = DateTime.Now;
            _isLongPressTriggered = false;

            if (_activePointers.Count == 1)
            {
                _lastSinglePointerPosition = point.Position;
                StartLongPressTimer(point.Position);
            }
            else
            {
                StopLongPressTimer();
            }
        }

        private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_target);
            
            if (_activePointers.ContainsKey(point.PointerId))
            {
                _activePointers[point.PointerId] = point;

                // Check if movement exceeds threshold, cancel long press
                if (_activePointers.Count == 1 && !_isLongPressTriggered)
                {
                    var distance = GetDistance(_lastSinglePointerPosition, point.Position);
                    if (distance > MovementThreshold)
                    {
                        StopLongPressTimer();
                    }
                }
            }
        }

        private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_target);
            
            if (_activePointers.ContainsKey(point.PointerId))
            {
                _activePointers.Remove(point.PointerId);
                StopLongPressTimer();

                // Detect single tap and double tap
                if (_activePointers.Count == 0 && !_isLongPressTriggered)
                {
                    var distance = GetDistance(_lastTapPosition, point.Position);

                    if (_isPendingTap && distance < MovementThreshold)
                    {
                        // Second tap detected within threshold - this is a double tap
                        StopSingleTapTimer();
                        _isPendingTap = false;
                        OnDoubleTap(point.Position);
                    }
                    else
                    {
                        // First tap or taps too far apart - start single tap timer
                        _pendingTapPosition = point.Position;
                        _lastTapPosition = point.Position;
                        _isPendingTap = true;
                        StartSingleTapTimer();
                    }
                }
            }
        }

        private void OnPointerCanceled(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(_target);
            if (_activePointers.ContainsKey(point.PointerId))
            {
                _activePointers.Remove(point.PointerId);
            }
            StopLongPressTimer();
            StopSingleTapTimer();
            _isPendingTap = false;
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // Handle pinch gesture
            if (e.Delta.Scale != 1.0f)
            {
                var args = new PinchGestureEventArgs
                {
                    Scale = e.Delta.Scale,
                    CenterX = (float)e.Position.X,
                    CenterY = (float)e.Position.Y,
                    PageIndex = -1 // Will be set by the caller
                };
                PinchGesture?.Invoke(this, args);
            }

            // Handle pan gesture (single finger drag)
            if (_activePointers.Count == 1 && (e.Delta.Translation.X != 0 || e.Delta.Translation.Y != 0))
            {
                var args = new PanGestureEventArgs
                {
                    DeltaX = (float)e.Delta.Translation.X,
                    DeltaY = (float)e.Delta.Translation.Y,
                    TotalX = (float)e.Cumulative.Translation.X,
                    TotalY = (float)e.Cumulative.Translation.Y,
                    PageIndex = -1 // Will be set by the caller
                };
                PanGesture?.Invoke(this, args);
            }
        }

        private void StartLongPressTimer(Point position)
        {
            StopLongPressTimer();
            
            _longPressTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(LongPressThreshold)
            };
            
            _longPressTimer.Tick += (s, e) =>
            {
                _isLongPressTriggered = true;
                StopSingleTapTimer();
                _isPendingTap = false;
                OnLongPress(position);
                StopLongPressTimer();
            };
            
            _longPressTimer.Start();
        }

        private void StopLongPressTimer()
        {
            if (_longPressTimer != null)
            {
                _longPressTimer.Stop();
                _longPressTimer = null;
            }
        }

        private void StartSingleTapTimer()
        {
            StopSingleTapTimer();
            
            _singleTapTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(DoubleTapThreshold)
            };
            
            _singleTapTimer.Tick += (s, e) =>
            {
                _isPendingTap = false;
                OnSingleTap(_pendingTapPosition);
                StopSingleTapTimer();
            };
            
            _singleTapTimer.Start();
        }

        private void StopSingleTapTimer()
        {
            if (_singleTapTimer != null)
            {
                _singleTapTimer.Stop();
                _singleTapTimer = null;
            }
        }

        private void OnSingleTap(Point position)
        {
            var args = new SingleTapEventArgs
            {
                X = (float)position.X,
                Y = (float)position.Y,
                PageIndex = -1 // Will be set by the caller
            };
            SingleTap?.Invoke(this, args);
        }

        private void OnDoubleTap(Point position)
        {
            var args = new DoubleTapEventArgs
            {
                X = (float)position.X,
                Y = (float)position.Y,
                PageIndex = -1 // Will be set by the caller
            };
            DoubleTap?.Invoke(this, args);
        }

        private void OnLongPress(Point position)
        {
            var args = new LongPressEventArgs
            {
                X = (float)position.X,
                Y = (float)position.Y,
                PageIndex = -1 // Will be set by the caller
            };
            LongPress?.Invoke(this, args);
        }

        private double GetDistance(Point p1, Point p2)
        {
            var dx = p1.X - p2.X;
            var dy = p1.Y - p2.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public void Dispose()
        {
            StopLongPressTimer();
            StopSingleTapTimer();
            _target.PointerPressed -= OnPointerPressed;
            _target.PointerMoved -= OnPointerMoved;
            _target.PointerReleased -= OnPointerReleased;
            _target.PointerCanceled -= OnPointerCanceled;
            _target.ManipulationDelta -= OnManipulationDelta;
            _activePointers.Clear();
        }
    }
}
