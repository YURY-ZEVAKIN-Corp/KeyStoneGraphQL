using Microsoft.Extensions.DependencyInjection;

namespace KeyStoneGraphQL.Application.Providers
{
    public static class StartUpExtensions
    {
        public static IServiceCollection AddDynamicTypeProvider(this IServiceCollection services)
        {
            return services.AddSingleton<DynamicTypeProvider>();
        }
    }
}
