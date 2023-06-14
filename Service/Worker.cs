using System.Diagnostics;

namespace AgentService.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Start the update checking task.
            var updateTask = Task.Run(() => CheckForUpdatesPeriodically("AgentService", stoppingToken), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running ats: {time}", DateTimeOffset.Now);

                var psi = new ProcessStartInfo("secedit", "/export /cfg c:\\temp\\secpol.cfg")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Start the process.
                var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogError("Could not start the process.");
                    continue;
                }

                // Read the output of the process.
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                // Wait for the process to exit.
                await process.WaitForExitAsync(stoppingToken);

                // Log the output of the process.
                _logger.LogInformation("Command Output: {output}", output);
                _logger.LogError("Command Error: {error}", error);

                await Task.Delay(1000 * 60, stoppingToken);
            }

            await updateTask; // Optional, if you want to make sure update checking stops when main task stops.
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker starting at: {time}", DateTimeOffset.Now);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Worker stopping at: {time}", DateTimeOffset.Now);
            return base.StopAsync(cancellationToken);
        }

        private async Task CheckForUpdatesPeriodically(string serviceName, CancellationToken stoppingToken)
        {
            var random = new Random();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Check for updates
                var (isUpdateAvailable, releaseUrl) = await ServiceUpdater.CheckForUpdates(serviceName);

                if (isUpdateAvailable)
                {
                    // Stop the worker
                    _logger.LogInformation("Update available, stopping the worker at: {time}", DateTimeOffset.Now);

                    // Perform update logic
                    await ServiceUpdater.PerformUpdate(serviceName, releaseUrl);

                    

                    var processStartInfo = new ProcessStartInfo("sc", $"start {serviceName}")
                    {
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    Process.Start(processStartInfo);

                    Environment.Exit(0);
                }

                // Generate a random delay between 1 and 60 minutes.
                int delay = random.Next(1, 61) * 60 * 1000;  // Convert minutes to milliseconds.
                Console.WriteLine(delay);
                await Task.Delay(delay, stoppingToken);
            }
        }
    }
}