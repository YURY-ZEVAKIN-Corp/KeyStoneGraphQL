using KeyStoneGraphQL.Application.Providers;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();



// Register the dynamic type provider via extension method from Application layer
builder.Services.AddDynamicTypeProvider();

// Register GraphQL server with dynamic schema configuration
builder.Services.AddGraphQLServer()
    .AddQueryType<KeyStoneGraphQL.Application.Query.Query>()
    .ConfigureSchema((sp, schemaBuilder) =>
    {
        var dynamicTypeProvider = sp.GetRequiredService<DynamicTypeProvider>();
        foreach (var type in dynamicTypeProvider.GetDynamicTypes())
        {
            schemaBuilder.AddType(type);
        }
    });


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




