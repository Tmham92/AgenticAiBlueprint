using AgenticAiBlueprint.Api;
using AgenticAiBlueprint.Api.Persistence;
using AgenticAiBlueprint.Contracts.Interfaces;
using AgenticAiBlueprint.Contracts.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
});

builder.Services.AddAgenticCore(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AgenticDbContext>>();
    await using var db = await dbContextFactory.CreateDbContextAsync();
    await db.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseHttpsRedirection();

app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }))
    .WithName("GetHealth");

app.MapPost("/api/agent/execute", async (AgentGoal goal, IAgentOrchestrator orchestrator, CancellationToken cancellationToken) =>
{
    var result = await orchestrator.ExecuteGoalAsync(goal, cancellationToken);
    return Results.Ok(result);
})
.WithName("ExecuteGoal");

app.MapPost("/api/controltower/chat", async (ControlTowerChatRequest request, IControlTowerChatService chatService, CancellationToken cancellationToken) =>
{
    var response = await chatService.ChatAsync(request, cancellationToken);
    return Results.Ok(response);
})
.WithName("ControlTowerChat");

app.MapGet("/api/controltower", async (IControlTowerChatService chatService, CancellationToken cancellationToken) =>
{
    var dashboard = await chatService.GetDashboardAsync(cancellationToken);
    return Results.Ok(dashboard);
})
.WithName("GetControlTowerDashboard");

app.MapGet("/api/interactions", async (IOrganizationalMemoryService memoryService, CancellationToken cancellationToken) =>
{
    var interactions = await memoryService.GetRecentInteractionsAsync(cancellationToken: cancellationToken);
    return Results.Ok(interactions);
})
.WithName("GetInteractions");

app.MapGet("/api/recommendations", async (IExecutiveRecommendationAgent recommendationAgent, CancellationToken cancellationToken) =>
{
    var recommendations = await recommendationAgent.GenerateRecommendationsAsync(cancellationToken);
    return Results.Ok(recommendations);
})
.WithName("GetRecommendations");

app.Run();

public partial class Program;
