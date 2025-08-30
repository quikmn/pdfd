# PDF'd - Stop PDFing Around

A desktop PDF tool that actually works.

## What PDF'd Does (and does perfectly)
1. **Convert PDF to Word** - Without destroying formatting
2. **Compress PDFs** - Intelligently 
3. **Batch Processing** - Handle thousands of files without crashing

## Price
$15.99 once. Own it forever.

## Development Philosophy
- Do few things. Do them perfectly. No bullshit.
- Every line of code is intentional
- Every feature is essential
- Every decision is deliberate

## Tech Stack (Minimal by Design)
- .NET 8 + C# 12 (latest LTS)
- WPF (mature, fast, no BS)
- PDFsharp (MIT)
- Open XML SDK (MIT)
- Tesseract.NET (Apache 2.0)
- CommunityToolkit.Mvvm (MIT)

That's it. No more libraries without a fight.

## Building
```bash
dotnet build
dotnet test
dotnet run --project PDFd.UI
```

## Performance Requirements
- Startup time < 1 second
- Memory usage < 500MB for any operation
- Processing speed > 10MB/second
- Batch handling: 10,000 files without crashing
- Success rate > 99% on real-world PDFs

---
*Stop PDFing around. Get PDF'd.*
