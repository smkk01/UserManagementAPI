using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "User Management API", Version = "v1" });
});

var app = builder.Build();

// 1. Error handling first
app.UseErrorHandling();

// 2. Authentication next
app.UseTokenAuthentication();

// 3. Logging last
app.UseRequestResponseLogging();


// Global exception handler
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync("An unexpected error occurred.");
    });
});

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "User Management API v1");
});

// In-memory user list
var users = new List<User>();

// GET - Retrieve all users (with optional pagination)
app.MapGet("/users", (int? skip, int? take) =>
{
    var result = users.Skip(skip ?? 0).Take(take ?? users.Count).ToList();
    return Results.Ok(result);
});

// GET - Retrieve user by ID
app.MapGet("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is not null ? Results.Ok(user) : Results.NotFound($"User with ID {id} not found.");
});

// POST - Add new user with validation
app.MapPost("/users", (User newUser) =>
{
    if (string.IsNullOrWhiteSpace(newUser.Username))
        return Results.BadRequest("Username cannot be empty.");
    if (newUser.UserAge <= 0)
        return Results.BadRequest("Age must be positive.");

    newUser = newUser with { Id = users.Count > 0 ? users[^1].Id + 1 : 1 };
    users.Add(newUser);
    return Results.Created($"/users/{newUser.Id}", newUser);
});

// PUT - Update user
app.MapPut("/users/{id}", (int id, User updatedUser) =>
{
    var index = users.FindIndex(u => u.Id == id);
    if (index < 0) return Results.NotFound($"User with ID {id} not found.");

    if (string.IsNullOrWhiteSpace(updatedUser.Username))
        return Results.BadRequest("Username cannot be empty.");
    if (updatedUser.UserAge <= 0)
        return Results.BadRequest("Age must be positive.");

    var user = users[index] with { Username = updatedUser.Username, UserAge = updatedUser.UserAge };
    users[index] = user;
    return Results.Ok(user);
});

// DELETE - Remove user
app.MapDelete("/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user is null) return Results.NotFound($"User with ID {id} not found.");

    users.Remove(user);
    return Results.Ok(user);
});

app.Run();

record User(int Id, string Username, int UserAge);
