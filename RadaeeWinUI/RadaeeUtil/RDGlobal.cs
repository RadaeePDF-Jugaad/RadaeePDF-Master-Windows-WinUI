using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace RadaeeWinUI.RadaeeUtil
{
    public sealed class RDGlobal
    {
        static private bool ms_loaded = false;
        static private void load_data()
        {
            if (ms_loaded) return;
            ms_loaded = true;
            String inst_path = Package.Current.InstalledLocation.Path;
            String cmap_path = inst_path + "\\Assets\\dat\\cmaps.dat";
            String umap_path = inst_path + "\\Assets\\dat\\umaps.dat";
            String cmyk_path = inst_path + "\\Assets\\dat\\cmyk_rgb.dat";
            if (new FileInfo(cmap_path).Exists && new FileInfo(umap_path).Exists)
                RDUILib.RDGlobal.SetCMapsPath(cmap_path, umap_path);
            if (new FileInfo(cmyk_path).Exists)
                RDUILib.RDGlobal.SetCMYKICC(cmyk_path);

            RDUILib.RDGlobal.FontFileListStart();
            //the new UWP can access font directory in system path.
            String fpath = SystemDataPaths.GetDefault().Fonts;
            DirectoryInfo finfo = new DirectoryInfo(fpath);
            FileInfo[] files = finfo.GetFiles();
            foreach (FileInfo file in files)
            {
                String ext = file.Extension.ToLower();
                if (ext.CompareTo(".ttf") == 0 || ext.CompareTo(".ttc") == 0 ||
                    ext.CompareTo(".otf") == 0 || ext.CompareTo(".otc") == 0)
                    RDUILib.RDGlobal.FontFileListAdd(file.FullName);
            }
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\argbsn00lp.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimo.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimob.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimobi.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\arimoi.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgy.otf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgyb.otf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgybi.otf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\texgyi.otf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousine.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousineb.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousinei.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\cousinebi.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\symbol.ttf");
            RDUILib.RDGlobal.FontFileListAdd(inst_path + "\\Assets\\font\\amiriRegular.ttf");
            RDUILib.RDGlobal.FontFileListEnd();

            RDUILib.RDGlobal.FontFileMapping("Arial", "Arimo");
            RDUILib.RDGlobal.FontFileMapping("Arial Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Arial BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Arial Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("Arial,Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Arial,BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Arial,Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("Arial-Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Arial-BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Arial-Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("ArialMT", "Arimo");
            RDUILib.RDGlobal.FontFileMapping("Calibri", "Arimo");
            RDUILib.RDGlobal.FontFileMapping("Calibri Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Calibri BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Calibri Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("Calibri,Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Calibri,BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Calibri,Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("Calibri-Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Calibri-BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Calibri-Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("Helvetica", "Arimo");
            RDUILib.RDGlobal.FontFileMapping("Helvetica Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Helvetica BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Helvetica Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("Helvetica,Bold", "Arimo,Bold");
            RDUILib.RDGlobal.FontFileMapping("Helvetica,BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Helvetica,Italic", "Arimo Italic");
            RDUILib.RDGlobal.FontFileMapping("Helvetica-Bold", "Arimo Bold");
            RDUILib.RDGlobal.FontFileMapping("Helvetica-BoldItalic", "Arimo Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Helvetica-Italic", "Arimo Italic");

            RDUILib.RDGlobal.FontFileMapping("Garamond", "TeXGyreTermes-Regular");
            RDUILib.RDGlobal.FontFileMapping("Garamond,Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("Garamond,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("Garamond,Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("Garamond-Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("Garamond-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("Garamond-Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("Times", "TeXGyreTermes-Regular");
            RDUILib.RDGlobal.FontFileMapping("Times,Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("Times,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("Times,Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("Times-Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("Times-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("Times-Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("Times-Roman", "TeXGyreTermes-Regular");
            RDUILib.RDGlobal.FontFileMapping("Times New Roman", "TeXGyreTermes-Regular");
            RDUILib.RDGlobal.FontFileMapping("Times New Roman,Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("Times New Roman,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("Times New Roman,Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("Times New Roman-Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("Times New Roman-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("Times New Roman-Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRoman", "TeXGyreTermes-Regular");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRoman,Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRoman,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRoman,Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRoman-Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRoman-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRoman-Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPS", "TeXGyreTermes-Regular");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPS,Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPS,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPS,Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPS-Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPS-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPS-Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPSMT", "TeXGyreTermes-Regular");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPSMT,Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPSMT,BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPSMT,Italic", "TeXGyreTermes-Italic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPSMT-Bold", "TeXGyreTermes-Bold");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPSMT-BoldItalic", "TeXGyreTermes-BoldItalic");
            RDUILib.RDGlobal.FontFileMapping("TimesNewRomanPSMT-Italic", "TeXGyreTermes-Italic");

            RDUILib.RDGlobal.FontFileMapping("Courier", "Cousine");
            RDUILib.RDGlobal.FontFileMapping("Courier Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("Courier BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier,Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("Courier,BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier,Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier-Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("Courier-BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier-Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier New", "Cousine");
            RDUILib.RDGlobal.FontFileMapping("Courier New Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("Courier New BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier New Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier New,Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("Courier New,BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier New,Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier New-Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("Courier New-BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("Courier New-Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("CourierNew", "Cousine");
            RDUILib.RDGlobal.FontFileMapping("CourierNew Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("CourierNew BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("CourierNew Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("CourierNew,Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("CourierNew,BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("CourierNew,Italic", "Cousine Italic");
            RDUILib.RDGlobal.FontFileMapping("CourierNew-Bold", "Cousine Bold");
            RDUILib.RDGlobal.FontFileMapping("CourierNew-BoldItalic", "Cousine Bold Italic");
            RDUILib.RDGlobal.FontFileMapping("CourierNew-Italic", "Cousine Italic");

            RDUILib.RDGlobal.FontFileMapping("Symbol", "Symbol Neu for Powerline");
            RDUILib.RDGlobal.FontFileMapping("Symbol,Bold", "Symbol Neu for Powerline");
            RDUILib.RDGlobal.FontFileMapping("Symbol,BoldItalic", "Symbol Neu for Powerline");
            RDUILib.RDGlobal.FontFileMapping("Symbol,Italic", "Symbol Neu for Powerline");

            // first available face, used as the last fallback of every font chain.
            String? rand_fname = null;
            int face_count = RDUILib.RDGlobal.GetFaceCount();
            for (int face_first = 0; face_first < face_count; face_first++)
            {
                String fname = RDUILib.RDGlobal.GetFaceName(face_first);
                if (fname != null && fname.Length > 0)
                {
                    rand_fname = fname;
                    break;
                }
            }

            // set default fonts.
            // CJK system fonts may be missing (e.g. on EUR/US systems), so every collection
            // has its own fallback chain, ending with the bundled "AR PL SungtiL GB" / Arimo.
            SetDefaultFontChain("", true, "Calibri", "Arimo", rand_fname);
            SetDefaultFontChain("", false, "Times New Roman", "TeXGyreTermes-Regular", "Arimo", rand_fname);
            foreach (bool fixd in new bool[] { true, false })
            {
                SetDefaultFontChain("GB1", fixd, "SimSun", "Microsoft YaHei", "AR PL SungtiL GB", rand_fname);
                SetDefaultFontChain("CNS1", fixd, "Microsoft JhengHei", "Microsoft JHengHei", "MingLiU", "PMingLiU", "AR PL SungtiL GB", rand_fname);
                SetDefaultFontChain("Japan1", fixd, "MS Gothic", "Yu Gothic", "Meiryo", "AR PL SungtiL GB", rand_fname);
                SetDefaultFontChain("Korea1", fixd, "Malgun Gothic Regular", "Malgun Gothic", "Gulim", "AR PL SungtiL GB", rand_fname);
            }

            // set text font for edit-box and combo-box editing, depends on user's display language.
            List<String?> annot_fonts = new List<String?>(GetAnnotFontCandidates());
            annot_fonts.Add(rand_fname);
            SetAnnotFontChain(annot_fonts.ToArray());

            //RDUILib.PDFEditNode.SetDefFont("Times New Roman");
            //RDUILib.PDFEditNode.SetDefCJKFont("SimSun");

            // set annotation text font.
            RDUILib.RDGlobal.LoadStdFont(13, inst_path + "\\Assets\\font\\rdf013");
        }
        /// <summary>
        /// Try fonts in order until one is set as default font of the collection.
        /// </summary>
        /// <returns>Name of the font applied, or null if all failed.</returns>
        static private String? SetDefaultFontChain(String collection, bool fixd, params String?[] names)
        {
            foreach (String? name in names)
            {
                if (name != null && RDUILib.RDGlobal.SetDefaultFont(collection, name, fixd))
                {
                    Debug.WriteLine("RDGlobal: default font [" + collection + "] fixed=" + fixd + " -> " + name);
                    return name;
                }
            }
            Debug.WriteLine("RDGlobal: no default font for [" + collection + "] fixed=" + fixd);
            return null;
        }
        /// <summary>
        /// Try fonts in order until one is set as annotation font.
        /// </summary>
        /// <returns>Name of the font applied, or null if all failed.</returns>
        static private String? SetAnnotFontChain(params String?[] names)
        {
            foreach (String? name in names)
            {
                if (name != null && RDUILib.RDGlobal.SetAnnotFont(name))
                {
                    Debug.WriteLine("RDGlobal: annot font -> " + name);
                    return name;
                }
            }
            Debug.WriteLine("RDGlobal: no annot font");
            return null;
        }
        // Windows display language. Unlike GlobalizationPreferences.Languages,
        // it does not depend on the order of user's preferred languages list.
        [DllImport("kernel32.dll")]
        static private extern ushort GetUserDefaultUILanguage();
        private const int LANG_CHINESE = 0x04;
        private const int LANG_JAPANESE = 0x11;
        private const int LANG_KOREAN = 0x12;
        private const int SUBLANG_CHINESE_TRADITIONAL = 0x01; // zh-TW
        private const int SUBLANG_CHINESE_HONGKONG = 0x03;    // zh-HK
        private const int SUBLANG_CHINESE_MACAU = 0x05;       // zh-MO
        private const int SUBLANG_CHINESE_HANT = 0x1F;        // zh-Hant
        /// <summary>
        /// Annotation font candidates by Windows display language.
        /// CJK users get CJK fonts first, others get Arimo first (better EUR languages support).
        /// </summary>
        static private String[] GetAnnotFontCandidates()
        {
            int langid = GetUserDefaultUILanguage();
            int primary = langid & 0x3FF;
            int sub = langid >> 10;
            Debug.WriteLine("RDGlobal: UI language id = 0x" + langid.ToString("X4"));

            if (primary == LANG_CHINESE)
            {
                if (sub == SUBLANG_CHINESE_TRADITIONAL || sub == SUBLANG_CHINESE_HONGKONG ||
                    sub == SUBLANG_CHINESE_MACAU || sub == SUBLANG_CHINESE_HANT)
                    return new String[] { "Microsoft JhengHei", "Microsoft JHengHei", "MingLiU", "AR PL SungtiL GB", "Arimo" };
                return new String[] { "SimSun", "Microsoft YaHei", "AR PL SungtiL GB", "Arimo" };
            }
            if (primary == LANG_JAPANESE)
                return new String[] { "MS Gothic", "Yu Gothic", "Meiryo", "AR PL SungtiL GB", "Arimo" };
            if (primary == LANG_KOREAN)
                return new String[] { "Malgun Gothic Regular", "Malgun Gothic", "Gulim", "AR PL SungtiL GB", "Arimo" };
            return new String[] { "Arimo", "AR PL SungtiL GB" };
        }

        static public bool init()
        {
            load_data();
            String sver = RDUILib.RDGlobal.GetVersion();//this versioin string, example "20220225".
            //the key is binding to package "com.radaee.reader", can active version before "20271006"
            int ret = RDUILib.RDGlobal.Active("21C3B000BAA97ECDCC6365F07BDB48D2EF7537C8B027E4B1442199452C71AC453012C367E1499D7B1906E95C810FF0EB");
            return ret > 0;
        }

        static public bool DrawDash(float[] dash, int dashCount, WriteableBitmap dib) {
            return true;
            //return RDUILib.RDGlobal.DrawDash(dash, dashCount, dib);
        }
    }
}
