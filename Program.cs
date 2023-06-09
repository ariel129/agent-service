using System.Diagnostics; 
using Serilog; 
using AgentService.Service;
using System.ServiceProcess;

namespace AgentService
{
    public class Program
    {
        public static async Task Main(string[] args)
        { 
            var progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(Path.Combine(progData, "Agent", "agent-logs.txt"))
            .CreateLogger();
             
            if (IsServiceInstalled("AgentService"))
            {
                StartService("AgentService");
            }
            else
            {
                InstallService();
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService()
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                });

        public static void InstallService()
        {
            string serviceName = "AgentService";
            string displayName = "Agent Service";
            string description = "This is an agent service.";
            #nullable disable
            string executablePath = Process.GetCurrentProcess().MainModule.FileName;
            #nullable restore

            using (Process process = new Process())
            {
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = $"create {serviceName} binPath= \"{executablePath}\" displayName= \"{displayName}\" start= auto";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Log.Information("Service installed successfully.");
                    ModifyServiceDescription(serviceName, description);
                    StartService(serviceName);
                }
                else
                {
                    Log.Error("Failed to install service. Error message:");
                    Log.Error(output);
                }
            }
        }

        public static void ModifyServiceDescription(string serviceName, string description)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = $"description {serviceName} \"{description}\"";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Log.Information("Service description updated successfully.");
                }
                else
                {
                    Log.Error("Failed to update service description. Error message:");
                    Log.Error(output);
                }
            }
        }

        public static void StartService(string serviceName)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = $"start {serviceName}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Log.Information("Service started successfully.");
                }
                else
                {
                    Log.Error("Failed to start service. Error message:");
                    Log.Error(output);
                }
            }
        }

        public static bool IsServiceInstalled(string serviceName)
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == serviceName);
        }
    }
}