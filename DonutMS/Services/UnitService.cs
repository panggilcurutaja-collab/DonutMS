using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface IUnitService
{
    Task<IEnumerable<UnitDto>> GetAllUnitsAsync();
    Task<IEnumerable<UnitDto>> GetActiveUnitsAsync();
    Task<UnitDto?> GetUnitByIdAsync(int id);
    Task<UnitDto?> GetUnitByCodeAsync(string code);
    Task<UnitDto> CreateUnitAsync(CreateUnitDto dto);
    Task<UnitDto> UpdateUnitAsync(int id, UpdateUnitDto dto);
    Task<bool> DeleteUnitAsync(int id);
}

public class UnitService : IUnitService
{
    private readonly IRepository<Unit> _unitRepository;
    private readonly IValidator<Unit> _validator;
    private readonly ILogger<UnitService> _logger;
    private readonly IMapper _mapper;

    public UnitService(
        IRepository<Unit> unitRepository,
        IValidator<Unit> validator,
        ILogger<UnitService> logger,
        IMapper mapper)
    {
        _unitRepository = unitRepository;
        _validator = validator;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IEnumerable<UnitDto>> GetAllUnitsAsync()
    {
        var units = await _unitRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<UnitDto>>(units);
    }

    public async Task<IEnumerable<UnitDto>> GetActiveUnitsAsync()
    {
        var units = await _unitRepository.FindAsync(u => u.IsActive && !u.IsDeleted);
        return _mapper.Map<IEnumerable<UnitDto>>(units);
    }

    public async Task<UnitDto?> GetUnitByIdAsync(int id)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        return unit != null ? _mapper.Map<UnitDto>(unit) : null;
    }

    public async Task<UnitDto?> GetUnitByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var unit = await _unitRepository.AsQueryable()
            .FirstOrDefaultAsync(u => u.Code == code);

        return unit != null ? _mapper.Map<UnitDto>(unit) : null;
    }

    public async Task<UnitDto> CreateUnitAsync(CreateUnitDto dto)
    {
        var existing = await _unitRepository.AsQueryable()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Code == dto.Code);
        if (existing != null)
            throw new InvalidOperationException($"Unit code '{dto.Code}' already exists");

        var unit = _mapper.Map<Unit>(dto);
        var validation = await _validator.ValidateAsync(unit);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Unit validation failed: {errors}");
        }

        await _unitRepository.AddAsync(unit);
        await _unitRepository.SaveChangesAsync();

        _logger.LogInformation($"Unit '{unit.Code}' created");
        return _mapper.Map<UnitDto>(unit);
    }

    public async Task<UnitDto> UpdateUnitAsync(int id, UpdateUnitDto dto)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        if (unit == null)
            throw new KeyNotFoundException($"Unit with ID {id} not found");

        if (!string.IsNullOrWhiteSpace(dto.Code) && !string.Equals(dto.Code, unit.Code, StringComparison.OrdinalIgnoreCase))
        {
            var duplicate = await _unitRepository.AsQueryable()
                .IgnoreQueryFilters()
                .AnyAsync(u => u.Code == dto.Code && u.Id != id);
            if (duplicate)
                throw new InvalidOperationException($"Unit code '{dto.Code}' already exists");
            unit.Code = dto.Code;
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
            unit.Name = dto.Name;

        if (dto.Description != null)
            unit.Description = dto.Description;

        if (!string.IsNullOrWhiteSpace(dto.Category))
            unit.Category = dto.Category;

        if (dto.ConversionFactor.HasValue)
            unit.ConversionFactor = dto.ConversionFactor.Value;

        if (dto.BaseUnit != null)
            unit.BaseUnit = dto.BaseUnit;

        if (dto.IsActive.HasValue)
            unit.IsActive = dto.IsActive.Value;

        var validation = await _validator.ValidateAsync(unit);
        if (!validation.IsValid)
        {
            var errors = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
            throw new ValidationException($"Unit validation failed: {errors}");
        }

        await _unitRepository.UpdateAsync(unit);
        await _unitRepository.SaveChangesAsync();

        _logger.LogInformation($"Unit '{unit.Code}' updated");
        return _mapper.Map<UnitDto>(unit);
    }

    public async Task<bool> DeleteUnitAsync(int id)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        if (unit == null)
            return false;

        unit.IsDeleted = true;
        unit.IsActive = false;
        await _unitRepository.UpdateAsync(unit);
        await _unitRepository.SaveChangesAsync();

        _logger.LogInformation($"Unit '{unit.Code}' deleted");
        return true;
    }
}
