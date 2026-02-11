using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using DonutMS.Core.MVVM;
using DonutMS.Models.DTOs;
using DonutMS.Services;
using DonutMS.Data.Repositories;

namespace DonutMS.ViewModels;

public partial class PromoManagerViewModel : BaseViewModel
{
    private readonly IPromoService _promoService;
    private readonly IPricingService _pricingService;
    private readonly ISKURepository _skuRepository;

    [ObservableProperty]
    private ObservableCollection<DiscountDto> discounts = new();

    [ObservableProperty]
    private ObservableCollection<BundlePackageDto> bundles = new();

    [ObservableProperty]
    private ObservableCollection<SKUDto> skus = new();

    [ObservableProperty]
    private DiscountDto? selectedDiscount;

    [ObservableProperty]
    private BundlePackageDto? selectedBundle;

    [ObservableProperty]
    private SKUDto? selectedBundleSku;

    [ObservableProperty]
    private SKUDto? selectedPromoSku;

    [ObservableProperty]
    private DiscountDto? selectedPromoDiscount;

    [ObservableProperty]
    private BundlePackageDto? selectedPromoBundle;

    [ObservableProperty]
    private ObservableCollection<string> discountTypes = new();

    [ObservableProperty]
    private ObservableCollection<string> applicableForOptions = new();

    [ObservableProperty]
    private string discountName = string.Empty;

    [ObservableProperty]
    private string? discountDescription;

    [ObservableProperty]
    private string discountType = "Percentage";

    [ObservableProperty]
    private decimal discountValue;

    [ObservableProperty]
    private DateTime discountStartDate = DateTime.Today;

    [ObservableProperty]
    private DateTime discountEndDate = DateTime.Today.AddMonths(1);

    [ObservableProperty]
    private string? discountApplicableFor;

    [ObservableProperty]
    private int? discountMinimumQuantity;

    [ObservableProperty]
    private bool discountIsActive = true;

    [ObservableProperty]
    private string? discountNotes;

    [ObservableProperty]
    private string bundleName = string.Empty;

    [ObservableProperty]
    private string? bundleDescription;

    [ObservableProperty]
    private decimal bundlePrice;

    [ObservableProperty]
    private int bundleQuantity = 6;

    [ObservableProperty]
    private bool bundleIsActive = true;

    [ObservableProperty]
    private string? bundleNotes;

    [ObservableProperty]
    private int promoQuantity = 1;

    [ObservableProperty]
    private decimal promoBasePrice;

    [ObservableProperty]
    private decimal discountedUnitPrice;

    [ObservableProperty]
    private decimal discountedTotal;

    [ObservableProperty]
    private decimal discountGrossMargin;

    [ObservableProperty]
    private decimal discountNetMargin;

    [ObservableProperty]
    private decimal bundleTotalPrice;

    [ObservableProperty]
    private decimal bundleEffectiveUnitPrice;

    [ObservableProperty]
    private decimal bundleGrossMargin;

    [ObservableProperty]
    private decimal bundleNetMargin;

