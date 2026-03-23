using Microsoft.UI.Xaml.Controls;
using RDUILib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RadaeeWinUI.RadaeeUtil
{
    public sealed class PDFSel
    {
        private PDFPage _page;
        private int _pageIndex;
        private int _index1 = -1;
        private int _index2 = -1;
        private bool _selStart = false;
        private bool _swiped = false;

        public int Index1 => _index1;
        public int Index2 => _index2;
        public int PageIndex => _pageIndex;

        public PDFSel(PDFPage page, int pageIndex)
        {
            _page = page;
            _pageIndex = pageIndex;
            _page.ObjsStart();
        }

        public void Clear()
        {
            _page.Close();
            _index1 = -1;
            _index2 = -1;
            _selStart = false;
            _swiped = false;
        }

        public int[] GetRect1(float scale, float page_height)
        {
            if (_index1 < 0 || _index2 < 0 || !_selStart)
                return null;

            RDRect rect;
            if (_swiped)
                rect = _page.ObjsGetCharRect(_index2);
            else
                rect = _page.ObjsGetCharRect(_index1);
            int[] rect_draw = new int[4];
            // Use page-local coordinates (no origin offset needed)
            rect_draw[0] = (int)(rect.left * scale);
            rect_draw[1] = (int)((page_height - rect.bottom) * scale);
            rect_draw[2] = (int)(rect.right * scale);
            rect_draw[3] = (int)((page_height - rect.top) * scale);
            return rect_draw;
        }

        public int[] GetRect2(float scale, float page_height)
        {
            if (_index1 < 0 || _index2 < 0 || !_selStart)
                return null;

            RDRect rect;
            if (_swiped)
                rect = _page.ObjsGetCharRect(_index1);
            else
                rect = _page.ObjsGetCharRect(_index2);
            int[] rect_draw = new int[4];
            // Use page-local coordinates (no origin offset needed)
            rect_draw[0] = (int)(rect.left * scale);
            rect_draw[1] = (int)((page_height - rect.bottom) * scale);
            rect_draw[2] = (int)(rect.right * scale);
            rect_draw[3] = (int)((page_height - rect.top) * scale);
            return rect_draw;
        }

        public void SetSel(float x1, float y1, float x2, float y2)
        {
            if (!_selStart)
            {
                _page.ObjsStart();
                _selStart = true;
            }
            _index1 = GetCharIndex(x1, y1);
            _index2 = GetCharIndex(x2, y2);
            if (_index1 > _index2)
            {
                int tmp = _index1;
                _index1 = _index2;
                _index2 = tmp;
                _swiped = true;
            }
            else
                _swiped = false;
            _index1 = _page.ObjsAlignWord(_index1, -1);
            _index2 = _page.ObjsAlignWord(_index2, 1);
        }

        private int GetCharIndex(float x, float y)
        {
            double minD = 10000000;
            int index = -1;//if no texts on page.
            int ocnt = _page.ObjsGetCharCount();
            for (int i = 0; i < ocnt; i++)
            {
                //Note: PDFPage.ObjsGetCharUnicode() is missing in RDUILib,
                //int his case, image may be selected. Ask Yongzhi to update the
                //native library to add missing API.

                /*if (_page.ObjsGetCharUnicode(i) > 0)
                {*/
                RDRect rect = _page.ObjsGetCharRect(i);
                double dx = x - (rect.left + rect.right) * 0.5;
                double dy = y - (rect.top + rect.bottom) * 0.5;
                double d = dx * dx + dy * dy;//no need to get square root value.
                if (d <= minD)
                {
                    minD = d;
                    index = i;
                }
                //}
            }
            return index;
        }

        public PDFPage GetPage()
        {
            return _page;
        }

        public bool SetSelMarkup(int type)
        {
            if (_index1 < 0 || _index2 < 0 || !_selStart) return false;
            uint color = 0xFF0000FF;
            return _page.AddAnnotMarkup(_index1, _index2, color, type);
        }

        public bool EraseSel()
        {
            if (_index1 < 0 || _index2 < 0 || !_selStart) return false;
            int[] range = new int[2];
            range[0] = _index1;
            range[1] = _index2;
            _page.UpdateWithPGEditor();//clear edit buffer.
            return _page.ObjsRemove(range, true);
        }

        public string GetSelString()
        {
            if (_index1 < 0 || _index2 < 0 || !_selStart) return null;
            return _page.ObjsGetString(_index1, _index2 + 1);
        }

        public void DrawSel(Canvas canvas, float scale, float pageHeight)
        {
            if (_index1 < 0 || _index2 < 0 || !_selStart) return;

            uint color = 0x800000FF;

            for (int i = _index1; i <= _index2; i++)
            {
                RDRect rect = _page.ObjsGetCharRect(i);

                // Use page-local coordinates (no origin offset needed)
                double left = rect.left * scale;
                double top = (pageHeight - rect.bottom) * scale;
                double width = (rect.right - rect.left) * scale;
                double height = (rect.bottom - rect.top) * scale;

                var selRect = new Microsoft.UI.Xaml.Shapes.Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(
                        (byte)((color >> 24) & 0xFF),
                        (byte)((color >> 16) & 0xFF),
                        (byte)((color >> 8) & 0xFF),
                        (byte)(color & 0xFF)))
                };

                Microsoft.UI.Xaml.Controls.Canvas.SetLeft(selRect, left);
                Microsoft.UI.Xaml.Controls.Canvas.SetTop(selRect, top);
                canvas.Children.Add(selRect);
            }
        }
    }
}
