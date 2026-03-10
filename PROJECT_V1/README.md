# Sales Proposal Builder (Hackathon Demo)

An AI-powered sales proposal generator built with ASP.NET Core 6 MVC + Razor.  
Enter client details, choose a tone, and generate a structured proposal in one click.

## Quick Start

1. Set your Gemini API key (recommended)

```powershell
$env:GEMINI_API_KEY="YOUR_GEMINI_API_KEY"
```

2. Run the app

```powershell
dotnet build
dotnet run
```

3. Open the browser to the URL shown in the console (typically `https://localhost:5001` or `http://localhost:5000`).

## Configuration

You can also set the API key and provider in `appsettings.json`:

```json
{
  "Llm": {
    "Provider": "Gemini",
    "Gemini": {
      "ApiKey": "YOUR_GEMINI_API_KEY",
      "ApiBase": "https://generativelanguage.googleapis.com/v1beta/models/",
      "Model": "gemini-2.5-pro"
    }
  }
}
```

Environment variables (`GEMINI_API_KEY` or `OPENAI_API_KEY`) take precedence over `appsettings.json`.

## Notes

- The app expects JSON-only responses from the LLM and renders each proposal section with inline editing.
- No database or authentication is required for this demo.
- Use the "Download PDF" button after a proposal is created to save a generated PDF.
- Basic security hardening is enabled (HSTS, no server header, and strict security headers like CSP, X-Frame-Options, and nosniff).
