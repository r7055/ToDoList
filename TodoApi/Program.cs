using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using TodoApi;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("todo"),
    new MySqlServerVersion(new Version(8, 0, 41))));

builder.Services.AddCors(opt => opt.AddPolicy("MyPolicy", policy =>
{
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
}));

builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Bearer Authentication with JWT Token",
        Type = SecuritySchemeType.Http
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Id = "Bearer",
                    Type = ReferenceType.SecurityScheme
                }
            },
            new List<string>()
        }
    });
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "ToDo API",
        Description = "An ASP.NET Core Web API for managing ToDo items",
    });
});

var app = builder.Build();

// if (app.Environment.IsDevelopment())
// {
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });

// }

app.UseCors("MyPolicy");
app.MapGet("/",()=>"toDoListServer api is running ");
app.MapGet("/items", async (ToDoDbContext db) =>
{
    try
    {
        var items = await db.Items.ToListAsync();
        return Results.Ok(items);
    }
    catch (Exception ex)
    {
        return Results.Problem("An error occurred while retrieving items: " + ex.Message);
    }
});

app.MapPost("/items", async (ToDoDbContext db, [FromBody] Item item) =>
{
    try
    {
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return Results.Created($"/items/{item.Id}", item);
    }
    catch (Exception ex)
    {
        return Results.Problem("An error occurred while adding the item: " + ex.Message);
    }
});

app.MapPut("/items/{id}", async (ToDoDbContext db, int id) =>
{
    try
    {
        var item = await db.Items.FindAsync(id);
        if (item is null) return Results.NotFound();

        item.IsComplete=!item.IsComplete;

        System.Console.WriteLine(item.IsComplete);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem("An error occurred while updating the item: " + ex.Message);
    }
});

app.MapDelete("/items/{id}", async (ToDoDbContext db, int id) =>
{
    try
    {
        var item = await db.Items.FindAsync(id);
        if (item is null) return Results.NotFound();

        db.Items.Remove(item);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    catch (Exception ex)
    {
        return Results.Problem("An error occurred while deleting the item: " + ex.Message);
    }
});


app.Run();