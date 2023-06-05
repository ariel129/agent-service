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
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                var psi = new ProcessStartInfo("secedit", "/export /cfg c:\\temp\\secpol.cfg")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // Start the process.
                var process = Process.Start(psi);

                // Read the output of the process.
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                // Wait for the process to exit.
                await process.WaitForExitAsync(stoppingToken);

                // Log the output of the process.
                _logger.LogInformation("Command Output: {output}", output);
                _logger.LogError("Command Error: {error}", error);

                await Task.Delay(1000, stoppingToken);
            }
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
    }
}