# RadaeePDF SDK Master for Windows (WinUI 3 - Beta)
<img src="https://www.radaeepdf.com/wp-content/uploads/2024/08/solo_butterly_midres.png" style="width:100px;"> 

> **DISCLAIMER (BETA)**  
> This repository contains the **WinUI 3** version of the RadaeePDF Windows demo and is currently in **BETA**.  
> The API surface, project structure, and some features are subject to change without notice.  
> Do **not** use this version in production without thoroughly testing it in your own environment.

RadaeePDF SDK is a powerful, native PDF rendering and manipulation library for Android, iOS, and Windows UI applications. Built from true native C++ code, it provides exceptional performance and a comprehensive set of features for working with PDF documents.

## About RadaeePDF

RadaeePDF SDK is designed to solve most developers' needs regard to PDF rendering and manipulation. The SDK is trusted across industries worldwide including automotive, banking, publishing, healthcare, and more.

### Key Features

- **PDF ISO32000 Compliance** - Full support for the widely-used PDF format standard
- **High Performance** - True native code compiled from C++ sources for optimal speed
- **Annotations** - Create and manage text annotations, highlights, ink annotations, and more
- **Protection & Encryption** - Full AES256 cryptography for document security
- **Text Handling** - Search, extract, and highlight text with ease
- **Form Editing** - Create, read, and write PDF form fields (AcroForms)
- **Digital Signatures** - Sign and verify PDF documents with digital certificates
- **Multiple View Modes** - Single page, continuous scroll, and more
- **Night Mode** - Built-in dark mode support for better readability

## Requirements

Before running the demo, ensure you have the following installed:

### Visual Studio Requirements
- **Visual Studio 2022** (or later) with the following workloads:
  - **WinUI desktop development** - Desktop development with WinUI 3 and C#, optionally for C++
  - **Desktop development with C++** - With C++ (v143) tools for native components
  
### Windows SDK
- **Windows 11 SDK (10.0.22621.0)** or later

### Windows Developer Mode
You must enable Developer Mode on Windows:

1. Open **Settings**
2. Go to **System** → **For developers**
3. Enable **Developer Mode**
   - This allows you to install apps from any source, including loose files

## Git and Git LFS

This repository uses **Git LFS** to store large binary files (such as the `*.lib` libraries).  
All developers **must** have Git LFS correctly installed and configured, both on **Windows** and **macOS**, otherwise the project will not build correctly.

### macOS (with Homebrew)

```bash
brew install git-lfs
git lfs install

git clone git@github.com:RadaeePDF-Jugaad/RadaeePDF-Master-Windows-WinUI.git
cd RadaeePDF-Master-Windows-WinUI

# From now on, a normal pull will also download/update LFS files (.lib)
git pull
```

### Windows

On Windows, install Git LFS using one of the following options:

- **Git for Windows installer** (select “Git LFS” during setup), or  
- **Chocolatey**:

  ```powershell
  choco install git-lfs
  git lfs install
  ```

Then clone the repository:

```powershell
git clone git@github.com:RadaeePDF-Jugaad/RadaeePDF-Master-Windows-WinUI.git
cd RadaeePDF-Master-Windows-WinUI

# From now on, a normal pull will also download/update LFS files (.lib)
git pull
```

If you already cloned this repository in the past **without** Git LFS enabled and you are missing `.lib` files, reclone the repository after configuring Git LFS, or contact the maintainer for migration instructions.

## Quick Start - Run Demo

To quickly test the RadaeePDF SDK demo:

1. **Clone the Repository**
   - Open Visual Studio
   - Click on **Clone a repository** (or File → Clone Repository)
   - Paste the repository URL:
     ```
     https://github.com/RadaeePDF-Jugaad/RadaeePDF-Master-Windows-WinUI.git
     ```
   - Click **Clone** and wait for the project to download

2. **Keep the Repository Up to Date**
   - From a terminal, inside the cloned folder:
     ```bash
     # Standard git
     git pull
     
     # Using git-lts (if installed in your environment)
     git-lts pull
     ```

3. **Open the Solution**
   - Visual Studio should automatically open the `RadaeeWinUI.sln` solution file
   - If not, navigate to the cloned folder and double-click `RadaeeWinUI.sln`

4. **Configure Build Settings**
   - In the toolbar, set the platform to **x86** (or other platform based on your system)
   - Set the configuration to **Debug** or **Release**

5. **Build library project**
   - In solution explorer, right click on project "RDUICom" and select **Build**
   - Wait for the build to complete

