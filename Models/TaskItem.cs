using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace TaskManager.Models;

public class TaskItem
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime DueDate { get; set; }
    public string Frequency { get; set; } = "Daily"; //dail-weekly-Monthly
    public string UserId { get; set; } = ""; //links task to user
}
