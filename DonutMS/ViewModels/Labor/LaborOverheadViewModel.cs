using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;

namespace DonutMS.ViewModels;

public partial class LaborOverheadViewModel : BaseViewModel
{
    private readonly ILaborOverheadService _laborService;

    [ObservableProperty]
    private ObservableCollection<OperatorDto> operators = new();

    [ObservableProperty]
    private ObservableCollection<LaborRateDto> laborRates = new();

    [ObservableProperty]
    private ObservableCollection<BatchDto> batches = new();

    [ObservableProperty]
    private ObservableCollection<BatchLaborDto> batchLaborRecords = new();

    [ObservableProperty]
    private ObservableCollection<EquipmentDepreciationDto> equipmentItems = new();

    [ObservableProperty]
    private ObservableCollection<UtilityExpenseDto> utilityExpenses = new();

    [ObservableProperty]
    private OperatorDto? selectedOperator;

    [ObservableProperty]
    private OperatorDto? selectedRateOperator;

    [ObservableProperty]
    private BatchDto? selectedBatch;

    [ObservableProperty]
    private OperatorDto? selectedBatchOperator;

    [ObservableProperty]
    private EquipmentDepreciationDto? selectedEquipment;

    [ObservableProperty]
    private UtilityExpenseDto? selectedUtilityExpense;

    [ObservableProperty]
    private ObservableCollection<string> salaryPeriods = new();

    [ObservableProperty]
    private ObservableCollection<string> shiftTypes = new();

    [ObservableProperty]
    private ObservableCollection<string> utilityTypes = new();

    [ObservableProperty]
    private ObservableCollection<string> allocationMethods = new();

    [ObservableProperty]
    private ObservableCollection<string> depreciationMethods = new();

    [ObservableProperty]
    private string operatorName = string.Empty;

    [ObservableProperty]
    private string operatorEmployeeId = string.Empty;

    [ObservableProperty]
    private string? operatorJobTitle;

    [ObservableProperty]
    private string? operatorEmail;

    [ObservableProperty]
    private string? operatorPhoneNumber;

    [ObservableProperty]
    private string? operatorAddress;

    [ObservableProperty]
    private decimal operatorBaseSalary;

    [ObservableProperty]
    private string operatorSalaryPeriod = "Monthly";

    [ObservableProperty]
    private DateTime operatorHireDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? operatorTerminationDate;

    [ObservableProperty]
    private bool operatorIsActive = true;

    [ObservableProperty]
    private string? operatorNotes;

    [ObservableProperty]
    private decimal rateHourlyRate;

    [ObservableProperty]
    private decimal rateOvertimeMultiplier = 1.5m;

    [ObservableProperty]
    private DateTime rateEffectiveDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? rateEndDate;

    [ObservableProperty]
    private string? rateShiftType;

    [ObservableProperty]
    private string? rateNotes;

    [ObservableProperty]
    private DateTime batchLaborDate = DateTime.Today;

    [ObservableProperty]
    private decimal batchHoursWorked;

    [ObservableProperty]
    private decimal batchOvertimeHours;

    [ObservableProperty]
    private string? batchShiftType;

    [ObservableProperty]
    private string? batchLaborNotes;

    [ObservableProperty]
    private string equipmentName = string.Empty;

    [ObservableProperty]
    private string? equipmentDescription;

    [ObservableProperty]
    private decimal equipmentAcquisitionCost;

    [ObservableProperty]
    private DateTime equipmentAcquisitionDate = DateTime.Today;

    [ObservableProperty]
    private int equipmentDepreciationYears = 5;

    [ObservableProperty]
    private string equipmentDepreciationMethod = "StraightLine";

    [ObservableProperty]
    private decimal equipmentResidualValue;

    [ObservableProperty]
    private decimal equipmentMonthlyDepreciation;

    [ObservableProperty]
    private DateTime? equipmentDisposalDate;

    [ObservableProperty]
    private bool equipmentIsActive = true;

    [ObservableProperty]
    private string? equipmentNotes;

    [ObservableProperty]
    private string utilityName = string.Empty;

    [ObservableProperty]
    private string? utilityDescription;

    [ObservableProperty]
    private string utilityType = "Electricity";

    [ObservableProperty]
    private decimal utilityMonthlyAmount;

    [ObservableProperty]
    private DateTime utilityEffectiveDate = DateTime.Today;

    [ObservableProperty]
    private DateTime? utilityEndDate;

    [ObservableProperty]
    private string utilityAllocationMethod = "Percentage";

    [ObservableProperty]
    private decimal utilityAllocationValue;

