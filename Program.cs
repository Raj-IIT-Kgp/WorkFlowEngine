using System.Collections.Concurrent;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// --- START: ADD CORS POLICY ---
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});
// --- END: ADD CORS POLICY ---

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// --- START: USE CORS POLICY ---
// This line must be added here
app.UseCors(MyAllowSpecificOrigins);
// --- END: USE CORS POLICY ---

var definitionsDb = new ConcurrentDictionary<string, WorkflowDefinition>();
var instancesDb = new ConcurrentDictionary<string, WorkflowInstance>();

// (The rest of your API endpoint code remains exactly the same below)

app.MapPost("/definitions", (WorkflowDefinition definition) =>
{
    var initialStates = definition.States.Count(s => s.IsInitial);
    if (initialStates != 1)
    {
        return Results.BadRequest("A workflow definition must have exactly one initial state.");
    }

    if (!definitionsDb.TryAdd(definition.Id, definition))
    {
        return Results.BadRequest($"Definition ID '{definition.Id}' already exists.");
    }

    return Results.Created($"/definitions/{definition.Id}", definition);
})
.WithSummary("Define a new workflow.")
.WithDescription("Creates a new workflow definition with its states and actions.");

app.MapPost("/instances", ([FromBody] StartInstanceRequest request) =>
{
    if (!definitionsDb.TryGetValue(request.DefinitionId, out var definition))
    {
        return Results.NotFound($"Workflow definition '{request.DefinitionId}' not found.");
    }

    var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
    if (initialState is null)
    {
        return Results.Problem("Definition is invalid: no initial state found.");
    }

    var instance = new WorkflowInstance(Guid.NewGuid().ToString(), request.DefinitionId, initialState.Id);
    instancesDb.TryAdd(instance.InstanceId, instance);

    return Results.Ok(instance);
})
.WithSummary("Start a new workflow instance.")
.WithDescription("Creates and starts a new instance of a specified workflow definition.");

app.MapPost("/instances/{instanceId}/execute", (string instanceId, [FromBody] ExecuteActionRequest request) =>
{
    if (!instancesDb.TryGetValue(instanceId, out var instance))
    {
        return Results.NotFound($"Instance '{instanceId}' not found.");
    }

    if (!definitionsDb.TryGetValue(instance.DefinitionId, out var definition))
    {
        return Results.Problem($"Could not find definition '{instance.DefinitionId}' associated with instance '{instanceId}'.");
    }

    var actionToExecute = definition.Actions.FirstOrDefault(a => a.Id == request.ActionId);

    if (actionToExecute is null || !actionToExecute.Enabled)
    {
        return Results.BadRequest($"Action '{request.ActionId}' not found or is disabled.");
    }

    if (!actionToExecute.FromStates.Contains(instance.CurrentState))
    {
        return Results.BadRequest($"Action '{request.ActionId}' cannot be executed from current state '{instance.CurrentState}'.");
    }

    var targetState = definition.States.FirstOrDefault(s => s.Id == actionToExecute.ToState);
    if (targetState is null || !targetState.Enabled)
    {
        return Results.BadRequest($"Target state '{actionToExecute.ToState}' not found or is disabled.");
    }

    var updatedInstance = instance with { CurrentState = actionToExecute.ToState };
    instancesDb[instanceId] = updatedInstance;

    return Results.Ok(updatedInstance);
})
.WithSummary("Execute an action on an instance.")
.WithDescription("Moves a workflow instance from one state to another after validating the action.");

app.MapGet("/definitions", () => Results.Ok(definitionsDb.Values))
.WithSummary("List all workflow definitions.");

app.MapGet("/instances", () => Results.Ok(instancesDb.Values))
.WithSummary("List all running workflow instances.");

app.MapGet("/instances/{instanceId}", (string instanceId) =>
{
    return instancesDb.TryGetValue(instanceId, out var instance)
        ? Results.Ok(instance)
        : Results.NotFound();
})
.WithSummary("Get a specific workflow instance.");


app.Run();

public record State(string Id, bool IsInitial, bool IsFinal, bool Enabled);
public record Action(string Id, List<string> FromStates, string ToState, bool Enabled);
public record WorkflowDefinition(string Id, List<State> States, List<Action> Actions);
public record WorkflowInstance(string InstanceId, string DefinitionId, string CurrentState);

public record StartInstanceRequest(string DefinitionId);
public record ExecuteActionRequest(string ActionId);