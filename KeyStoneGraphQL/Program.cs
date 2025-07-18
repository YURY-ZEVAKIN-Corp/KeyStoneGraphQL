
using Microsoft.AspNetCore.Mvc;
using KeyStoneGraphQL.Application.Query;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddGraphQLServer()
    .AddQueryType<KeyStoneGraphQL.Application.Query.Query>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

// Map GraphQL endpoint
app.MapGraphQL("/graphql");

app.Run();



namespace KeyStoneGraphQL
{
    public partial class Program { }
}


