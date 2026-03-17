using System;
using System.Diagnostics;
using System.IO;

namespace HecateonCore.Launcher
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "HecateonCore Launcher";
            Console.WriteLine("=== HecateonCore Launcher ===");
            Console.WriteLine("Monitor logs, run tests, check system health, and review compliance before launching the main application.");
            Console.WriteLine("Options:");
            Console.WriteLine("1. View recent audit log");
            Console.WriteLine("2. View recent health log");
            Console.WriteLine("3. Run automated tests");
            Console.WriteLine("4. Review compliance status");
            Console.WriteLine("5. Launch Hecateon Desktop");
            Console.WriteLine("6. Exit");

            while (true)
            {
                Console.Write("\nSelect an option (1-6): ");
                var input = Console.ReadLine();
                switch (input)
                {
                    case "1":
                        ShowLog("logs/audit.log", "Audit Log");
                        break;
                    case "2":
                        ShowLog("logs/health.log", "Health Log");
                        break;
                    case "3":
                        RunTests();
                        break;
                    case "4":
                        ShowCompliance();
                        break;
                    case "5":
                        LaunchDesktop();
                        return;
                    case "6":
                        return;
                    default:
                        Console.WriteLine("Invalid option.");
                        break;
                }
            }
        }

        static void ShowLog(string path, string label)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"\n--- Recent {label} ---");
                foreach (var line in File.ReadLines(path))
                {
                    Console.WriteLine(line);
                }
            }
            else
            {
                Console.WriteLine($"No {label.ToLower()} found.");
            }
        }

        static void RunTests()
        {
            Console.WriteLine("\nRunning automated tests...");
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "test",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            proc.WaitForExit();
            Console.WriteLine(proc.StandardOutput.ReadToEnd());
        }

        static void ShowCompliance()
        {
            var compliancePath = Path.Combine("docs", "compliance.md");
            if (File.Exists(compliancePath))
            {
                Console.WriteLine("\n--- Compliance Documentation ---");
                foreach (var line in File.ReadLines(compliancePath))
                {
                    Console.WriteLine(line);
                }
            }
            else
            {
                Console.WriteLine("No compliance documentation found.");
            }
        }

        static void LaunchDesktop()
        {
            Console.WriteLine("\nLaunching Hecateon Desktop...");
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --project Hecateon.Desktop/Hecateon.Desktop.csproj",
                UseShellExecute = true // For GUI apps
            };
            Process.Start(psi);
        }
    }
}