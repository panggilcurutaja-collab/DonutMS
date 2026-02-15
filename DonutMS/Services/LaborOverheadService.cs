using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DonutMS.Data.Entities;
using DonutMS.Data.Repositories;
using DonutMS.Models.DTOs;

namespace DonutMS.Services;

public interface ILaborOverheadService
{
    Task<IEnumerable<OperatorDto>> GetOperatorsAsync(bool includeInactive = true);
    Task<OperatorDto> CreateOperatorAsync(CreateOperatorDto dto);
    Task<OperatorDto?> UpdateOperatorAsync(int id, UpdateOperatorDto dto);
    Task<IEnumerable<LaborRateDto>> GetLaborRatesAsync(int? operatorId = null);
    Task<LaborRateDto> AddLaborRateAsync(CreateLaborRateDto dto);
    Task<IEnumerable<BatchDto>> GetActiveBatchesAsync();
    Task<IEnumerable<BatchLaborDto>> GetBatchLaborRecordsAsync(int batchId);
    Task<BatchLaborDto> AddBatchLaborAsync(CreateBatchLaborDto dto);
    Task<IEnumerable<EquipmentDepreciationDto>> GetEquipmentDepreciationsAsync();
    Task<EquipmentDepreciationDto> SaveEquipmentDepreciationAsync(EquipmentDepreciationDto dto);
    Task<IEnumerable<UtilityExpenseDto>> GetUtilityExpensesAsync();
    Task<UtilityExpenseDto> SaveUtilityExpenseAsync(UtilityExpenseDto dto);
}

public class LaborOverheadService : ILaborOverheadService
{
    private readonly IRepository<Operator> _operatorRepository;
    private readonly IRepository<LaborRate> _laborRateRepository;
    private readonly IRepository<BatchLabor> _batchLaborRepository;
    private readonly IProductionRepository _productionRepository;
    private readonly IRepository<EquipmentDepreciation> _equipmentRepository;
    private readonly IRepository<UtilityExpense> _utilityRepository;
    private readonly ILogger<LaborOverheadService> _logger;
    private readonly IMapper _mapper;

    public LaborOverheadService(
        IRepository<Operator> operatorRepository,
        IRepository<LaborRate> laborRateRepository,
        IRepository<BatchLabor> batchLaborRepository,
        IProductionRepository productionRepository,
        IRepository<EquipmentDepreciation> equipmentRepository,
        IRepository<UtilityExpense> utilityRepository,
        ILogger<LaborOverheadService> logger,
        IMapper mapper)
    {
        _operatorRepository = operatorRepository;
        _laborRateRepository = laborRateRepository;
        _batchLaborRepository = batchLaborRepository;
        _productionRepository = productionRepository;
        _equipmentRepository = equipmentRepository;
        _utilityRepository = utilityRepository;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<IEnumerable<OperatorDto>> GetOperatorsAsync(bool includeInactive = true)
    {
        var query = _operatorRepository
            .AsQueryable()
            .Where(o => !o.IsDeleted);

        if (!includeInactive)
            query = query.Where(o => o.IsActive);

        var operators = await query
            .OrderBy(o => o.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<OperatorDto>>(operators);
    }

    public async Task<OperatorDto> CreateOperatorAsync(CreateOperatorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.EmployeeId))
            throw new InvalidOperationException("Operator name and employee ID are required");

        var entity = _mapper.Map<Operator>(dto);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _operatorRepository.AddAsync(entity);
        await _operatorRepository.SaveChangesAsync();

        _logger.LogInformation($"Operator '{entity.Name}' created");
        return _mapper.Map<OperatorDto>(entity);
    }

    public async Task<OperatorDto?> UpdateOperatorAsync(int id, UpdateOperatorDto dto)
    {
        var entity = await _operatorRepository.GetByIdAsync(id);
        if (entity == null || entity.IsDeleted)
            return null;

        if (dto.Name != null)
            entity.Name = dto.Name;
        if (dto.EmployeeId != null)
            entity.EmployeeId = dto.EmployeeId;
        if (dto.Email != null)
            entity.Email = dto.Email;
        if (dto.PhoneNumber != null)
            entity.PhoneNumber = dto.PhoneNumber;
        if (dto.Address != null)
            entity.Address = dto.Address;
        if (dto.HireDate.HasValue)
            entity.HireDate = dto.HireDate.Value;
        if (dto.TerminationDate.HasValue)
            entity.TerminationDate = dto.TerminationDate.Value;
        if (dto.JobTitle != null)
            entity.JobTitle = dto.JobTitle;
        if (dto.BaseSalary.HasValue)
            entity.BaseSalary = dto.BaseSalary.Value;
        if (dto.SalaryPeriod != null)
            entity.SalaryPeriod = dto.SalaryPeriod;
        if (dto.IsActive.HasValue)
            entity.IsActive = dto.IsActive.Value;
        if (dto.Notes != null)
            entity.Notes = dto.Notes;

        entity.UpdatedAt = DateTime.UtcNow;

        await _operatorRepository.UpdateAsync(entity);
        await _operatorRepository.SaveChangesAsync();

        _logger.LogInformation($"Operator '{entity.Name}' updated");
        return _mapper.Map<OperatorDto>(entity);
    }

