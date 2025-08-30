# PDF'd - Industrial-Strength PDF Processing

A Windows desktop tool that does four things perfectly: Convert, Compress, Batch Process, and Analyze PDFs.

## What PDF'd Does (And Does Better Than Anyone)

### 1. **PDF to Word Conversion** 
Preserves complex layouts, tables, and formatting that other tools destroy.

### 2. **Batch Processing**
Process 10,000 files without crashing. Parallel execution. Zero memory leaks.

### 3. **PDF Intelligence** 
Instantly diagnose problem PDFs. See what's really inside. Know why conversions fail.

### 4. **PDF Compression**
Reduce file size intelligently using Ghostscript's proven algorithms.

## Pricing

$15.99 - One-time purchase. Lifetime license. No subscriptions. No BS.

## Why PDF'd Doesn't Fail

We use the same tools that power enterprise systems worldwide:
- **Xpdf Tools** - 25+ years of PDF parsing excellence
- **Ghostscript** - The industry standard for PDF manipulation
- **Process Isolation** - Each operation in its own sandbox

## Tech Stack

### Core
* .NET 8 + C# 12 (LTS)
* WPF (fast, native, no web framework overhead)

### Battle-Tested PDF Tools
* **pdftotext.exe** - Text extraction that actually works
* **pdfinfo.exe** - PDF analysis and validation
* **pdffonts.exe** - Font inspection and debugging
* **pdfimages.exe** - Image extraction with format preservation
* **pdfdetach.exe** - Extract embedded files
* **Ghostscript** - Compression and optimization

### Supporting Libraries
* **Open XML SDK** - Word document generation
* **CommunityToolkit.Mvvm** - MVVM implementation

## Architecture Principles

1. **Process Isolation** - Tools run in separate processes (can't crash the app)
2. **Parallel by Default** - Multi-core batch processing
3. **Fail-Safe** - Original files are never touched
4. **Deterministic** - Same input = same output, always

## Performance Guarantees

* Startup: < 1 second
* Memory: < 500MB per operation
* Speed: > 10MB/second
* Batch: 10,000 files without failure
* Accuracy: > 99.9% success rate

## Building

```bash
dotnet build
dotnet run --project PDFd.UI
```

## Repository

https://github.com/quikmn/pdfd