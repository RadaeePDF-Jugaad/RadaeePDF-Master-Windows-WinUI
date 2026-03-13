using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using RadaeeWinUI.Models;
using RDUILib;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace RadaeeWinUI.Controls
{
    public sealed partial class AnnotationCanvas : UserControl
    {
        private Point _startPoint;
        private bool _isDrawing;
        private AnnotationType _currentTool = AnnotationType.None;
        private List<Point> _inkPoints = new();

        public event EventHandler<AnnotationCreatedEventArgs>? AnnotationCreated;

        public AnnotationType CurrentAnnotationType
        {
            get => _currentTool;
            set => SetAnnotationTool(value);
        }

        public Windows.UI.Color StrokeColor { get; set; }
        public Windows.UI.Color FillColor { get; set; }
        public float StrokeWidth { get; set; }

        public AnnotationCanvas()
        {
            this.InitializeComponent();
        }

        public void SetAnnotationTool(AnnotationType tool)
        {
            _currentTool = tool;
            _isDrawing = false;
            SelectionRectangle.Visibility = Visibility.Collapsed;
            InkPath.Visibility = Visibility.Collapsed;
            _inkPoints.Clear();
        }

        public void SetSize(double width, double height)
        {
            MainCanvas.Width = width;
            MainCanvas.Height = height;
        }

        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (_currentTool == AnnotationType.None)
                return;

            _startPoint = e.GetCurrentPoint(MainCanvas).Position;
            _isDrawing = true;

            if (_currentTool == AnnotationType.Ink)
            {
                _inkPoints.Clear();
                _inkPoints.Add(_startPoint);
                InkPath.Points.Clear();
                InkPath.Points.Add(_startPoint);
                InkPath.Visibility = Visibility.Visible;
            }
            else if (IsShapeTool(_currentTool))
            {
                SelectionRectangle.Visibility = Visibility.Visible;
                Canvas.SetLeft(SelectionRectangle, _startPoint.X);
                Canvas.SetTop(SelectionRectangle, _startPoint.Y);
                SelectionRectangle.Width = 0;
                SelectionRectangle.Height = 0;
            }

            MainCanvas.CapturePointer(e.Pointer);
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDrawing)
                return;

            Point currentPoint = e.GetCurrentPoint(MainCanvas).Position;

            if (_currentTool == AnnotationType.Ink)
            {
                _inkPoints.Add(currentPoint);
                InkPath.Points.Add(currentPoint);
            }
            else if (IsShapeTool(_currentTool))
            {
                double left = Math.Min(_startPoint.X, currentPoint.X);
                double top = Math.Min(_startPoint.Y, currentPoint.Y);
                double width = Math.Abs(currentPoint.X - _startPoint.X);
                double height = Math.Abs(currentPoint.Y - _startPoint.Y);

                Canvas.SetLeft(SelectionRectangle, left);
                Canvas.SetTop(SelectionRectangle, top);
                SelectionRectangle.Width = width;
                SelectionRectangle.Height = height;
            }
        }

        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDrawing)
                return;

            _isDrawing = false;
            MainCanvas.ReleasePointerCapture(e.Pointer);

            Point endPoint = e.GetCurrentPoint(MainCanvas).Position;

            if (_currentTool == AnnotationType.TextNote)
            {
                OnAnnotationCreated(new AnnotationCreatedEventArgs
                {
                    Type = AnnotationType.TextNote,
                    X = (float)_startPoint.X,
                    Y = (float)_startPoint.Y,
                    Width = 20,
                    Height = 20
                });
            }
            else if (_currentTool == AnnotationType.Ink && _inkPoints.Count > 1)
            {
                OnAnnotationCreated(new AnnotationCreatedEventArgs
                {
                    Type = AnnotationType.Ink,
                    InkPoints = new List<Point>(_inkPoints)
                });
                InkPath.Visibility = Visibility.Collapsed;
                InkPath.Points.Clear();
            }
            else if (IsShapeTool(_currentTool))
            {
                double left = Math.Min(_startPoint.X, endPoint.X);
                double top = Math.Min(_startPoint.Y, endPoint.Y);
                double width = Math.Abs(endPoint.X - _startPoint.X);
                double height = Math.Abs(endPoint.Y - _startPoint.Y);

                if (width > 5 && height > 5)
                {
                    OnAnnotationCreated(new AnnotationCreatedEventArgs
                    {
                        Type = _currentTool,
                        X = (float)left,
                        Y = (float)top,
                        Width = (float)width,
                        Height = (float)height
                    });
                }

                SelectionRectangle.Visibility = Visibility.Collapsed;
            }
        }

        private bool IsShapeTool(AnnotationType tool)
        {
            return tool == AnnotationType.Rectangle || 
                   tool == AnnotationType.Ellipse || 
                   tool == AnnotationType.Line;
        }

        private void OnAnnotationCreated(AnnotationCreatedEventArgs args)
        {
            AnnotationCreated?.Invoke(this, args);
        }
    }

    public class AnnotationCreatedEventArgs : EventArgs
    {
        public AnnotationType Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public List<Point>? InkPoints { get; set; }
    }
}
