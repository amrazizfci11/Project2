namespace Backend.DTOs;

public class DocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public DocumentAnalysisDto? Analysis { get; set; }
}

public class DocumentAnalysisDto
{
    public int Id { get; set; }
    public string? ProjectName { get; set; }
    public string? ProjectDuration { get; set; }
    public string? HumanResourcesHierarchy { get; set; }
    public string? ProjectStages { get; set; }
    public string? SpecialConditions { get; set; }
    public string? ImplementationBoundaries { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public class AnalyzeDocumentsDto
{
    public List<int> DocumentIds { get; set; } = new();
}
