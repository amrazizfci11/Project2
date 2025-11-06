namespace Backend.Models;

public class DocumentAnalysis
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public Document Document { get; set; } = null!;
    public string? ProjectName { get; set; }
    public string? ProjectDuration { get; set; }
    public string? HumanResourcesHierarchy { get; set; }
    public string? ProjectStages { get; set; }
    public string? SpecialConditions { get; set; }
    public string? ImplementationBoundaries { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string RawAnalysis { get; set; } = string.Empty;
}