6. **Build and Run the Demo**
   - Press **F5** or click the **Start** button (▶) to build and run the demo
   - The WinUI 3 application will launch on your local machine

### Manual Installation

1. Download the RadaeePDF SDK library from Git Repository

## Getting Started

### Initialize the Library

Before using RadaeePDF, initialize the library with your license key:

1. Modify the 'RDGlobal.cs' file under namespace 'com.radaee.master', to add your license key:
```csharp
static public bool init()
{
    load_data();
    int ret = RDUILib.RDGlobal.Active("[YOUR-LICENSE-KEY]");
    return ret == 3;
}
```

2. Add the following code to your 'App' class to initialize and activate the library:
```csharp
using com.radaee.master;

sealed partial class App : Application
{
    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        //...... your code
        RDGlobal.init();
        //...... your code
    }
}
```

### Open and Display a PDF

```csharp
using com.radaee.master;
using RDDLib.comm;
using RDDLib.pdf;
using RDDLib.pdfv;
using RDDLib.reader;
using RDPDFMaster;
using RDPDFMaster.Modules.FTS;
using RDPDFMaster.PDFReaderPage;
using RDPDFMaster.util;
using RDPDFReader.annotui;

public sealed partial class PDFReaderPage : Page, IPDFViewListener, IThumbListener
{
    private PDFReader m_reader;
    private PDFThumb m_thumb;
    private PDFDoc m_doc;
    private void file_open(IRandomAccessStream stream, String password)
    {
        m_doc = new PDFDoc();
        RD_ERROR err = m_doc.Open(stream, password);
        switch (err)
        {
            case RD_ERROR.err_open:
                //can't open the PDF file
                break;
            case RD_ERROR.err_invalid_para:
                //invalid parameter
                break;
            case RD_ERROR.err_encrypt:
                //PDF file encrypted with unknown filter.
                break;
            case RD_ERROR.err_bad_file:
                //bad file
                break;
            case RD_ERROR.err_password:
                //password required
                break;
            case RD_ERROR.err_ok:
                //success
                _pdfViewModel.OnDocumentLoaded(_currentDocument);
                break;
            default:
                //Unknown error
                break;
        }
    }
}
```

## Common Operations

### Get Page Count

```csharp
int pageCount = m_doc.PageCount;
```

### Navigate to a Specific Page

```csharp
ViewModel.GoToPage(5); // Go to page 5
```

### Set View Mode

```csharp
ViewModel.PDFViewModel.SwitchViewMode(ViewMode.VerticalContinuous); // Vertical scroll mode
```

### Text Highlighting (Professional License)

```csharp
uint color = 0xFFFFFF00; // Yellow color
// Highlight selected text
PDFSel.SetSelMarkup(AnnotationType.Highlight); // highlight

// Underline selected text
PDFSel.SetSelMarkup(AnnotationType.Underline); // underline

// Strikeout selected text
PDFSel.SetSelMarkup(AnnotationType.Strikeout); // strikeout

// Rod squiggly selected text
PDFSel.SetSelMarkup(AnnotationType.Squiggly); // rod squiggly
```

### Add Annotations (Professional License)

```csharp
// Add a note annotation at point (x, y)
PDFPage page = m_doc.GetPage(0); // Get the first page
if(page != null) {
    page.ObjsStart();
    page.AddAnnotTextNote(x, y);
    page.Close();
}


// Remove annotation
PDFPage page = m_doc.GetPage(0); // Get the first page
if (page != null) {
    //Start object mode
    page.ObjsStart();
    PDFAnnot annot = page.GetAnnot(0);
    if (annot != null) { 
        annot.RemoveFromPage();
    }

    //Close page and release holding resource
    page.Close();
}
```

### Save Document

```csharp
// Save changes to the same file
m_doc.Save();

// Save to a new file
m_doc.SaveAs("[path_to_save]");
```

## License Levels

RadaeePDF offers different license levels with varying features:

Visit [https://www.radaeepdf.com/](https://www.radaeepdf.com/) for detailed licensing information.

## Documentation

For complete API documentation and advanced features, visit:
- [RadaeePDF Support Portal](https://support.radaeepdf.com/)

## Support

For technical support and questions:
- Email: support@radaeepdf.com
- Website: [https://www.radaeepdf.com/](https://www.radaeepdf.com/)

## License

This SDK is commercial software. Please ensure you have a valid license before using it in production applications.

---

© 2026 RadaeePDF. All rights reserved.
