using System.Text;
using PROJECT_V1.Models;

namespace PROJECT_V1.Services;

public static class ProposalPdfRenderer
{
    private const int PageWidth = 612;
    private const int PageHeight = 792;
    private const int MarginLeft = 72;
    private const int MarginTop = 72;
    private const int TitleFontSize = 18;
    private const int HeadingFontSize = 13;
    private const int BodyFontSize = 11;
    private const int LineGap = 4;
    private const int BodyIndent = 14;
    private const int BulletIndent = 16;
    private const int BulletContinuationIndent = 28;
    private const int MaxCharsPerLine = 95;

    public static byte[] Render(ProposalDocumentModel model)
    {
        var lines = BuildLines(model);
        var pages = Paginate(lines);

        var pdf = new PdfBuilder();
        var fontRegularObj = pdf.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>");
        var fontBoldObj = pdf.AddObject("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>");
        var pagesObj = pdf.AddObject("<< >>");

        var pageObjects = new List<int>();
        foreach (var pageLines in pages)
        {
            var content = BuildContentStream(pageLines);
            var contentObj = pdf.AddStreamObject(content);
            var pageObj = pdf.AddObject(
                $"<< /Type /Page /Parent {pagesObj} 0 R /MediaBox [0 0 {PageWidth} {PageHeight}] " +
                $"/Resources << /Font << /F1 {fontRegularObj} 0 R /F2 {fontBoldObj} 0 R >> >> " +
                $"/Contents {contentObj} 0 R >>");
            pageObjects.Add(pageObj);
        }

        var kids = string.Join(" ", pageObjects.Select(id => $"{id} 0 R"));
        pdf.UpdateObject(pagesObj, $"<< /Type /Pages /Kids [{kids}] /Count {pageObjects.Count} >>");

        var catalogObj = pdf.AddObject($"<< /Type /Catalog /Pages {pagesObj} 0 R >>");
        return pdf.Build(catalogObj);
    }

    private static List<PdfLine> BuildLines(ProposalDocumentModel model)
    {
        var lines = new List<PdfLine>
        {
            new("Sales Proposal", true, TitleFontSize),
            new(string.Empty, false, BodyFontSize)
        };

        AddMetaLine(lines, "Client", model.ClientName);
        AddMetaLine(lines, "Industry", model.Industry);
        AddMetaLine(lines, "Budget", model.BudgetRange);
        AddMetaLine(lines, "Tone", model.Tone);

        lines.Add(new PdfLine(string.Empty, false, BodyFontSize));

        AddSection(lines, "Executive Summary", model.ExecutiveSummary);
        AddSection(lines, "Scope of Work", model.ScopeOfWork);
        AddSection(lines, "Timeline", model.Timeline);
        AddSection(lines, "Pricing Estimate", model.PricingEstimate);
        AddAssumptions(lines, model.Assumptions);
        AddSection(lines, "Call to Action", model.CallToAction);

        return lines;
    }

    private static void AddMetaLine(List<PdfLine> lines, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        lines.Add(new PdfLine($"{label}: {value}", false, BodyFontSize));
    }

    private static void AddSection(List<PdfLine> lines, string title, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        lines.Add(new PdfLine(title.ToUpperInvariant(), true, HeadingFontSize));
        foreach (var line in WrapText(content, MaxCharsPerLine - 6))
        {
            lines.Add(new PdfLine(line, false, BodyFontSize, BodyIndent));
        }
        lines.Add(new PdfLine(string.Empty, false, BodyFontSize));
    }

    private static void AddAssumptions(List<PdfLine> lines, string assumptions)
    {
        if (string.IsNullOrWhiteSpace(assumptions))
        {
            return;
        }

        lines.Add(new PdfLine("ASSUMPTIONS", true, HeadingFontSize));
        foreach (var item in SplitAssumptions(assumptions))
        {
            var wrapped = WrapText(item, MaxCharsPerLine - 10).ToList();
            for (var i = 0; i < wrapped.Count; i++)
            {
                var text = i == 0 ? $"- {wrapped[i]}" : wrapped[i];
                var indent = i == 0 ? BulletIndent : BulletContinuationIndent;
                lines.Add(new PdfLine(text, false, BodyFontSize, indent));
            }
        }
        lines.Add(new PdfLine(string.Empty, false, BodyFontSize));
    }

