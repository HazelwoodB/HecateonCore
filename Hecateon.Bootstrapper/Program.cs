using System.Diagnostics;
using System.Text.Json;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

var config = BootstrapConfig.Create();
var runner = new BootstrapRunner(config);
var exitCode = await runner.RunAsync();
Environment.Exit(exitCode);

internal sealed class BootstrapRunner
{
    private readonly BootstrapConfig _config;
    private readonly bool _useAnsiColors = !Console.IsOutputRedirected;
    private bool _inPlaceDashboardEnabled;
    private int _dashboardLines;
    private readonly Dictionary<string, StepState> _stepStates = new()
    {
        ["Dependencies"] = StepState.Pending,
        ["Repository"] = StepState.Pending,
        ["Validation"] = StepState.Pending,
        ["Server"] = StepState.Pending,
        ["AI Warmup"] = StepState.Pending,
        ["Publish"] = StepState.Pending,
        ["Launch"] = StepState.Pending
    };
    private readonly Dictionary<string, string> _stepDetails = new();

    public BootstrapRunner(BootstrapConfig config)
    {
        _config = config;
        _inPlaceDashboardEnabled = !_config.DisableInPlaceDashboard && !Console.IsOutputRedirected;
    }

    public async Task<int> RunAsync()
    {
        PrintHeader();
        RenderStatusMatrix();

        SetStepState("Dependencies", StepState.Running, "Checking dotnet and git");
        if (!await EnsureCommandAvailableAsync("dotnet", "--version"))
        {
            SetStepState("Dependencies", StepState.Failed, "dotnet SDK missing");
            WriteError("dotnet SDK is required.");
            return 1;
        }

        if (!await EnsureCommandAvailableAsync("git", "--version"))
        {
            SetStepState("Dependencies", StepState.Failed, "git missing");
            WriteError("git is required.");
            return 1;
        }
        SetStepState("Dependencies", StepState.Success, "Tools detected");

        SetStepState("Repository", StepState.Running, "Preparing repository");
        if (!await EnsureRepoAsync())
        {
            SetStepState("Repository", StepState.Failed, "Repo setup failed");
            return 1;
        }
        SetStepState("Repository", StepState.Success, "Repository ready");

        SetStepState("Validation", StepState.Running, "Restore/build/test");
        if (!await RunValidationAsync())
        {
            SetStepState("Validation", StepState.Failed, "Validation failed");
            return 1;
        }
        if (_config.EnableValidation)
        {
            SetStepState("Validation", StepState.Success, "Validation passed");
        }
        else
        {
            SetStepState("Validation", StepState.Skipped, "Disabled by configuration");
        }

        SetStepState("Server", StepState.Running, "Starting and waiting for readiness");
        var serverProcess = await StartServerAsync();
        if (serverProcess is null)
        {
            SetStepState("Server", StepState.Failed, "Server start failed");
            return 1;
        }

        if (!await WaitForServerAsync())
        {
            SetStepState("Server", StepState.Failed, "Health check timed out");
            WriteError("Server failed to become ready.");
            return 1;
        }
        SetStepState("Server", StepState.Success, "Server ready");

        if (_config.EnableAiWarmup)
        {
            SetStepState("AI Warmup", StepState.Running, "Warming chat endpoint");
        }
        else
        {
            SetStepState("AI Warmup", StepState.Skipped, "Disabled by configuration");
        }

        var aiWarmupResult = await WarmAiAsync();
        if (_config.EnableAiWarmup)
        {
            SetStepState("AI Warmup", aiWarmupResult ? StepState.Success : StepState.Failed, aiWarmupResult ? "Warmup completed" : "Warmup failed (continuing)");
        }

        if (_config.EnableDesktopPublish)
        {
            SetStepState("Publish", StepState.Running, "Publishing desktop app");
        }
        else
        {
            SetStepState("Publish", StepState.Skipped, "Disabled by configuration");
        }
        if (!await PublishDesktopAsync())
        {
            SetStepState("Publish", StepState.Failed, "Publish failed");
            return 1;
        }
        if (_config.EnableDesktopPublish)
        {
            SetStepState("Publish", StepState.Success, "Publish completed");
        }

        SetStepState("Launch", StepState.Running, "Launching desktop app");
        if (!LaunchDesktop())
        {
            SetStepState("Launch", StepState.Failed, "Executable not found");
            return 1;
        }
        SetStepState("Launch", StepState.Success, "Desktop launched");

        WriteSuccess("Bootstrap complete. Desktop app launched.");
        return 0;
    }

