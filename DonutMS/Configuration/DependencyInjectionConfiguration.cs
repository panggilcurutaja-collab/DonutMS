using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using DonutMS.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        // ViewModels - Register with proper dependency resolution
        // Note: NavigationViewModel must be registered first as it's a dependency of MainWindowViewModel
        services.AddScoped<NavigationViewModel>(sp =>
            new NavigationViewModel(
                sp.GetRequiredService<INavigationService>(),
                sp.GetRequiredService<ILogger<NavigationViewModel>>()));
        
        services.AddScoped<MainWindowViewModel>(sp =>
            new MainWindowViewModel(
                sp.GetRequiredService<INavigationService>(),
                sp.GetRequiredService<NavigationViewModel>(),
                sp.GetRequiredService<ILogger<MainWindowViewModel>>()));
        
        services.AddScoped<DashboardViewModel>(sp =>
            new DashboardViewModel(
                sp.GetRequiredService<IReportingService>(),
                sp.GetRequiredService<ICostCalculationService>(),
                sp.GetRequiredService<IInventoryService>(),
                sp.GetRequiredService<IPricingService>(),
                sp.GetRequiredService<ILogger<DashboardViewModel>>()));
        
        services.AddScoped<RecipeEditorViewModel>(sp =>
            new RecipeEditorViewModel(
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<FluentValidation.IValidator<DonutMS.Data.Entities.Recipe>>(),
                sp.GetRequiredService<ILogger<RecipeEditorViewModel>>()));
        
        services.AddScoped<InventoryManagerViewModel>(sp =>
            new InventoryManagerViewModel(
                sp.GetRequiredService<IInventoryService>(),
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<ILogger<InventoryManagerViewModel>>()));
        
        services.AddScoped<BatchManagementViewModel>(sp =>
            new BatchManagementViewModel(
                sp.GetRequiredService<IProductionService>(),
                sp.GetRequiredService<IRecipeService>(),
                sp.GetRequiredService<IInventoryService>(),
                sp.GetRequiredService<ILogger<BatchManagementViewModel>>()));
        
        services.AddScoped<PricingCalculatorViewModel>(sp =>
            new PricingCalculatorViewModel(
                sp.GetRequiredService<IPricingService>(),
                sp.GetRequiredService<ICostCalculationService>(),
                sp.GetRequiredService<ISKURepository>(),
                sp.GetRequiredService<ILogger<PricingCalculatorViewModel>>()));
        
        services.AddScoped<IngredientsViewModel>(sp =>
            new IngredientsViewModel(
                sp.GetRequiredService<IIngredientService>(),
                sp.GetRequiredService<IUnitConversionService>(),
                sp.GetRequiredService<ILogger<IngredientsViewModel>>()));

        return services;
    }
}