    public async Task<IEnumerable<LaborRateDto>> GetLaborRatesAsync(int? operatorId = null)
    {
        var query = _laborRateRepository
            .AsQueryable()
            .Include(r => r.Operator)
            .Where(r => !r.IsDeleted);

        if (operatorId.HasValue)
            query = query.Where(r => r.OperatorId == operatorId.Value);

        var rates = await query
            .OrderByDescending(r => r.EffectiveDate)
            .ToListAsync();

        return _mapper.Map<IEnumerable<LaborRateDto>>(rates);
    }

    public async Task<LaborRateDto> AddLaborRateAsync(CreateLaborRateDto dto)
    {
        if (dto.OperatorId <= 0)
            throw new InvalidOperationException("Operator must be selected");

        if (dto.HourlyRate <= 0)
            throw new InvalidOperationException("Hourly rate must be greater than 0");

        var operatorEntity = await _operatorRepository.GetByIdAsync(dto.OperatorId);
        if (operatorEntity == null || operatorEntity.IsDeleted)
            throw new InvalidOperationException("Operator not found");

        var existingRates = await _laborRateRepository
            .AsQueryable()
            .Where(r => r.OperatorId == dto.OperatorId && r.IsActive && !r.IsDeleted)
            .ToListAsync();

        foreach (var rate in existingRates)
        {
            rate.IsActive = false;
            rate.EndDate ??= dto.EffectiveDate.AddDays(-1);
            rate.UpdatedAt = DateTime.UtcNow;
            await _laborRateRepository.UpdateAsync(rate);
        }

        var entity = _mapper.Map<LaborRate>(dto);
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _laborRateRepository.AddAsync(entity);
        await _laborRateRepository.SaveChangesAsync();

        _logger.LogInformation($"Labor rate added for operator {dto.OperatorId}");
        return _mapper.Map<LaborRateDto>(entity);
    }

    public async Task<IEnumerable<BatchDto>> GetActiveBatchesAsync()
    {
        var batches = await _productionRepository.GetActiveBatchesAsync();
        return _mapper.Map<IEnumerable<BatchDto>>(batches);
    }

    public async Task<IEnumerable<BatchLaborDto>> GetBatchLaborRecordsAsync(int batchId)
    {
        var records = await _batchLaborRepository
            .AsQueryable()
            .Include(bl => bl.Operator)
            .Include(bl => bl.Batch)
            .Where(bl => bl.BatchId == batchId && !bl.IsDeleted)
            .OrderByDescending(bl => bl.StartTime)
            .ToListAsync();

        return _mapper.Map<IEnumerable<BatchLaborDto>>(records);
    }

    public async Task<BatchLaborDto> AddBatchLaborAsync(CreateBatchLaborDto dto)
    {
        if (dto.BatchId <= 0 || dto.OperatorId <= 0)
            throw new InvalidOperationException("Batch and operator must be selected");

        if (dto.HoursWorked <= 0)
            throw new InvalidOperationException("Hours worked must be greater than 0");

        var batch = await _productionRepository.GetByIdAsync(dto.BatchId);
        if (batch == null || batch.IsDeleted)
            throw new InvalidOperationException("Batch not found");

        var operatorEntity = await _operatorRepository.GetByIdAsync(dto.OperatorId);
        if (operatorEntity == null || operatorEntity.IsDeleted)
            throw new InvalidOperationException("Operator not found");

        var entity = _mapper.Map<BatchLabor>(dto);

        if (entity.StartTime == default)
        {
            entity.StartTime = DateTime.UtcNow;
        }

        if (entity.EndTime == default)
        {
            var totalHours = dto.HoursWorked + dto.OvertimeHours;
            entity.EndTime = entity.StartTime.AddHours((double)totalHours);
        }

        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _batchLaborRepository.AddAsync(entity);
        await _batchLaborRepository.SaveChangesAsync();

        _logger.LogInformation($"Batch labor record added for batch {dto.BatchId}");
        return _mapper.Map<BatchLaborDto>(entity);
    }

