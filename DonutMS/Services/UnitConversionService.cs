using Microsoft.Extensions.Logging;
using AutoMapper;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;
using DonutMS.Core.Utils;

namespace DonutMS.Services;

public interface IUnitConversionService
{
    Task<decimal> ConvertAsync(decimal quantity, int fromUnitId, int toUnitId);
    Task<bool> AreUnitsCompatibleAsync(int unitId1, int unitId2);
    Task<IEnumerable<UnitDto>> GetAllUnitsAsync();
}

public class UnitConversionService : IUnitConversionService
{
    private readonly IRepository<DonutMS.Data.Entities.Unit> _unitRepository;
    private readonly ILogger<UnitConversionService> _logger;
    private readonly IMapper _mapper;

    public UnitConversionService(IRepository<DonutMS.Data.Entities.Unit> unitRepository, ILogger<UnitConversionService> logger, IMapper mapper)
    {
        _unitRepository = unitRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<decimal> ConvertAsync(decimal quantity, int fromUnitId, int toUnitId)
    {
        if (fromUnitId == toUnitId)
            return quantity;

        var fromUnit = await _unitRepository.GetByIdAsync(fromUnitId);
        var toUnit = await _unitRepository.GetByIdAsync(toUnitId);

        if (fromUnit == null || toUnit == null)
            throw new KeyNotFoundException("One or both units not found");

        if (fromUnit.Category != toUnit.Category)
            throw new InvalidOperationException($"Cannot convert between {fromUnit.Category} and {toUnit.Category}");

        if (fromUnit.Category == "Weight")
            return UnitConverter.ConvertWeight(quantity, fromUnit.Code, toUnit.Code);
        else if (fromUnit.Category == "Volume")
            return UnitConverter.ConvertVolume(quantity, fromUnit.Code, toUnit.Code);

        throw new InvalidOperationException($"Conversion not supported for category {fromUnit.Category}");
    }

    public async Task<bool> AreUnitsCompatibleAsync(int unitId1, int unitId2)
    {
        var unit1 = await _unitRepository.GetByIdAsync(unitId1);
        var unit2 = await _unitRepository.GetByIdAsync(unitId2);

        if (unit1 == null || unit2 == null)
            return false;

        return unit1.Category == unit2.Category;
    }

    public async Task<IEnumerable<UnitDto>> GetAllUnitsAsync()
    {
        var units = await _unitRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<UnitDto>>(units);
    }
}
