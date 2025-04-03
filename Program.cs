using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using TaskManager.Models;
using MongoDB.Driver;
using MongoDB.Bson;
using BCrypt.Net;
var builder = WebApplication.CreateBuilder(args);

// jwt authent setup
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
    };
});

builder.Services.AddAuthorization();

// mongoDB setup
var mongoClient = new MongoClient(builder.Configuration.GetConnectionString("MongoDB"));
var mongoDatabase = mongoClient.GetDatabase("TaskManagerDB");
builder.Services.AddSingleton(mongoDatabase);

//swagger setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Task Manager API", Version = "v1" });
    //aading jwt authentication to swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
var app = builder.Build();

//************ Endpoints*************
//1. User Registration
app.MapPost("/register", async (User user, IMongoDatabase db) =>
{
    var usersCollection = db.GetCollection<User>("Users");
    //checking username already exists
    if (await usersCollection.Find(u => u.Username == user.Username).AnyAsync())
        return Results.BadRequest("Username already exists");

    //hashing password before saving
    user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
    await usersCollection.InsertOneAsync(user);
    
    return Results.Ok("User registered");
});

//2.User Login (with jwt)
app.MapPost("/login", async (User loginUser, IMongoDatabase db) =>
{
    var usersCollection = db.GetCollection<User>("Users");
    var user = await usersCollection.Find(u => u.Username == loginUser.Username).FirstOrDefaultAsync();
    // validate password
    if (user == null || !BCrypt.Net.BCrypt.Verify(loginUser.Password, user.Password))
        return Results.Unauthorized();
    // generate jwt token
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]!);
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Name, user.Username)
        }),
        Expires = DateTime.UtcNow.AddHours(1),
        Issuer = builder.Configuration["Jwt:Issuer"],
        Audience = builder.Configuration["Jwt:Audience"],
        SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(key), 
            SecurityAlgorithms.HmacSha256Signature)
    };
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Results.Ok(new { Token = tokenHandler.WriteToken(token) });
});

//3. getting all tasks
app.MapGet("/tasks", async (IMongoDatabase db, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var tasksCollection = db.GetCollection<TaskItem>("Tasks");
    return Results.Ok(await tasksCollection.Find(t => t.UserId == userId).ToListAsync());
}).RequireAuthorization();

//4. creating a task 
app.MapPost("/tasks", async (TaskItem newTask, IMongoDatabase db, ClaimsPrincipal user) =>
{
    if (string.IsNullOrEmpty(newTask.Title))
        return Results.BadRequest("Title is required");

    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    newTask.UserId = userId!;

    var tasksCollection = db.GetCollection<TaskItem>("Tasks");
    await tasksCollection.InsertOneAsync(newTask);
    
    return Results.Created($"/tasks/{newTask.Id}", newTask);
}).RequireAuthorization();

//5. Update a Task
app.MapPut("/tasks/{id}", async (string id, TaskItem updatedTask, IMongoDatabase db, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var tasksCollection = db.GetCollection<TaskItem>("Tasks");

    //ensuring all tasks belng to user
    var filter = Builders<TaskItem>.Filter.Eq(t => t.Id, id) & 
                 Builders<TaskItem>.Filter.Eq(t => t.UserId, userId);

    updatedTask.Id = id; //preventing ID change
    var result = await tasksCollection.ReplaceOneAsync(filter, updatedTask);

    return result.ModifiedCount > 0 ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

//6. Deleting a task
app.MapDelete("/tasks/{id}", async (string id, IMongoDatabase db, ClaimsPrincipal user) =>
{
    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var tasksCollection = db.GetCollection<TaskItem>("Tasks");

    var filter = Builders<TaskItem>.Filter.Eq(t => t.Id, id) & 
                 Builders<TaskItem>.Filter.Eq(t => t.UserId, userId);

    var result = await tasksCollection.DeleteOneAsync(filter);
    return result.DeletedCount > 0 ? Results.Ok() : Results.NotFound();
}).RequireAuthorization();

// 7. Generating report
app.MapGet("/tasks/report", async (string frequency, IMongoDatabase db, ClaimsPrincipal user) =>
{
    // Validate frequency
    if (!new[] { "Daily", "Weekly", "Monthly" }.Contains(frequency))
        return Results.BadRequest("Frequency must be Daily, Weekly, or Monthly");

    var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    var tasksCollection = db.GetCollection<TaskItem>("Tasks");
    
    var filter = Builders<TaskItem>.Filter.Eq(t => t.UserId, userId) & 
                 Builders<TaskItem>.Filter.Eq(t => t.Frequency, frequency);
    
    return Results.Ok(await tasksCollection.Find(filter).ToListAsync());
}).RequireAuthorization();

////////////
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();

///////////
app.Run();