    [ObservableProperty]
    private bool utilityIsActive = true;

    [ObservableProperty]
    private string? utilityNotes;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public LaborOverheadViewModel(
        ILaborOverheadService laborService,
        ILogger<LaborOverheadViewModel> logger) : base(logger)
    {
        _laborService = laborService;

        SalaryPeriods = new ObservableCollection<string> { "Monthly", "Weekly", "Daily", "Hourly" };
        ShiftTypes = new ObservableCollection<string> { "Day", "Night", "Shift A", "Shift B" };
        UtilityTypes = new ObservableCollection<string> { "Electricity", "Gas", "Water", "Rent", "Internet", "Other" };
        AllocationMethods = new ObservableCollection<string> { "Percentage", "Fixed", "PerBatch", "PerUnit" };
        DepreciationMethods = new ObservableCollection<string> { "StraightLine", "DecliningBalance" };
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var operatorList = await _laborService.GetOperatorsAsync();
            Operators = new ObservableCollection<OperatorDto>(operatorList);

            var rateList = await _laborService.GetLaborRatesAsync();
            LaborRates = new ObservableCollection<LaborRateDto>(rateList);

            var batchList = await _laborService.GetActiveBatchesAsync();
            Batches = new ObservableCollection<BatchDto>(batchList);

            var equipmentList = await _laborService.GetEquipmentDepreciationsAsync();
            EquipmentItems = new ObservableCollection<EquipmentDepreciationDto>(equipmentList);

            var utilityList = await _laborService.GetUtilityExpensesAsync();
            UtilityExpenses = new ObservableCollection<UtilityExpenseDto>(utilityList);

            if (SelectedBatch == null && Batches.Count > 0)
            {
                SelectedBatch = Batches[0];
            }

            StatusMessage = "Labor & overhead data loaded";
        }
        catch (Exception ex)
        {
            SetError($"Error loading labor & overhead data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RefreshAsync()
    {
        await LoadAsync();
    }

    [RelayCommand]
    public async Task LoadBatchLaborRecordsAsync()
    {
        try
        {
            if (SelectedBatch == null)
            {
                BatchLaborRecords = new ObservableCollection<BatchLaborDto>();
                return;
            }

            var records = await _laborService.GetBatchLaborRecordsAsync(SelectedBatch.Id);
            BatchLaborRecords = new ObservableCollection<BatchLaborDto>(records);
        }
        catch (Exception ex)
        {
            SetError($"Error loading batch labor: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task SaveOperatorAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(OperatorName) || string.IsNullOrWhiteSpace(OperatorEmployeeId))
            {
                SetError("Operator name and employee ID are required");
                return;
            }

            IsLoading = true;
            ClearError();

            if (SelectedOperator == null)
            {
                var dto = new CreateOperatorDto
                {
                    Name = OperatorName,
                    EmployeeId = OperatorEmployeeId,
                    Email = OperatorEmail,
                    PhoneNumber = OperatorPhoneNumber,
                    Address = OperatorAddress,
                    HireDate = OperatorHireDate,
                    TerminationDate = OperatorTerminationDate,
                    JobTitle = OperatorJobTitle,
                    BaseSalary = OperatorBaseSalary,
                    SalaryPeriod = OperatorSalaryPeriod,
                    IsActive = OperatorIsActive,
                    Notes = OperatorNotes
                };

                await _laborService.CreateOperatorAsync(dto);
            }
            else
            {
                var dto = new UpdateOperatorDto
                {
                    Name = OperatorName,
                    EmployeeId = OperatorEmployeeId,
                    Email = OperatorEmail,
                    PhoneNumber = OperatorPhoneNumber,
                    Address = OperatorAddress,
                    HireDate = OperatorHireDate,
                    TerminationDate = OperatorTerminationDate,
                    JobTitle = OperatorJobTitle,
                    BaseSalary = OperatorBaseSalary,
                    SalaryPeriod = OperatorSalaryPeriod,
                    IsActive = OperatorIsActive,
                    Notes = OperatorNotes
                };

                await _laborService.UpdateOperatorAsync(SelectedOperator.Id, dto);
            }

            await LoadAsync();
            ClearOperatorForm();
            StatusMessage = "Operator saved";
        }
        catch (Exception ex)
        {
            SetError($"Error saving operator: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ClearOperatorForm()
    {
        SelectedOperator = null;
        OperatorName = string.Empty;
        OperatorEmployeeId = string.Empty;
        OperatorEmail = null;
        OperatorPhoneNumber = null;
        OperatorAddress = null;
        OperatorJobTitle = null;
        OperatorBaseSalary = 0;
        OperatorSalaryPeriod = SalaryPeriods.FirstOrDefault() ?? "Monthly";
        OperatorHireDate = DateTime.Today;
        OperatorTerminationDate = null;
        OperatorIsActive = true;
        OperatorNotes = null;
    }

    [RelayCommand]
    public async Task AddLaborRateAsync()
    {
        try
        {
            if (SelectedRateOperator == null)
            {
                SetError("Please select an operator for the labor rate");
                return;
            }

            if (RateHourlyRate <= 0)
            {
                SetError("Hourly rate must be greater than 0");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new CreateLaborRateDto
            {
                OperatorId = SelectedRateOperator.Id,
                HourlyRate = RateHourlyRate,
                OvertimeMultiplier = RateOvertimeMultiplier,
                EffectiveDate = RateEffectiveDate,
                EndDate = RateEndDate,
                ShiftType = RateShiftType,
                Notes = RateNotes
            };

            await _laborService.AddLaborRateAsync(dto);
            await LoadAsync();

            RateHourlyRate = 0;
            RateOvertimeMultiplier = 1.5m;
            RateEffectiveDate = DateTime.Today;
            RateEndDate = null;
            RateShiftType = null;
            RateNotes = null;

            StatusMessage = "Labor rate added";
        }
        catch (Exception ex)
        {
            SetError($"Error adding labor rate: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task AddBatchLaborAsync()
    {
        try
        {
            if (SelectedBatch == null || SelectedBatchOperator == null)
            {
                SetError("Please select batch and operator");
                return;
            }

            if (BatchHoursWorked <= 0)
            {
                SetError("Hours worked must be greater than 0");
                return;
            }

            IsLoading = true;
            ClearError();

            var startTime = BatchLaborDate.Date;
            var totalHours = BatchHoursWorked + BatchOvertimeHours;
            var dto = new CreateBatchLaborDto
            {
                BatchId = SelectedBatch.Id,
                OperatorId = SelectedBatchOperator.Id,
                StartTime = startTime,
                EndTime = startTime.AddHours((double)totalHours),
                HoursWorked = BatchHoursWorked,
                OvertimeHours = BatchOvertimeHours,
                ShiftType = BatchShiftType,
                Notes = BatchLaborNotes
            };

            await _laborService.AddBatchLaborAsync(dto);
            await LoadBatchLaborRecordsAsync();

            BatchHoursWorked = 0;
            BatchOvertimeHours = 0;
            BatchShiftType = null;
            BatchLaborNotes = null;

            StatusMessage = "Batch labor recorded";
        }
        catch (Exception ex)
        {
            SetError($"Error recording batch labor: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveEquipmentAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EquipmentName))
            {
                SetError("Equipment name is required");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new EquipmentDepreciationDto
            {
                Id = SelectedEquipment?.Id ?? 0,
                EquipmentName = EquipmentName,
                Description = EquipmentDescription,
                AcquisitionCost = EquipmentAcquisitionCost,
                AcquisitionDate = EquipmentAcquisitionDate,
                DepreciationYears = EquipmentDepreciationYears,
                DepreciationMethod = EquipmentDepreciationMethod,
                ResidualValue = EquipmentResidualValue,
                MonthlyDepreciation = EquipmentMonthlyDepreciation,
                DisposalDate = EquipmentDisposalDate,
                IsActive = EquipmentIsActive,
                Notes = EquipmentNotes
            };

            var saved = await _laborService.SaveEquipmentDepreciationAsync(dto);
            EquipmentMonthlyDepreciation = saved.MonthlyDepreciation;
            await LoadAsync();
            ClearEquipmentForm();

            StatusMessage = "Equipment depreciation saved";
        }
        catch (Exception ex)
        {
            SetError($"Error saving equipment depreciation: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ClearEquipmentForm()
    {
        SelectedEquipment = null;
        EquipmentName = string.Empty;
        EquipmentDescription = null;
        EquipmentAcquisitionCost = 0;
        EquipmentAcquisitionDate = DateTime.Today;
        EquipmentDepreciationYears = 5;
        EquipmentDepreciationMethod = DepreciationMethods.FirstOrDefault() ?? "StraightLine";
        EquipmentResidualValue = 0;
        EquipmentMonthlyDepreciation = 0;
        EquipmentDisposalDate = null;
        EquipmentIsActive = true;
        EquipmentNotes = null;
    }

    [RelayCommand]
    public async Task SaveUtilityExpenseAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(UtilityName))
            {
                SetError("Utility name is required");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new UtilityExpenseDto
            {
                Id = SelectedUtilityExpense?.Id ?? 0,
                Name = UtilityName,
                Description = UtilityDescription,
                UtilityType = UtilityType,
                MonthlyAmount = UtilityMonthlyAmount,
                EffectiveDate = UtilityEffectiveDate,
                EndDate = UtilityEndDate,
                AllocationMethod = UtilityAllocationMethod,
                AllocationValue = UtilityAllocationValue,
                IsActive = UtilityIsActive,
                Notes = UtilityNotes
            };

            await _laborService.SaveUtilityExpenseAsync(dto);
            await LoadAsync();
            ClearUtilityForm();

            StatusMessage = "Utility expense saved";
        }
        catch (Exception ex)
        {
            SetError($"Error saving utility expense: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ClearUtilityForm()
    {
        SelectedUtilityExpense = null;
        UtilityName = string.Empty;
        UtilityDescription = null;
        UtilityType = UtilityTypes.FirstOrDefault() ?? "Electricity";
        UtilityMonthlyAmount = 0;
        UtilityEffectiveDate = DateTime.Today;
        UtilityEndDate = null;
        UtilityAllocationMethod = AllocationMethods.FirstOrDefault() ?? "Percentage";
        UtilityAllocationValue = 0;
        UtilityIsActive = true;
        UtilityNotes = null;
    }

    partial void OnSelectedOperatorChanged(OperatorDto? value)
    {
        if (value == null)
        {
            return;
        }

        OperatorName = value.Name;
        OperatorEmployeeId = value.EmployeeId;
        OperatorEmail = value.Email;
        OperatorPhoneNumber = value.PhoneNumber;
        OperatorAddress = value.Address;
        OperatorJobTitle = value.JobTitle;
        OperatorBaseSalary = value.BaseSalary;
        OperatorSalaryPeriod = value.SalaryPeriod;
        OperatorHireDate = value.HireDate;
        OperatorTerminationDate = value.TerminationDate;
        OperatorIsActive = value.IsActive;
        OperatorNotes = value.Notes;
    }

    partial void OnSelectedEquipmentChanged(EquipmentDepreciationDto? value)
    {
        if (value == null)
        {
            return;
        }

        EquipmentName = value.EquipmentName;
        EquipmentDescription = value.Description;
        EquipmentAcquisitionCost = value.AcquisitionCost;
        EquipmentAcquisitionDate = value.AcquisitionDate;
        EquipmentDepreciationYears = value.DepreciationYears;
        EquipmentDepreciationMethod = value.DepreciationMethod;
        EquipmentResidualValue = value.ResidualValue;
        EquipmentMonthlyDepreciation = value.MonthlyDepreciation;
        EquipmentDisposalDate = value.DisposalDate;
        EquipmentIsActive = value.IsActive;
        EquipmentNotes = value.Notes;
    }

    partial void OnSelectedUtilityExpenseChanged(UtilityExpenseDto? value)
    {
        if (value == null)
        {
            return;
        }

        UtilityName = value.Name;
        UtilityDescription = value.Description;
        UtilityType = value.UtilityType;
        UtilityMonthlyAmount = value.MonthlyAmount;
        UtilityEffectiveDate = value.EffectiveDate;
        UtilityEndDate = value.EndDate;
        UtilityAllocationMethod = value.AllocationMethod;
        UtilityAllocationValue = value.AllocationValue;
        UtilityIsActive = value.IsActive;
        UtilityNotes = value.Notes;
    }

    partial void OnSelectedBatchChanged(BatchDto? value)
    {
        if (value == null)
            return;

        LoadBatchLaborRecordsCommand.ExecuteAsync(null);
    }

    partial void OnEquipmentAcquisitionCostChanged(decimal value)
    {
        UpdateEquipmentMonthlyDepreciation();
    }

    partial void OnEquipmentResidualValueChanged(decimal value)
    {
        UpdateEquipmentMonthlyDepreciation();
    }

    partial void OnEquipmentDepreciationYearsChanged(int value)
    {
        UpdateEquipmentMonthlyDepreciation();
    }

    private void UpdateEquipmentMonthlyDepreciation()
    {
        if (EquipmentDepreciationYears <= 0)
        {
            EquipmentMonthlyDepreciation = 0;
            return;
        }

        var depreciable = Math.Max(0m, EquipmentAcquisitionCost - EquipmentResidualValue);
        EquipmentMonthlyDepreciation = Math.Round(depreciable / (EquipmentDepreciationYears * 12m), 2, MidpointRounding.AwayFromZero);
    }
}
