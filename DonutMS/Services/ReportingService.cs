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
    Task<IEnumerable<TrendPointDto>> GetWeeklyMaterialTrendAsync(int weeks);
    Task<IEnumerable<TrendPointDto>> GetMonthlyMaterialTrendAsync(int months);
    Task<IEnumerable<SkuPerformanceDto>> GetSkuPerformanceAsync();
    Task<int> GetActiveBatchCountAsync();
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

    public async Task<IEnumerable<TrendPointDto>> GetWeeklyMaterialTrendAsync(int weeks)
    {
        if (weeks <= 0)
            return Array.Empty<TrendPointDto>();

        var today = DateTime.Today;
        var weekStart = StartOfWeek(today, DayOfWeek.Monday);
        var results = new List<TrendPointDto>();

        for (var i = weeks - 1; i >= 0; i--)
        {
            var periodStart = weekStart.AddDays(-7 * i).Date;
            var periodEnd = periodStart.AddDays(7).AddTicks(-1);

            var batches = await _productionRepository.GetBatchesByDateRangeAsync(periodStart, periodEnd);
            var totalCost = batches.Sum(b => b.TargetYield * 100);

            results.Add(new TrendPointDto
            {
                Label = periodStart.ToString("dd MMM"),
                Value = totalCost
            });
        }

        return results;
    }

    public async Task<IEnumerable<TrendPointDto>> GetMonthlyMaterialTrendAsync(int months)
    {
        if (months <= 0)
            return Array.Empty<TrendPointDto>();

        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var results = new List<TrendPointDto>();

        for (var i = months - 1; i >= 0; i--)
        {
            var periodStart = monthStart.AddMonths(-i);
            var periodEnd = periodStart.AddMonths(1).AddTicks(-1);

            var batches = await _productionRepository.GetBatchesByDateRangeAsync(periodStart, periodEnd);
            var totalCost = batches.Sum(b => b.TargetYield * 100);

            results.Add(new TrendPointDto
            {
                Label = periodStart.ToString("MMM yy"),
                Value = totalCost
            });
        }

        return results;
    }

    public async Task<IEnumerable<SkuPerformanceDto>> GetSkuPerformanceAsync()
    {
        var skus = await _skuRepository.GetAllAsync();
        var costs = await _skuRepository.GetCurrentCostsAsync();

        var costLookup = costs
            .Where(c => c.IsActive)
            .GroupBy(c => c.SKUId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.EffectiveDate).First());

        var results = new List<SkuPerformanceDto>();
        foreach (var sku in skus.Where(s => !s.IsDeleted))
        {
            costLookup.TryGetValue(sku.Id, out var cost);
            var hpp = cost?.TotalHPP ?? 0m;
            var profit = sku.RetailPrice - hpp;
            var marginPercent = sku.RetailPrice > 0 ? (profit / sku.RetailPrice) * 100 : 0m;

            results.Add(new SkuPerformanceDto
            {
                SKUId = sku.Id,
                Name = sku.Name,
                Code = sku.Code,
                RetailPrice = sku.RetailPrice,
                HPP = hpp,
                ProfitPerUnit = profit,
                GrossMarginPercent = marginPercent
            });
        }

        return results;
    }

    public async Task<int> GetActiveBatchCountAsync()
    {
        var batches = await _productionRepository.GetActiveBatchesAsync();
        return batches.Count();
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startDay)
    {
        var diff = (7 + (date.DayOfWeek - startDay)) % 7;
        return date.AddDays(-diff).Date;
    }
}
