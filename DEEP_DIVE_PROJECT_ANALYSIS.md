# DonutMS — Deep Dive Analysis

Dokumen ini merangkum hasil pembelajaran mendalam terhadap codebase DonutMS dengan fokus pada arsitektur, aliran data, kualitas implementasi, dan prioritas perbaikan teknis.

## 1) Gambaran Arsitektur

### Stack inti
- **UI**: WPF (`net10.0-windows`) dengan kombinasi **MahApps.Metro** + **MaterialDesignThemes**.
- **Pattern**: **MVVM** berbasis CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`).
- **Backend lokal aplikasi desktop**: Layer service + repository.
- **Persistence**: EF Core + SQLite (`donutms.db`).
- **Cross-cutting**: DI (`Microsoft.Extensions.DependencyInjection`), Host builder, Serilog, FluentValidation, AutoMapper.

### Layering saat ini
1. **Presentation** (`Views`, `ViewModels`)  
2. **Application services** (`Services`)  
3. **Data access** (`Data/Repositories`, `Data/DbContext`)  
4. **Domain model** (`Data/Entities`)  

Secara struktur, layering sudah jelas; tantangan utama ada di konsistensi implementasi antar-layer.

## 2) Startup Lifecycle & Dependency Graph

### Alur startup
Startup dipisah menjadi beberapa tahap yang ditulis eksplisit (Stage 1–8): konfigurasi logger, host, registrasi service, inisialisasi database, pembuatan window, hingga `Show()`. Ini bagus untuk observability dan troubleshooting.

### Kekuatan
- Logging startup sangat detail dan mudah ditelusuri.
- Konfigurasi environment (`DOTNET_ENVIRONMENT`) sudah diakomodasi.
- Integrasi `IHost` untuk desktop app sudah modern.

### Catatan risiko
- Database initializer di-resolve namun tidak dipanggil method inisialisasi eksplisit di startup (hanya resolve instance); jika logika init tidak ada di constructor, maka init tidak terjadi.
- Error handling startup sudah kuat, tetapi beberapa warning “non-critical” dapat menutupi problem data awal.

## 3) Navigasi, Otorisasi, dan UX Flow

### Mekanisme saat ini
- `NavigationService` menyimpan peta string view-name -> type ViewModel.
- `CanNavigateTo` memakai role bitwise (`UserRole`) untuk pembatasan akses.
- `MainWindowViewModel` mengatur `CurrentViewModel` dan memicu command load per halaman.

### Kekuatan
- Role-based menu visibility sudah ada.
- Menu dihasilkan dinamis sesuai role user.

### Temuan penting
1. **Flow tombol menu belum memindahkan halaman secara efektif**  
   `NavigationViewModel.NavigateToMenu()` hanya memanggil `NavigateTo` (logging), tetapi tidak mengubah `MainWindowViewModel.CurrentViewModel`. Artinya klik menu kemungkinan besar tidak mengubah konten utama.
2. **State duplikasi** (`IsMenuOpen`, `IsDarkTheme`) ada di MainWindowViewModel dan NavigationViewModel; rentan divergen.
3. **Default user hard-coded** admin di service; cocok untuk bootstrap/dev, tetapi bukan desain produksi.

## 4) Data Model & EF Core

### Kekuatan
- `DbContext` cukup kaya domain (inventory, recipe, production, costing, PO, audit).
- Banyak relationship penting sudah diatur dengan `DeleteBehavior` yang relatif aman.
- Index unik untuk kode penting (`SKU`, `Recipe.Code`, `BatchCode`, `PONumber`) sudah ada.

### Titik perhatian
- Domain cukup besar namun masih dikelola dalam monolitic entity assembly; scaling akan sulit tanpa bounded context jelas.
- Soft delete (`IsDeleted`) digunakan luas, tetapi belum terlihat konsisten lewat global query filter; bergantung pada disiplin query manual.

## 5) Service Layer Assessment

### Positif
- Interface per service sudah baik untuk testability.
- Separation use case per domain (Inventory, Production, Costing, Pricing).

### Temuan kritikal
1. **Inventory transaction tidak dipersist**  
   Di `AddStockInAsync` / `RemoveStockOutAsync`, object `StockTransaction` dibuat tetapi tidak ditambahkan ke DbSet/repository sebelum `SaveChangesAsync()`. Dampaknya histori transaksi berpotensi hilang.
2. **Beberapa async method tanpa `await`** (contoh `CalculateMarkupAsync`) menambah noise API.
3. **Business rule masih simplistik** (misalnya net margin = gross margin - 5 tetap), perlu parameterisasi.

## 6) Repository Layer Assessment

### Positif
- Generic repository + specialized repository dipakai cukup konsisten.
- Query domain spesifik sudah dipisahkan dari service.

### Catatan
- Ada typo naming API (`GetBySupplerIdAsync`) yang menurunkan kejelasan kontrak.
- Operator precedence query rawan bug (misalnya kondisi aktif batch `||` dan `&&` perlu parenthesis eksplisit untuk menghindari miskomunikasi logika).

## 7) Kualitas Kode & Maintainability

### Hal baik
- Struktur folder rapi dan cukup enterprise-oriented.
- Logging pattern cukup konsisten.
- Validasi sudah disiapkan dengan FluentValidation extension.

### Isu maintainability
- Beberapa literal string status (“Planned”, “In Progress”, dll.) tersebar; sebaiknya jadi enum/value object.
- Ada code smell null-forgiveness saat inisialisasi (`new(null!, null!)`) di ViewModel property default.
- Integrasi command antar-ViewModel belum terpola untuk one-way event/mediator, membuat coupling implisit.

## 8) Prioritas Perbaikan (Roadmap Praktis)

### Prioritas P0 (segera)
1. Benahi alur navigasi agar klik menu benar-benar update `CurrentViewModel` di shell VM.
2. Persist `StockTransaction` secara nyata di inventory service.
3. Tambahkan unit/integration test untuk skenario stok masuk/keluar + histori transaksi.
4. Pastikan database initializer benar-benar menjalankan migrate/seed.

### Prioritas P1 (dekat)
1. Centralize status constants (enum + converter DB jika perlu).
2. Hilangkan duplikasi state UI (`IsMenuOpen`/`IsDarkTheme`) dengan single source of truth.
3. Terapkan global query filter untuk soft delete.
4. Rapikan kontrak repository (naming typo, nullable flow, async signature).

### Prioritas P2 (menengah)
1. Introduce application use-case handlers (CQRS ringan) untuk mengurangi god-service.
2. Tambahkan domain events untuk audit trail otomatis.
3. Buat test harness EF Core SQLite in-memory untuk regression suite.
4. Refactor bounded context (Recipe, Inventory, Production, Pricing) lebih modular.

## 9) Kesimpulan

DonutMS sudah memiliki fondasi arsitektur yang kuat untuk aplikasi desktop enterprise skala kecil-menengah: MVVM modern, DI/Host, EF Core, logging, serta domain model yang kaya. Hambatan terbesar saat ini bukan pada teknologi, tetapi pada **konsistensi alur antar-layer** (terutama navigasi UI dan persistensi transaksi inventory). Dengan eksekusi roadmap P0–P1, stabilitas sistem dapat meningkat signifikan tanpa rewrite besar.
