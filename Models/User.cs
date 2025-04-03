using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
namespace TaskManager.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } //mongodb will assign this on insertion if = null
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}