    private static IEnumerable<string> SplitAssumptions(string assumptions)
    {
        var parts = assumptions
            .Split(new[] { "\r\n", "\n", ";" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim().TrimStart('-', '*', ' '))
            .Where(item => !string.IsNullOrWhiteSpace(item));

        return parts;
    }

    private static IEnumerable<string> WrapText(string text, int maxChars)
    {
        var cleaned = Sanitize(text);
        var paragraphs = cleaned.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var paragraph in paragraphs)
        {
            var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var line = new StringBuilder();
            foreach (var word in words)
            {
                if (line.Length == 0)
                {
                    line.Append(word);
                    continue;
                }

                if (line.Length + word.Length + 1 > maxChars)
                {
                    yield return line.ToString();
                    line.Clear();
                    line.Append(word);
                    continue;
                }

                line.Append(' ').Append(word);
            }

            if (line.Length > 0)
            {
                yield return line.ToString();
            }
        }
    }

    private static List<List<PdfLine>> Paginate(List<PdfLine> lines)
    {
        var availableHeight = PageHeight - (MarginTop * 2);
        var pages = new List<List<PdfLine>>();
        var current = new List<PdfLine>();
        var currentHeight = 0;

        foreach (var line in lines)
        {
            var lineHeight = GetLineHeight(line);
            if (currentHeight + lineHeight > availableHeight && current.Count > 0)
            {
                pages.Add(current);
                current = new List<PdfLine>();
                currentHeight = 0;
            }

            current.Add(line);
            currentHeight += lineHeight;
        }

        if (current.Count > 0)
        {
            pages.Add(current);
        }

        return pages;
    }

    private static string BuildContentStream(IEnumerable<PdfLine> lines)
    {
        var builder = new StringBuilder();
        builder.AppendLine("BT");

        var currentY = PageHeight - MarginTop;
        foreach (var line in lines)
        {
            var fontRef = line.Bold ? "F2" : "F1";
            var fontSize = line.FontSize;
            var x = MarginLeft + line.Indent;
            var y = currentY;
            var escaped = EscapePdfText(line.Text);
            builder.AppendLine($"/{fontRef} {fontSize} Tf");
            builder.AppendLine($"1 0 0 1 {x} {y} Tm");
            builder.AppendLine($"({escaped}) Tj");
            currentY -= GetLineHeight(line);
        }

        builder.AppendLine("ET");
        return builder.ToString();
    }

    private static int GetLineHeight(PdfLine line)
    {
        return line.FontSize + LineGap;
    }

    private static string EscapePdfText(string text)
    {
        return text
            .Replace("\\", "\\\\")
            .Replace("(", "\\(")
            .Replace(")", "\\)");
    }

    private static string Sanitize(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch == '\t')
            {
                builder.Append(' ');
                continue;
            }

            if (ch < 32)
            {
                continue;
            }

            builder.Append(ch <= 126 ? ch : '?');
        }
        return builder.ToString();
    }

    private sealed record PdfLine(string Text, bool Bold, int FontSize, int Indent = 0);

    private sealed class PdfBuilder
    {
        private readonly List<string> _objects = new();

        public int AddObject(string content)
        {
            _objects.Add(content);
            return _objects.Count;
        }

        public int AddStreamObject(string content)
        {
            var length = Encoding.ASCII.GetByteCount(content);
            var stream = $"<< /Length {length} >>\nstream\n{content}endstream";
            return AddObject(stream);
        }

        public void UpdateObject(int id, string content)
        {
            if (id <= 0 || id > _objects.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }

            _objects[id - 1] = content;
        }

        public byte[] Build(int rootObjectId)
        {
            var builder = new StringBuilder();
            builder.AppendLine("%PDF-1.4");

            var offsets = new List<int> { 0 };
            for (var i = 0; i < _objects.Count; i++)
            {
                offsets.Add(builder.Length);
                builder.AppendLine($"{i + 1} 0 obj");
                builder.AppendLine(_objects[i]);
                builder.AppendLine("endobj");
            }

            var xrefStart = builder.Length;
            builder.AppendLine("xref");
            builder.AppendLine($"0 {_objects.Count + 1}");
            builder.AppendLine("0000000000 65535 f ");
            for (var i = 1; i < offsets.Count; i++)
            {
                builder.AppendLine($"{offsets[i]:D10} 00000 n ");
            }

            builder.AppendLine("trailer");
            builder.AppendLine($"<< /Size {_objects.Count + 1} /Root {rootObjectId} 0 R >>");
            builder.AppendLine("startxref");
            builder.AppendLine($"{xrefStart}");
            builder.AppendLine("%%EOF");

            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
