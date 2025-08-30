# PDF'd - Professional PDF Processing for Windows

A desktop PDF tool that leverages battle-tested technology to deliver reliable results.

## Core Features

1. **PDF to Word Conversion** - Preserves formatting and structure
2. **PDF Compression** - Intelligent size reduction without quality loss
3. **Batch Processing** - Handle thousands of files efficiently

## Pricing

$15.99 - One-time purchase. Lifetime license.

## Development Philosophy

* Focus on core functionality with perfect execution
* Use proven, reliable tools over trendy solutions
* Every dependency must earn its place
* Performance and reliability over feature bloat

## Tech Stack

### Core Framework
* .NET 8 + C# 12 (LTS)
* WPF for UI (mature, performant, stable)

### PDF Processing Tools
* **pdftotext.exe** (Xpdf/Poppler) - Text extraction engine
* **pdfinfo.exe** (Xpdf/Poppler) - PDF metadata and structure analysis
* **PDFsharp** (MIT) - PDF manipulation and generation
* **Open XML SDK** (MIT) - Word document creation

### Supporting Libraries
* **Tesseract.NET** (Apache 2.0) - OCR for scanned PDFs
* **CommunityToolkit.Mvvm** (MIT) - MVVM pattern implementation

## Why pdftotext/pdfinfo?

These command-line tools from the Xpdf/Poppler suite are:
- Battle-tested over 20+ years
- Used by millions of systems worldwide
- Consistently accurate with complex PDFs
- Fast and memory-efficient
- Handle edge cases that break other libraries

## Building

```bash
dotnet build
dotnet test
dotnet run --project PDFd.UI
```

## Performance Targets

* Startup time: < 1 second
* Memory usage: < 500MB per operation
* Processing speed: > 10MB/second
* Batch capacity: 10,000 files without failure
* Success rate: > 99% on real-world PDFs

## Repository

https://github.com/quikmn/pdfd

## License

Commercial software. See LICENSE file for details.