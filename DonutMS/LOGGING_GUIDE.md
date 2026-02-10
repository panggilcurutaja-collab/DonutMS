# ?? DonutMS Advanced Logging System

## Overview
Kami telah mengimplementasikan sistem logging yang **sangat detail** menggunakan **Serilog** untuk membantu diagnosa issue startup aplikasi.

---

## ?? Logging Features

### 1. **Multi-Stage Startup Tracking**
Setiap tahap startup di-log secara detail:
- ? Stage 1: Serilog logger configuration
- ? Stage 2: Application settings configuration
- ? Stage 3: IHost creation
- ? Stage 4: Dependency Injection service registration
- ? Stage 5: Service provider creation
- ? Stage 6: Database initialization
- ? Stage 7: MainWindow creation
- ? Stage 8: MainWindow display

### 2. **Console Output**
Pada tahap startup awal (sebelum logger siap), ada console output dengan separator yang jelas:
```
???????????????????????????????????????????????????????????
?? DonutMS Application Starting...
???????????????????????????????????????????????????????????
[HH:mm:ss] Stage 1: Configuring Serilog logger...
```

### 3. **File Logging**
Logs disimpan di folder `Logs/` dengan format:
```
Logs/
  ??? donutms-20260210.log      (hari ini)
  ??? donutms-20260209.log      (kemarin)
  ??? donutms-20260208.log      (2 hari lalu)
```

**Format setiap log entry:**
```
[2026-02-10 14:23:45.123 +07:00] [INF] Message text here
[2026-02-10 14:23:46.456 +07:00] [ERR] Error details
```

### 4. **Error Handling**
Jika ada error di salah satu stage:
1. Error di-log ke file dengan stack trace lengkap
2. User akan melihat MessageBox dengan error detail
3. Application shutdown dengan exit code 1

---

## ?? How to Use

### View Logs While Running
1. **Console Output**: Lihat console window saat app running untuk real-time logs
2. **Log Files**: Buka `Logs/donutms-YYYYMMDD.log` di text editor

### Debug a Specific Stage
Cari log entry dengan stage name:
```
[Stage 4] Registering services with DI container...
  - Adding Serilog to logging...
  ? Application services registered
```

### Trace an Error
Jika app crash:
1. Buka `Logs/donutms-YYYYMMDD.log` (file hari ini)
2. Cari "?" atau "FATAL"
3. Lihat stack trace untuk detail error

---

## ?? Log Levels

| Level | Symbol | Use Case |
|-------|--------|----------|
| Debug | ?? | Detailed diagnostic info (defaults, loop iterations) |
| Information | ?? | General informational messages (stage completion) |
| Warning | ?? | Warning but non-critical (e.g., optional DB init fails) |
| Error | ? | Error occurred but handled gracefully |
| Fatal | ?? | Critical error, application will shutdown |

---

## ?? Configuration

### LoggingConfiguration.cs
```csharp
// Path to store logs
string logsFolder = "Logs"  // Change if needed

// File size limit per log file
fileSizeLimitBytes = 104857600  // 100 MB

// Retain how many old log files
retainedFileCountLimit = 10  // Keep last 10 days
```

### Log Output Format
```
[Timestamp] [Level] Message
```

---

## ?? Typical Successful Startup Log

```
???????????????????????????????????????????????????????????
?? DonutMS Application Starting...
???????????????????????????????????????????????????????????
[14:23:45] Stage 1: Configuring Serilog logger...
? Serilog logger configured successfully
[Stage 2] Configuring application settings...
  Base Path: C:\Users\hazel\source\repos\DonutMS\bin\Debug\net10.0-windows
  Environment: Production
[Stage 3] Creating IHost with dependency injection...
[Stage 4] Registering services with DI container...
  ? All services registered successfully
? IHost created successfully
[Stage 5] Creating service provider...
? Service provider created
[Stage 6] Initializing database...
? Database initialized
[Stage 7] Creating main window...
? MainWindow created successfully
[Stage 8] Displaying main window...
? MainWindow displayed
???????????????????????????????????????????????????????????
?? Application started successfully!
???????????????????????????????????????????????????????????
```

---

## ?? Troubleshooting Guide

### Scenario 1: Crash at Stage 1 (Logging Setup)
? Check `appsettings.json` and `appsettings.Development.json` exist
? Check Logs folder is writable

### Scenario 2: Crash at Stage 4 (Service Registration)
? Check DependencyInjectionConfiguration.cs for missing registrations
? Check appsettings.json for correct connection string

### Scenario 3: Crash at Stage 7 (MainWindow Creation)
? Check MainWindow.xaml for binding errors
? Check MainWindowViewModel constructor
? Check DataContext setup

### Scenario 4: App Shows but Nothing Works
? Check Stage 8 in logs, verify window loaded successfully
? Check if NavigationViewModel is registered
? Check if commands are executing (look for debug logs)

---

## ?? Next Steps

1. **Run the app** with `F5`
2. **Check the output** in Visual Studio's Debug console
3. **Open logs folder** to verify `donutms-YYYYMMDD.log` file created
4. **Share the log file** if you encounter any issues during Phase 8

---

**Created**: 2026-02-10  
**Version**: 1.0  
**Used by**: DonutMS Phase 8+
