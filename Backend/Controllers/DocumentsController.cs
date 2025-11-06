using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;
    private readonly DocumentProcessingService _documentService;
    private readonly ClaudeService _claudeService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        DocumentProcessingService documentService,
        ClaudeService claudeService,
        ILogger<DocumentsController> logger)
    {
        _context = context;
        _environment = environment;
        _documentService = documentService;
        _claudeService = claudeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<DocumentDto>>> GetDocuments()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var documents = await _context.Documents
            .Include(d => d.Analysis)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.UploadedAt)
            .Select(d => new DocumentDto
            {
                Id = d.Id,
                FileName = d.FileName,
                ContentType = d.ContentType,
                FileSize = d.FileSize,
                UploadedAt = d.UploadedAt,
                Analysis = d.Analysis != null ? new DocumentAnalysisDto
                {
                    Id = d.Analysis.Id,
                    ProjectName = d.Analysis.ProjectName,
                    ProjectDuration = d.Analysis.ProjectDuration,
                    HumanResourcesHierarchy = d.Analysis.HumanResourcesHierarchy,
                    ProjectStages = d.Analysis.ProjectStages,
                    SpecialConditions = d.Analysis.SpecialConditions,
                    ImplementationBoundaries = d.Analysis.ImplementationBoundaries,
                    AnalyzedAt = d.Analysis.AnalyzedAt
                } : null
            })
            .ToListAsync();

        return Ok(documents);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<DocumentDto>> UploadDocument(IFormFile file)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized();
        }

        // Check user's document count
        var documentCount = await _context.Documents.CountAsync(d => d.UserId == userId);
        if (documentCount >= 10)
        {
            return BadRequest(new { message = "Maximum of 10 documents allowed per user" });
        }

        // Validate file type
        var allowedTypes = new[] { "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword" };
        if (!allowedTypes.Contains(file.ContentType))
        {
            return BadRequest(new { message = "Only PDF and Word documents are allowed" });
        }

        // Create uploads directory
        var uploadsPath = Path.Combine(_environment.ContentRootPath, "uploads", userId);
        Directory.CreateDirectory(uploadsPath);

        // Save file
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create document record
        var document = new Document
        {
            FileName = file.FileName,
            FilePath = filePath,
            ContentType = file.ContentType,
            FileSize = file.Length,
            UserId = userId
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        return Ok(new DocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            ContentType = document.ContentType,
            FileSize = document.FileSize,
            UploadedAt = document.UploadedAt
        });
    }

    [HttpPost("analyze")]
    public async Task<ActionResult> AnalyzeDocuments(AnalyzeDocumentsDto dto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Get documents
        var documents = await _context.Documents
            .Where(d => dto.DocumentIds.Contains(d.Id) && d.UserId == userId)
            .ToListAsync();

        if (documents.Count == 0)
        {
            return NotFound(new { message = "No documents found" });
        }

        // Extract text from all documents
        var combinedText = new System.Text.StringBuilder();
        foreach (var doc in documents)
        {
            try
            {
                var text = await _documentService.ExtractTextFromFileAsync(doc.FilePath, doc.ContentType);
                combinedText.AppendLine($"--- Document: {doc.FileName} ---");
                combinedText.AppendLine(text);
                combinedText.AppendLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from document {DocumentId}", doc.Id);
            }
        }

        // Analyze with Claude
        var analysisResult = await _claudeService.AnalyzeDocumentAsync(combinedText.ToString());

        // Parse and save analysis for each document
        foreach (var doc in documents)
        {
            try
            {
                // Try to parse JSON from the response
                var jsonStart = analysisResult.IndexOf('{');
                var jsonEnd = analysisResult.LastIndexOf('}') + 1;
                var jsonString = analysisResult.Substring(jsonStart, jsonEnd - jsonStart);

                using var jsonDoc = JsonDocument.Parse(jsonString);
                var root = jsonDoc.RootElement;

                var analysis = new DocumentAnalysis
                {
                    DocumentId = doc.Id,
                    ProjectName = root.TryGetProperty("projectName", out var pn) ? pn.GetString() : null,
                    ProjectDuration = root.TryGetProperty("projectDuration", out var pd) ? pd.GetString() : null,
                    HumanResourcesHierarchy = root.TryGetProperty("humanResourcesHierarchy", out var hr) ? hr.GetString() : null,
                    ProjectStages = root.TryGetProperty("projectStages", out var ps) ? ps.GetString() : null,
                    SpecialConditions = root.TryGetProperty("specialConditions", out var sc) ? sc.GetString() : null,
                    ImplementationBoundaries = root.TryGetProperty("implementationBoundaries", out var ib) ? ib.GetString() : null,
                    RawAnalysis = analysisResult
                };

                _context.DocumentAnalyses.Add(analysis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing analysis for document {DocumentId}", doc.Id);
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Analysis completed successfully" });
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteDocument(int id)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id && d.UserId == userId);

        if (document == null)
        {
            return NotFound();
        }

        // Delete file
        if (System.IO.File.Exists(document.FilePath))
        {
            System.IO.File.Delete(document.FilePath);
        }

        _context.Documents.Remove(document);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
