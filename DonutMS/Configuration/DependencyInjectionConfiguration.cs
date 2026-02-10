using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using DonutMS.Configuration;
using Microsoft.EntityFrameworkCore;
using DonutMS.Data.DbContext;
using DonutMS.Data.Repositories;
using DonutMS.Services;
using DonutMS.ViewModels;

namespace DonutMS.Configuration;

public static class DependencyInjectionConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, string? connectionString = null)
    {
        connectionString ??= "Data Source=donutms.db";

        // Database
        services.AddDbContext<DonutMSDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        // AutoMapper
        services.AddAutoMapper(typeof(AutoMapperProfile));

        // Validation
        services.AddValidation();

        // Database Initialization
        services.AddScoped<DonutMS.Data.DbContext.DatabaseInitializer>();

        // Generic Repository
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        // Specialized Repositories
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IProductionRepository, ProductionRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISKURepository, SKURepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();

        // Business Services
        services.AddScoped<IRecipeService, RecipeService>();
        services.AddScoped<IIngredientService, IngredientService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<ICostCalculationService, CostCalculationService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IUnitConversionService, UnitConversionService>();

        // Navigation & UI Services
        services.AddScoped<INavigationService, NavigationService>();

        // ViewModels
        services.AddScoped<MainWindowViewModel>();
        services.AddScoped<NavigationViewModel>();
        services.AddScoped<DashboardViewModel>();
        services.AddScoped<RecipeEditorViewModel>();
        services.AddScoped<InventoryManagerViewModel>();
        services.AddScoped<BatchManagementViewModel>();
        services.AddScoped<PricingCalculatorViewModel>();
        services.AddScoped<IngredientsViewModel>();

        return services;
    }
}



