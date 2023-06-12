using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Octokit;
using System.ServiceProcess;
using System.Net.Http;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

namespace AgentService.Service
{
    public class ServiceUpdater
    {
        private const string Owner = "ariel129";
        private const string Repo = "agent-service";

        private static GitHubClient _client;
        static ServiceUpdater()
        {
            var token = "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"; // use a standard name for the env variable

            if (string.IsNullOrWhiteSpace(token))
                throw new InvalidOperationException("GitHub token not found in environment variables");

            _client = new GitHubClient(new ProductHeaderValue("agent-service"))
            {
                Credentials = new Credentials(token)
            };
        }

        public static async Task<(bool, string)> CheckForUpdates(string serviceName)
        {
            try
            {
                var (isNewReleaseAvailable, latestReleaseUrl) = await GetLatestReleaseUrl();

                if (!isNewReleaseAvailable)
                {
                    Console.WriteLine("No new release available.");
                    return (false, "");
                }

                StopService(serviceName);
                Console.WriteLine(serviceName + " service is stopped...");

                return (true, latestReleaseUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                return (false, "");
            }
        }

        public static void StopService(string serviceName)
        {
            if (!IsServiceRunning(serviceName))
            {
                Console.WriteLine("Service '{0}' is not running. Skipping stop command.", serviceName);
                return;
            }

            var process = ExecuteCommand("sc", $"stop {serviceName}");

            if (process.ExitCode != 0)
            {
                Console.WriteLine("Failed to stop service. Output:");
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                throw new Exception("Failed to stop service");
            }
        }

        public static void StartService(string serviceName)
        {
            if (IsServiceRunning(serviceName))
            {
                Console.WriteLine("Service '{0}' is already running. Skipping start command.", serviceName);
                return;
            }

            var process = ExecuteCommand("sc", $"start {serviceName}");

            if (process.ExitCode != 0)
            {
                Console.WriteLine("Failed to start service. Output:");
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                throw new Exception("Failed to start service");
            }
        }

        public static bool IsServiceRunning(string serviceName)
        {
            var service = ServiceController.GetServices().FirstOrDefault(s => s.ServiceName == serviceName);
            return service != null && service.Status != ServiceControllerStatus.Stopped;
        }

        private static Process ExecuteCommand(string command, string arguments)
        {
            var psi = new ProcessStartInfo(command, arguments)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var process = Process.Start(psi);
            process.WaitForExit();

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();

            Console.WriteLine("Command output:");
            Console.WriteLine(standardOutput);

            if (!string.IsNullOrEmpty(standardError))
            {
                Console.WriteLine("Command error:");
                Console.WriteLine(standardError);
            }

            return process;
        }


        private static async Task DownloadLatestRelease()
        {
            try
            {
                var (isisNewReleaseAvailable, latestReleaseUrl) = await GetLatestReleaseUrl();

                Console.WriteLine(latestReleaseUrl);
                if (string.IsNullOrEmpty(latestReleaseUrl))
                {
                    Console.WriteLine("No latest release URL found.");
                    return;
                }

                Console.WriteLine("Latest release URL: " + latestReleaseUrl);
                string releaseFileName = Path.GetFileName(latestReleaseUrl);

                if (string.IsNullOrEmpty(releaseFileName))
                {
                    Console.WriteLine("Invalid release file name.");
                    return;
                }

                string releaseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, releaseFileName);

                using (HttpClient client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5);

                    var response = await client.GetAsync(latestReleaseUrl);

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Request error, HTTP {response.StatusCode}: {response.ReasonPhrase}");
                        return;
                    }

                    using (var stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (var fileStream = new FileStream(releaseFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }

                Console.WriteLine("Release downloaded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while downloading the release.");
                Console.WriteLine("Message: " + ex.Message);
                Console.WriteLine("Stack Trace: " + ex.StackTrace);
            }
        }

        private static async Task<(bool, string)> GetLatestReleaseUrl()
        {
            var releases = await _client.Repository.Release.GetAll(Owner, Repo);

            if (releases == null || !releases.Any())
            {
                Console.WriteLine("No releases found for this repository.");
                return (false, null);
            }
         
            var latestRelease = releases.OrderByDescending(r => r.PublishedAt).First();
            Console.WriteLine(latestRelease.TagName);
            // Compare latest version on GitHub with the current version
            // You need to replace 'GetCurrentVersion()' with the method that returns your current application version
            // if (latestRelease.TagName <= GetCurrentVersion())
            //{
            // return (false, null);
            // }

            var asset = latestRelease.Assets.FirstOrDefault(a => a.Name.EndsWith(".exe"));
            if (asset == null)
            {
                Console.WriteLine("No .exe asset found for the latest release.");

                return (false, null);
            }

            string latestReleaseUrl = asset.BrowserDownloadUrl;

            return (true, latestReleaseUrl);
        }

        public static async Task PerformUpdate(string serviceName, string releaseUrl)
        {

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{serviceName}_update.zip");

            StartService(serviceName);
            Console.WriteLine(tempFilePath + " service is started...");

        }
    }
}