    private void PrintHeader()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("HECATEON BOOTSTRAPPER");
        Console.WriteLine("Pretest -> Test -> Server -> AI Warmup -> App");
        Console.WriteLine("========================================");
        Console.WriteLine();
    }

    private async Task<bool> EnsureCommandAvailableAsync(string fileName, string args)
    {
        var result = await RunProcessAsync(fileName, args, _config.WorkingRoot, throwOnError: false);
        return result.ExitCode == 0;
    }

    private async Task<bool> EnsureRepoAsync()
    {
        Directory.CreateDirectory(_config.InstallRoot);

        var gitDir = Path.Combine(_config.RepoRoot, ".git");
        var requiresClone = !Directory.Exists(_config.RepoRoot) || !Directory.Exists(gitDir);

        if (requiresClone)
        {
            if (Directory.Exists(_config.RepoRoot) && !Directory.Exists(gitDir))
            {
                WriteWarning($"Repository folder exists without .git. Recreating: {_config.RepoRoot}");
                try
                {
                    Directory.Delete(_config.RepoRoot, recursive: true);
                }
                catch (Exception ex)
                {
                    WriteError($"Unable to reset repository folder: {ex.Message}");
                    return false;
                }
            }

            WriteInfo($"Cloning repository into {_config.RepoRoot}");
            var clone = await RunProcessAsync("git", $"clone {_config.RepoUrl} \"{_config.RepoRoot}\"", _config.InstallRoot, throwOnError: false);
            if (clone.ExitCode != 0)
            {
                WriteError("Failed to clone repository.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(_config.RepoBranch))
            {
                var checkout = await RunProcessAsync("git", $"checkout {_config.RepoBranch}", _config.RepoRoot, throwOnError: false);
                if (checkout.ExitCode != 0)
                {
                    WriteWarning($"Could not checkout branch '{_config.RepoBranch}'. Continuing.");
                }
            }
        }

        if (!File.Exists(_config.SolutionPath))
        {
            WriteError($"Solution file missing: {_config.SolutionPath}");
            return false;
        }

        if (_config.EnableAutoUpdate)
        {
            WriteInfo("Fetching latest changes from GitHub");
            await RunProcessAsync("git", "fetch --all --prune", _config.RepoRoot, throwOnError: false);

            if (!string.IsNullOrWhiteSpace(_config.RepoBranch))
            {
                var checkout = await RunProcessAsync("git", $"checkout {_config.RepoBranch}", _config.RepoRoot, throwOnError: false);
                if (checkout.ExitCode != 0)
                {
                    WriteWarning($"Could not checkout branch '{_config.RepoBranch}'. Continuing with current branch.");
                }
            }

            var pull = await RunProcessAsync("git", "pull --ff-only", _config.RepoRoot, throwOnError: false);
            if (pull.ExitCode != 0)
            {
                WriteWarning("Git pull failed; continuing with local checkout.");
            }
        }
        else
        {
            WriteInfo("Auto-update disabled. Skipping fetch/pull.");
        }

        return true;
    }

    private async Task<bool> RunValidationAsync()
    {
        if (!_config.EnableValidation)
        {
            WriteWarning("Validation disabled by configuration.");
            return true;
        }

        WriteInfo("Running pretesting + testing");
        await StopLingeringAppProcessesAsync();

        if (await RunProcessAsync("dotnet", $"restore \"{_config.SolutionPath}\"", _config.RepoRoot, throwOnError: false) is { ExitCode: not 0 })
        {
            WriteError("dotnet restore failed.");
            return false;
        }

        if (await RunProcessAsync("dotnet", $"build \"{_config.SolutionPath}\" -c Release -v minimal", _config.RepoRoot, throwOnError: false) is { ExitCode: not 0 })
        {
            WriteError("dotnet build failed.");
            return false;
        }

        if (File.Exists(_config.TestProjectPath))
        {
            var test = await RunProcessAsync("dotnet", $"test \"{_config.TestProjectPath}\" -c Release -v minimal", _config.RepoRoot, throwOnError: false);
            if (test.ExitCode != 0)
            {
                WriteError("dotnet test failed.");
                return false;
            }
        }
        else
        {
            WriteWarning("Test project not found; skipping tests.");
        }

        WriteSuccess("Validation passed.");
        return true;
    }

    private Task StopLingeringAppProcessesAsync()
    {
        foreach (var processName in new[] { "Lullaby", "Lullaby.Desktop" })
        {
            try
            {
                var processes = Process.GetProcessesByName(processName);
                foreach (var process in processes)
                {
                    WriteWarning($"Stopping stale process {process.ProcessName} ({process.Id}) before validation.");
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(5000);
                }
            }
            catch (Exception ex)
            {
                WriteWarning($"Could not stop process '{processName}': {ex.Message}");
            }
        }

        return Task.CompletedTask;
    }

    private async Task<Process?> StartServerAsync()
    {
        WriteInfo("Starting server");

        if (!File.Exists(_config.ServerProjectPath))
        {
            WriteError($"Server project missing: {_config.ServerProjectPath}");
            return null;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_config.ServerProjectPath}\" --configuration Release --urls {_config.ServerUrl}",
            WorkingDirectory = _config.RepoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.Environment["HECATEON_USE_INMEMORY_DB"] = _config.UseInMemoryDbForBootstrap ? "true" : "false";

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine($"[server] {e.Data}");
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine($"[server] {e.Data}");
            }
        };

        if (!process.Start())
        {
            WriteError("Unable to start server process.");
            return null;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return await Task.FromResult(process);
    }

    private async Task<bool> WaitForServerAsync()
    {
        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(3) };

        for (var i = 0; i < _config.ServerWaitSeconds; i++)
        {
            try
            {
                var response = await client.GetAsync($"{_config.ServerUrl.TrimEnd('/')}/api/test");
                if (response.IsSuccessStatusCode)
                {
                    WriteSuccess("Server is ready.");
                    return true;
                }
            }
            catch
            {
            }

            await Task.Delay(1000);
        }

        return false;
    }

    private async Task<bool> WarmAiAsync()
    {
        if (!_config.EnableAiWarmup)
        {
            WriteInfo("AI warmup disabled by configuration.");
            return true;
        }

        WriteInfo("Warming AI endpoint");

        using var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.Add("X-Device-Id", "bootstrapper-device");
        if (!string.IsNullOrWhiteSpace(_config.RecoveryCode))
        {
            client.DefaultRequestHeaders.Add("X-Recovery-Code", _config.RecoveryCode);
        }
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            var response = await client.PostAsJsonAsync($"{_config.ServerUrl.TrimEnd('/')}/api/chat", new { message = "warmup" });
            if (response.IsSuccessStatusCode)
            {
                WriteSuccess("AI warmup successful.");
                return true;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized || response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                WriteWarning("AI warmup requires device approval/recovery code. Skipping.");
                return true;
            }

            WriteWarning($"AI warmup response: {(int)response.StatusCode} {response.StatusCode}. Continuing.");
            return false;
        }
        catch (Exception ex)
        {
            WriteWarning($"AI warmup skipped: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> PublishDesktopAsync()
    {
        if (!_config.EnableDesktopPublish)
        {
            WriteWarning("Desktop publish disabled by configuration.");
            return true;
        }

        WriteInfo("Publishing desktop app");

        if (!File.Exists(_config.DesktopProjectPath))
        {
            WriteError($"Desktop project missing: {_config.DesktopProjectPath}");
            return false;
        }

        var publish = await RunProcessAsync(
            "dotnet",
            $"publish \"{_config.DesktopProjectPath}\" -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true",
            _config.RepoRoot,
            throwOnError: false);

        if (publish.ExitCode != 0)
        {
            WriteError("Desktop publish failed.");
            return false;
        }

        return true;
    }

    private bool LaunchDesktop()
    {
        var candidateNames = new[]
        {
            "Lullaby.Desktop.exe",
            "Lullaby.exe"
        };

        var publishDir = Path.Combine(
            _config.RepoRoot,
            "Lullaby.Desktop",
            "bin",
            "Release",
            "net8.0-windows",
            "win-x64",
            "publish");

        foreach (var file in candidateNames)
        {
            var path = Path.Combine(publishDir, file);
            if (!File.Exists(path))
            {
                continue;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                WorkingDirectory = publishDir,
                UseShellExecute = true
            });

            WriteSuccess($"Launched {path}");
            return true;
        }

        WriteError($"Published desktop executable not found in {publishDir}");
        return false;
    }

    private static async Task<ProcessResult> RunProcessAsync(string fileName, string arguments, string workingDirectory, bool throwOnError)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine(output.Trim());
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine(error.Trim());
        }

        var result = new ProcessResult(process.ExitCode, output, error);

        if (throwOnError && result.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command failed: {fileName} {arguments}");
        }

        return result;
    }

    private static void WriteInfo(string message) => Console.WriteLine($"[INFO] {message}");
    private static void WriteWarning(string message) => Console.WriteLine($"[WARN] {message}");
    private static void WriteError(string message) => Console.WriteLine($"[ERROR] {message}");
    private static void WriteSuccess(string message) => Console.WriteLine($"[OK] {message}");

    private void SetStepState(string step, StepState state, string detail)
    {
        _stepStates[step] = state;
        _stepDetails[step] = detail;
        RenderStatusMatrix();
    }

    private void RenderStatusMatrix()
    {
        if (CanUseInPlaceDashboard())
        {
            try
            {
                var currentTop = Console.CursorTop;
                var targetTop = Math.Max(0, currentTop - _dashboardLines);
                Console.SetCursorPosition(0, targetTop);
                _dashboardLines = 0;
            }
            catch
            {
                _inPlaceDashboardEnabled = false;
                _dashboardLines = 0;
            }
        }

        WriteDashboardLine("+----------------+----------+---------------------------------------+");
        WriteDashboardLine("| Step           | Status   | Detail                                |");
        WriteDashboardLine("+----------------+----------+---------------------------------------+");

        foreach (var step in new[] { "Dependencies", "Repository", "Validation", "Server", "AI Warmup", "Publish", "Launch" })
        {
            var state = _stepStates[step];
            var detail = _stepDetails.TryGetValue(step, out var value) ? value : "-";
            var status = state switch
            {
                StepState.Pending => "PENDING",
                StepState.Running => "RUNNING",
                StepState.Success => "PASS",
                StepState.Failed => "FAIL",
                StepState.Skipped => "SKIPPED",
                _ => "UNKNOWN"
            };

            var paddedStatus = Pad(status, 8);
            var coloredStatus = ColorizeStatus(paddedStatus, state);
            WriteDashboardLine($"| {Pad(step, 14)} | {coloredStatus} | {Pad(Truncate(detail, 37), 37)} |");
        }

        WriteDashboardLine("+----------------+----------+---------------------------------------+");

        if (CanUseInPlaceDashboard())
        {
            Console.WriteLine();
            _dashboardLines += 1;
        }
    }

    private static string Pad(string input, int width)
    {
        return input.Length >= width ? input[..width] : input.PadRight(width);
    }

    private static string Truncate(string input, int maxLen)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= maxLen)
        {
            return input;
        }

        return input[..Math.Max(0, maxLen - 3)] + "...";
    }

    private string ColorizeStatus(string value, StepState state)
    {
        if (!_useAnsiColors)
        {
            return value;
        }

        const string reset = "\u001b[0m";
        var color = state switch
        {
            StepState.Success => "\u001b[32m",
            StepState.Running => "\u001b[33m",
            StepState.Skipped => "\u001b[33m",
            StepState.Failed => "\u001b[31m",
            StepState.Pending => "\u001b[90m",
            _ => "\u001b[37m"
        };

        return $"{color}{value}{reset}";
    }

    private bool CanUseInPlaceDashboard()
    {
        return _inPlaceDashboardEnabled;
    }

    private void WriteDashboardLine(string line)
    {
        if (CanUseInPlaceDashboard())
        {
            try
            {
                var printable = StripAnsi(line);
                var width = Console.BufferWidth > 1 ? Console.BufferWidth - 1 : 120;
                var trailingPadding = printable.Length >= width ? string.Empty : new string(' ', width - printable.Length);
                Console.Write("\r");
                Console.Write(line);
                Console.Write(trailingPadding);
                Console.WriteLine();
                _dashboardLines += 1;
                return;
            }
            catch
            {
                _inPlaceDashboardEnabled = false;
            }
        }

        Console.WriteLine(line);
    }

    private static string StripAnsi(string input)
    {
        var builder = new StringBuilder(input.Length);
        var inEscape = false;

        foreach (var ch in input)
        {
            if (!inEscape && ch == '\u001b')
            {
                inEscape = true;
                continue;
            }

            if (inEscape)
            {
                if (ch == 'm')
                {
                    inEscape = false;
                }

                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }
}

internal enum StepState
{
    Pending,
    Running,
    Success,
    Failed,
    Skipped
}

internal sealed record ProcessResult(int ExitCode, string Output, string Error);

internal sealed class BootstrapConfig
{
    public string WorkingRoot { get; init; } = string.Empty;
    public string InstallRoot { get; init; } = string.Empty;
    public string RepoRoot { get; init; } = string.Empty;
    public string RepoUrl { get; init; } = "https://github.com/HazelwoodB/HecateonCore.git";
    public string RepoBranch { get; init; } = "main";
    public string ServerUrl { get; init; } = "https://localhost:5001";
    public bool EnableAutoUpdate { get; init; } = true;
    public bool EnableValidation { get; init; } = true;
    public bool EnableAiWarmup { get; init; } = true;
    public bool EnableDesktopPublish { get; init; } = true;
    public bool UseInMemoryDbForBootstrap { get; init; } = true;
    public string RecoveryCode { get; init; } = string.Empty;
    public bool DisableInPlaceDashboard { get; init; }
    public int ServerWaitSeconds { get; init; } = 40;
    public string SolutionPath { get; init; } = string.Empty;
    public string ServerProjectPath { get; init; } = string.Empty;
    public string DesktopProjectPath { get; init; } = string.Empty;
    public string TestProjectPath { get; init; } = string.Empty;

    public static BootstrapConfig Create()
    {
        var executableDir = AppContext.BaseDirectory;
        var currentDir = Directory.GetCurrentDirectory();
        var fallbackInstallRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HecateonCoreLauncher");
        var workingRoot = FindRootContainingSolution(currentDir)
                          ?? FindRootContainingSolution(executableDir)
                          ?? fallbackInstallRoot;
        var fallbackRepoRoot = Path.Combine(fallbackInstallRoot, "HecateonCore");
        var repoRoot = IsUsableRepoRoot(workingRoot)
            ? workingRoot
            : fallbackRepoRoot;

        var settingsPath = Path.Combine(executableDir, "bootstrapper.settings.json");
        if (!File.Exists(settingsPath))
        {
            settingsPath = Path.Combine(currentDir, "bootstrapper.settings.json");
        }

        var fileSettings = BootstrapFileSettings.Load(settingsPath);

        var repoUrl = ReadEnv("HECATEON_REPO_URL") ?? fileSettings.RepoUrl ?? "https://github.com/HazelwoodB/HecateonCore.git";
        var repoBranch = ReadEnv("HECATEON_REPO_BRANCH") ?? fileSettings.RepoBranch ?? "main";
        var serverUrl = ReadEnv("HECATEON_SERVER_URL") ?? fileSettings.ServerUrl ?? "https://localhost:5001";
        var enableAutoUpdate = ReadBoolEnv("HECATEON_AUTO_UPDATE") ?? fileSettings.EnableAutoUpdate ?? true;
        var enableValidation = ReadBoolEnv("HECATEON_ENABLE_VALIDATION") ?? fileSettings.EnableValidation ?? true;
        var enableAiWarmup = ReadBoolEnv("HECATEON_AI_WARMUP") ?? fileSettings.EnableAiWarmup ?? true;
        var enableDesktopPublish = ReadBoolEnv("HECATEON_ENABLE_PUBLISH") ?? fileSettings.EnableDesktopPublish ?? true;
        var useInMemoryDbForBootstrap = ReadBoolEnv("HECATEON_USE_INMEMORY_DB") ?? fileSettings.UseInMemoryDbForBootstrap ?? true;
        var recoveryCode = ReadEnv("HECATEON_RECOVERY_CODE") ?? fileSettings.RecoveryCode ?? string.Empty;
        var disableInPlaceDashboard = ReadBoolEnv("HECATEON_DISABLE_INPLACE_DASHBOARD") ?? fileSettings.DisableInPlaceDashboard ?? false;
        var serverWaitSeconds = ReadIntEnv("HECATEON_SERVER_WAIT_SECONDS") ?? fileSettings.ServerWaitSeconds ?? 40;

        return new BootstrapConfig
        {
            WorkingRoot = workingRoot,
            InstallRoot = fallbackInstallRoot,
            RepoRoot = repoRoot,
            RepoUrl = repoUrl,
            RepoBranch = repoBranch,
            ServerUrl = serverUrl,
            EnableAutoUpdate = enableAutoUpdate,
            EnableValidation = enableValidation,
            EnableAiWarmup = enableAiWarmup,
            EnableDesktopPublish = enableDesktopPublish,
            UseInMemoryDbForBootstrap = useInMemoryDbForBootstrap,
            RecoveryCode = recoveryCode,
            DisableInPlaceDashboard = disableInPlaceDashboard,
            ServerWaitSeconds = serverWaitSeconds,
            SolutionPath = Path.Combine(repoRoot, "Lullaby.slnx"),
            ServerProjectPath = Path.Combine(repoRoot, "Lullaby", "Lullaby", "Lullaby.csproj"),
            DesktopProjectPath = Path.Combine(repoRoot, "Lullaby.Desktop", "Lullaby.Desktop.csproj"),
            TestProjectPath = Path.Combine(repoRoot, "Lullaby", "Lullaby.Tests", "Lullaby.Tests.csproj")
        };
    }

    private static string? ReadEnv(string key)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool? ReadBoolEnv(string key)
    {
        var value = ReadEnv(key);
        if (value is null)
        {
            return null;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        if (value == "1") return true;
        if (value == "0") return false;
        return null;
    }

    private static int? ReadIntEnv(string key)
    {
        var value = ReadEnv(key);
        if (value is null)
        {
            return null;
        }

        return int.TryParse(value, out var parsed) ? parsed : null;
    }

    private static bool IsUsableRepoRoot(string path)
    {
        if (!Directory.Exists(path))
        {
            return false;
        }

        var solutionPath = Path.Combine(path, "Lullaby.slnx");
        var serverProject = Path.Combine(path, "Lullaby", "Lullaby", "Lullaby.csproj");
        var gitDir = Path.Combine(path, ".git");

        return File.Exists(solutionPath) && File.Exists(serverProject) && Directory.Exists(gitDir);
    }

    private static string? FindRootContainingSolution(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir is not null)
        {
            var sln = Path.Combine(dir.FullName, "Lullaby.slnx");
            if (File.Exists(sln))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}

internal sealed class BootstrapFileSettings
{
    public string? RepoUrl { get; init; }
    public string? RepoBranch { get; init; }
    public string? ServerUrl { get; init; }
    public bool? EnableAutoUpdate { get; init; }
    public bool? EnableValidation { get; init; }
    public bool? EnableAiWarmup { get; init; }
    public bool? EnableDesktopPublish { get; init; }
    public bool? UseInMemoryDbForBootstrap { get; init; }
    public string? RecoveryCode { get; init; }
    public bool? DisableInPlaceDashboard { get; init; }
    public int? ServerWaitSeconds { get; init; }

    public static BootstrapFileSettings Load(string settingsPath)
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return new BootstrapFileSettings();
            }

            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<BootstrapFileSettings>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new BootstrapFileSettings();
        }
        catch
        {
            return new BootstrapFileSettings();
        }
    }
}
