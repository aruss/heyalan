namespace HeyAlan.Data;

public interface IEntityWithAudit
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}