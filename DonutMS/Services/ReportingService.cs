using Microsoft.Extensions.Logging;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

// Foundation for PHASE 5 reporting & analytics
public interface IReportingService
{
    Task<decimal> GetAverageHPPAsync(DateTime fromDate, DateTime toDate);
    Task<decimal> GetTotalMaterialCostAsync(DateTime fromDate, DateTime toDate);
    Task<IEnumerable<(string SKU, decimal HPP, decimal Margin)>> GetSKUCostAnalysisAsync();
    Task<IEnumerable<(string SKU, decimal TotalProfit)>> GetProfitBySKUAsync(DateTime fromDate, DateTime toDate);
    Task<(decimal TargetYield, decimal ActualYield, decimal WastePercent)> GetProductionMetricsAsync(DateTime fromDate, DateTime toDate);
}

public class ReportingService : IReportingService
{
    private readonly ISKURepository _skuRepository;
    private readonly IProductionRepository _productionRepository;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        ISKURepository skuRepository,
        IProductionRepository productionRepository,
        Microsoft.Extensions.Logging.ILogger<ReportingService> logger)
    {
        _skuRepository = skuRepository;
        _productionRepository = productionRepository;
        _logger = logger;
    }

    public async Task<decimal> GetAverageHPPAsync(DateTime fromDate, DateTime toDate)
    {
        var costs = await _skuRepository.GetCurrentCostsAsync();
        if (!costs.Any())
            return 0;

        return costs.Average(c => c.TotalHPP);
    }

    public async Task<decimal> GetTotalMaterialCostAsync(DateTime fromDate, DateTime toDate)
    {
        var batches = await _productionRepository.GetBatchesByDateRangeAsync(fromDate, toDate);
        return batches.Sum(b => b.TargetYield * 100); // Simplified calculation
    }

    public async Task<IEnumerable<(string SKU, decimal HPP, decimal Margin)>> GetSKUCostAnalysisAsync()
    {
        var skus = await _skuRepository.GetAllAsync();
        var costs = await _skuRepository.GetCurrentCostsAsync();

        return skus
            .Join(costs, s => s.Id, c => c.SKUId, (s, c) => 
                (s.Code, c.TotalHPP, c.GrossMargin))
            .ToList();
    }

    public async Task<IEnumerable<(string SKU, decimal TotalProfit)>> GetProfitBySKUAsync(DateTime fromDate, DateTime toDate)
    {
        // Implementation would aggregate batches by SKU and calculate profit
        return Enumerable.Empty<(string, decimal)>();
    }

    public async Task<(decimal TargetYield, decimal ActualYield, decimal WastePercent)> GetProductionMetricsAsync(DateTime fromDate, DateTime toDate)
    {
        var batches = await _productionRepository.GetBatchesByDateRangeAsync(fromDate, toDate);

        var targetYield = batches.Sum(b => b.TargetYield);
        var actualYield = batches.Sum(b => b.ActualYield ?? 0);
        var wastePercent = targetYield > 0 ? ((targetYield - actualYield) / targetYield) * 100 : 0;

        return (targetYield, actualYield, wastePercent);
    }
}
