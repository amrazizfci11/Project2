using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text;

namespace Backend.Services;

public class DocumentProcessingService
{
    private readonly ILogger<DocumentProcessingService> _logger;

    public DocumentProcessingService(ILogger<DocumentProcessingService> logger)
    {
        _logger = logger;
    }

    public async Task<string> ExtractTextFromFileAsync(string filePath, string contentType)
    {
        try
        {
            return contentType.ToLower() switch
            {
                "application/pdf" => await ExtractTextFromPdfAsync(filePath),
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" =>
                    await ExtractTextFromWordAsync(filePath),
                "application/msword" => await ExtractTextFromWordAsync(filePath),
                _ => throw new NotSupportedException($"File type {contentType} is not supported")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file: {FilePath}", filePath);
            throw;
        }
    }

    private async Task<string> ExtractTextFromPdfAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var text = new StringBuilder();
            using var reader = new PdfReader(filePath);

            for (int page = 1; page <= reader.NumberOfPages; page++)
            {
                var pageText = PdfTextExtractor.GetTextFromPage(reader, page);
                text.AppendLine(pageText);
            }

            return text.ToString();
        });
    }

    private async Task<string> ExtractTextFromWordAsync(string filePath)
    {
        return await Task.Run(() =>
        {
            var text = new StringBuilder();

            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document.Body;

            if (body != null)
            {
                foreach (var paragraph in body.Elements<Paragraph>())
                {
                    text.AppendLine(paragraph.InnerText);
                }
            }

            return text.ToString();
        });
    }
}
