#pragma once

#include "RDUILib.RDDIB.g.h"
#include "UICom.h"
#include "UIPDF.h"

namespace winrt::RDUILib::implementation
{
    struct RDDIB : RDDIBT<RDDIB>
    {
        RDDIB(int32_t w, int32_t h)
        {
            m_dib = Global_dibGet(NULL, w, h);
        }
        ~RDDIB()
        {
            Global_dibFree(m_dib);
        }
        void Resize(int32_t w, int32_t h)
        {
            m_dib = Global_dibGet(m_dib, w, h);
        }
        int64_t Handle()
        {
            return (int64_t)m_dib;
        }
        bool SaveJPG(winrt::hstring path, int32_t quality)
        {
            char stxt[512];
            WideCharToMultiByte(CP_ACP, 0, path.c_str(), -1, stxt, 511, NULL, NULL);
            return Global_dibSaveJPG(m_dib, stxt, quality);
        }
        void Reset(uint32_t color)
        {
            int w = Global_dibGetWidth(m_dib);
            int h = Global_dibGetHeight(m_dib);
            unsigned int* dat_cur = (unsigned int*)Global_dibGetData(m_dib);
            unsigned int* dat_end = dat_cur + (w * h);
            while (dat_cur < dat_end) *dat_cur++ = color;
        }
        static inline void cpy_clr(unsigned int* dst, const unsigned int* src, int count)
        {
            unsigned int* dst_end = dst + count;
            while (dst < dst_end) *dst++ = *src++;
        }
        void DrawDIB(RDUILib::RDDIB src, int32_t x, int32_t y)
        {
            int sx = 0;
            int sy = 0;
            int dx = x;
            int dy = y;
            int dw = Global_dibGetWidth(m_dib);
            int dh = Global_dibGetHeight(m_dib);
            int sw = Global_dibGetWidth((PDF_DIB)src.Handle());
            int sh = Global_dibGetHeight((PDF_DIB)src.Handle());
            if (dx < 0)
            {
                sx -= dx;
                dx = 0;
            }
            if (dy < 0)
            {
                sy -= dy;
                dy = 0;
            }
            int w = dw - dx;
            int h = dh - dy;
            int w1 = sw - sx;
            int h1 = sh - sy;
            if (w < w1) w = w1;
            if (h < h1) h = h1;

            BYTE* psrc = (BYTE*)Global_dibGetData((PDF_DIB)src.Handle()) + ((sy * sw + sx) << 2);
            BYTE* pdst = (BYTE*)Global_dibGetData(m_dib) + ((dy * dw + dx) << 2);
            while (h > 0)
            {
                cpy_clr((unsigned int*)pdst, (const unsigned int*)psrc, w);
                pdst += (dw << 2);
                psrc += (sw << 2);
                h--;
            }
        }
        int32_t Width()
        {
            return Global_dibGetWidth(m_dib);
        }
        int32_t Height()
        {
            return Global_dibGetHeight(m_dib);
        }
        com_array<uint8_t> Data()
        {
            com_array<uint8_t> ret(Global_dibGetWidth(m_dib) * Global_dibGetHeight(m_dib) * 4);
            memcpy(ret.data(), (uint8_t*)Global_dibGetData(m_dib), ret.size());
            return ret;
        }
        PDF_DIB m_dib;
    };
}

namespace winrt::RDUILib::factory_implementation
{
    struct RDDIB : RDDIBT<RDDIB, implementation::RDDIB>
    {
    };
}
