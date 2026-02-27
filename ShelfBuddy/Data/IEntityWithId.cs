namespace ShelfBuddy.Data;

public interface IEntityWithId
{
    Guid Id { get; set; }
}