    public async Task<IEnumerable<EquipmentDepreciationDto>> GetEquipmentDepreciationsAsync()
    {
        var items = await _equipmentRepository
            .AsQueryable()
            .Where(e => !e.IsDeleted)
            .OrderBy(e => e.EquipmentName)
            .ToListAsync();

        return _mapper.Map<IEnumerable<EquipmentDepreciationDto>>(items);
    }

    public async Task<EquipmentDepreciationDto> SaveEquipmentDepreciationAsync(EquipmentDepreciationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.EquipmentName))
            throw new InvalidOperationException("Equipment name is required");

        var monthly = CalculateMonthlyDepreciation(dto.AcquisitionCost, dto.ResidualValue, dto.DepreciationYears);

        EquipmentDepreciation? entity = null;
        if (dto.Id > 0)
        {
            entity = await _equipmentRepository.GetByIdAsync(dto.Id);
        }

        if (entity == null)
        {
            entity = _mapper.Map<EquipmentDepreciation>(dto);
            entity.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            entity.EquipmentName = dto.EquipmentName;
            entity.Description = dto.Description;
            entity.AcquisitionCost = dto.AcquisitionCost;
            entity.AcquisitionDate = dto.AcquisitionDate;
            entity.DepreciationYears = dto.DepreciationYears;
            entity.DepreciationMethod = dto.DepreciationMethod;
            entity.ResidualValue = dto.ResidualValue;
            entity.DisposalDate = dto.DisposalDate;
            entity.IsActive = dto.IsActive;
            entity.Notes = dto.Notes;
        }

        entity.MonthlyDepreciation = monthly;
        entity.UpdatedAt = DateTime.UtcNow;

        if (dto.Id > 0)
        {
            await _equipmentRepository.UpdateAsync(entity);
        }
        else
        {
            await _equipmentRepository.AddAsync(entity);
        }

        await _equipmentRepository.SaveChangesAsync();

        _logger.LogInformation($"Equipment depreciation saved: {entity.EquipmentName}");
        return _mapper.Map<EquipmentDepreciationDto>(entity);
    }

    public async Task<IEnumerable<UtilityExpenseDto>> GetUtilityExpensesAsync()
    {
        var items = await _utilityRepository
            .AsQueryable()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.Name)
            .ToListAsync();

        return _mapper.Map<IEnumerable<UtilityExpenseDto>>(items);
    }

    public async Task<UtilityExpenseDto> SaveUtilityExpenseAsync(UtilityExpenseDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Utility name is required");

        UtilityExpense? entity = null;
        if (dto.Id > 0)
        {
            entity = await _utilityRepository.GetByIdAsync(dto.Id);
        }

        if (entity == null)
        {
            entity = _mapper.Map<UtilityExpense>(dto);
            entity.CreatedAt = DateTime.UtcNow;
        }
        else
        {
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.UtilityType = dto.UtilityType;
            entity.MonthlyAmount = dto.MonthlyAmount;
            entity.EffectiveDate = dto.EffectiveDate;
            entity.EndDate = dto.EndDate;
            entity.AllocationMethod = dto.AllocationMethod;
            entity.AllocationValue = dto.AllocationValue;
            entity.IsActive = dto.IsActive;
            entity.Notes = dto.Notes;
        }

        entity.UpdatedAt = DateTime.UtcNow;

        if (dto.Id > 0)
        {
            await _utilityRepository.UpdateAsync(entity);
        }
        else
        {
            await _utilityRepository.AddAsync(entity);
        }

        await _utilityRepository.SaveChangesAsync();

        _logger.LogInformation($"Utility expense saved: {entity.Name}");
        return _mapper.Map<UtilityExpenseDto>(entity);
    }

    private static decimal CalculateMonthlyDepreciation(decimal acquisitionCost, decimal residualValue, int years)
    {
        if (years <= 0)
            return 0m;

        var depreciable = Math.Max(0m, acquisitionCost - residualValue);
        return Math.Round(depreciable / (years * 12m), 2, MidpointRounding.AwayFromZero);
    }
}