    [ObservableProperty]
    private decimal savings;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    public PromoManagerViewModel(
        IPromoService promoService,
        IPricingService pricingService,
        ISKURepository skuRepository,
        ILogger<PromoManagerViewModel> logger) : base(logger)
    {
        _promoService = promoService;
        _pricingService = pricingService;
        _skuRepository = skuRepository;

        DiscountTypes = new ObservableCollection<string> { "Percentage", "Fixed", "Volume" };
        ApplicableForOptions = new ObservableCollection<string> { "All", "SKU", "Category" };
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        try
        {
            IsLoading = true;
            ClearError();

            var discounts = await _promoService.GetDiscountsAsync();
            Discounts = new ObservableCollection<DiscountDto>(discounts);

            var bundles = await _promoService.GetBundlesAsync();
            Bundles = new ObservableCollection<BundlePackageDto>(bundles);

            var skuList = await _skuRepository.GetAllAsync();
            Skus = new ObservableCollection<SKUDto>(skuList.Select(s => new SKUDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                RetailPrice = s.RetailPrice
            }));

            StatusMessage = "Promo data loaded";
        }
        catch (Exception ex)
        {
            SetError($"Error loading promo data: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task SaveDiscountAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(DiscountName))
            {
                SetError("Discount name is required");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new DiscountDto
            {
                Id = SelectedDiscount?.Id ?? 0,
                Name = DiscountName,
                Description = DiscountDescription,
                DiscountType = DiscountType,
                DiscountValue = DiscountValue,
                StartDate = DiscountStartDate,
                EndDate = DiscountEndDate,
                ApplicableFor = DiscountApplicableFor,
                MinimumQuantity = DiscountMinimumQuantity,
                IsActive = DiscountIsActive,
                Notes = DiscountNotes
            };

            await _promoService.SaveDiscountAsync(dto);
            await LoadAsync();
            ClearDiscountForm();

            StatusMessage = "Discount saved";
        }
        catch (Exception ex)
        {
            SetError($"Error saving discount: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ClearDiscountForm()
    {
        SelectedDiscount = null;
        DiscountName = string.Empty;
        DiscountDescription = null;
        DiscountType = DiscountTypes.FirstOrDefault() ?? "Percentage";
        DiscountValue = 0;
        DiscountStartDate = DateTime.Today;
        DiscountEndDate = DateTime.Today.AddMonths(1);
        DiscountApplicableFor = null;
        DiscountMinimumQuantity = null;
        DiscountIsActive = true;
        DiscountNotes = null;
    }

    [RelayCommand]
    public async Task SaveBundleAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(BundleName))
            {
                SetError("Bundle name is required");
                return;
            }

            if (SelectedBundleSku == null)
            {
                SetError("Select SKU for bundle");
                return;
            }

            IsLoading = true;
            ClearError();

            var dto = new BundlePackageDto
            {
                Id = SelectedBundle?.Id ?? 0,
                Name = BundleName,
                Description = BundleDescription,
                BundlePrice = BundlePrice,
                Quantity = BundleQuantity,
                SKUId = SelectedBundleSku.Id,
                IsActive = BundleIsActive,
                Notes = BundleNotes
            };

            await _promoService.SaveBundleAsync(dto);
            await LoadAsync();
            ClearBundleForm();

            StatusMessage = "Bundle saved";
        }
        catch (Exception ex)
        {
            SetError($"Error saving bundle: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void ClearBundleForm()
    {
        SelectedBundle = null;
        BundleName = string.Empty;
        BundleDescription = null;
        BundlePrice = 0;
        BundleQuantity = 6;
        SelectedBundleSku = null;
        BundleIsActive = true;
        BundleNotes = null;
    }

    [RelayCommand]
    public async Task RunSimulationAsync()
    {
        try
        {
            if (SelectedPromoSku == null)
            {
                SetError("Select SKU for simulation");
                return;
            }

            var quantity = PromoQuantity <= 0 ? 1 : PromoQuantity;
            var basePrice = PromoBasePrice > 0 ? PromoBasePrice : SelectedPromoSku.RetailPrice;
            if (basePrice <= 0)
            {
                SetError("Base price must be greater than 0");
                return;
            }

            IsLoading = true;
            ClearError();

            DiscountedUnitPrice = ApplyDiscount(basePrice, quantity, SelectedPromoDiscount);
            DiscountedTotal = DiscountedUnitPrice * quantity;
            Savings = (basePrice * quantity) - DiscountedTotal;

            var (grossMargin, netMargin) = await _pricingService.CalculateMarginsAsync(
                SelectedPromoSku.Id,
                DiscountedUnitPrice);

            DiscountGrossMargin = grossMargin;
            DiscountNetMargin = netMargin;

            if (SelectedPromoBundle != null && SelectedPromoBundle.Quantity > 0)
            {
                var bundleQuantity = SelectedPromoBundle.Quantity;
                var bundleCount = quantity / bundleQuantity;
                var remainder = quantity % bundleQuantity;
                BundleTotalPrice = (bundleCount * SelectedPromoBundle.BundlePrice) + (remainder * basePrice);
                BundleEffectiveUnitPrice = quantity > 0 ? BundleTotalPrice / quantity : 0;

                var (bundleGross, bundleNet) = await _pricingService.CalculateMarginsAsync(
                    SelectedPromoSku.Id,
                    BundleEffectiveUnitPrice);

                BundleGrossMargin = bundleGross;
                BundleNetMargin = bundleNet;
            }
            else
            {
                BundleTotalPrice = 0;
                BundleEffectiveUnitPrice = 0;
                BundleGrossMargin = 0;
                BundleNetMargin = 0;
            }

            StatusMessage = "Simulation completed";
        }
        catch (Exception ex)
        {
            SetError($"Error running simulation: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedDiscountChanged(DiscountDto? value)
    {
        if (value == null)
            return;

        DiscountName = value.Name;
        DiscountDescription = value.Description;
        DiscountType = value.DiscountType;
        DiscountValue = value.DiscountValue;
        DiscountStartDate = value.StartDate;
        DiscountEndDate = value.EndDate;
        DiscountApplicableFor = value.ApplicableFor;
        DiscountMinimumQuantity = value.MinimumQuantity;
        DiscountIsActive = value.IsActive;
        DiscountNotes = value.Notes;
    }

    partial void OnSelectedBundleChanged(BundlePackageDto? value)
    {
        if (value == null)
            return;

        BundleName = value.Name;
        BundleDescription = value.Description;
        BundlePrice = value.BundlePrice;
        BundleQuantity = value.Quantity;
        BundleIsActive = value.IsActive;
        BundleNotes = value.Notes;
        SelectedBundleSku = Skus.FirstOrDefault(s => s.Id == value.SKUId);
    }

    partial void OnSelectedPromoSkuChanged(SKUDto? value)
    {
        if (value == null)
            return;

        PromoBasePrice = value.RetailPrice;
        SelectedPromoBundle = Bundles.FirstOrDefault(b => b.SKUId == value.Id && b.IsActive);
    }

    private static decimal ApplyDiscount(decimal basePrice, int quantity, DiscountDto? discount)
    {
        if (discount == null)
            return basePrice;

        if (!discount.IsActive)
            return basePrice;

        var today = DateTime.Today;
        if (today < discount.StartDate.Date || today > discount.EndDate.Date)
            return basePrice;

        var type = discount.DiscountType?.Trim() ?? "";

        if (type.Equals("Fixed", StringComparison.OrdinalIgnoreCase))
        {
            return Math.Max(0, basePrice - discount.DiscountValue);
        }

        if (type.Equals("Volume", StringComparison.OrdinalIgnoreCase))
        {
            if (discount.MinimumQuantity.HasValue && quantity < discount.MinimumQuantity.Value)
                return basePrice;

            var pct = discount.DiscountValue / 100m;
            return Math.Max(0, basePrice * (1 - pct));
        }

        var percentage = discount.DiscountValue / 100m;
        return Math.Max(0, basePrice * (1 - percentage));
    }
}
