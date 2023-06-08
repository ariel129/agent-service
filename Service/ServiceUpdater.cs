using System;
using System.Diagnostics;
using Octokit;

namespace AgentService.Service
{
    public class ServiceUpdater
    {
        private const string Owner = "ariel129";
        private const string Repo = "agent-service";
        private const string UpdateUrl = "https://example.com/update/package.zip";
        private const string UpdateFilePath = "path/to/update/package.zip";
        private const string UpdaterPath = "path/to/updater.exe";
        private const string Token = "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";

        private static GitHubClient _client = new GitHubClient(new ProductHeaderValue("agent-service"))
        {
            Credentials = new Credentials(Token)
        };

        public static async Task UpdateService(string serviceName)
        {
            StopService(serviceName);
            await DownloadLatestRelease();
            StartService(serviceName);
        }

        private static void StopService(string serviceName)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = $"stop {serviceName}";
                process.Start();
                process.WaitForExit();
            }
        }

        private static async Task DownloadLatestRelease()
        {
            try
            {
                string latestReleaseUrl = await GetLatestReleaseUrl();

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
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", Token);

                    using (HttpResponseMessage response = await client.GetAsync(latestReleaseUrl))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"Request error, HTTP {response.StatusCode}: {response.ReasonPhrase}");
                            return;
                        }

                        using (Stream stream = await response.Content.ReadAsStreamAsync())
                        {
                            using (FileStream fileStream = new FileStream(releaseFilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                await stream.CopyToAsync(fileStream);
                            }
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


        private static void StartService(string serviceName)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "sc";
                process.StartInfo.Arguments = $"start {serviceName}";
                process.Start();
                process.WaitForExit();
            }
        }

        private static async Task<string> GetLatestReleaseUrl()
        {
            var releases = await _client.Repository.Release.GetAll(Owner, Repo);

            if (!releases.Any())
            {
                Console.WriteLine("No releases found for this repository.");
                #nullable disable
                return null;
            }

            // Ordering the releases by their published date in descending order
            var latestRelease = releases.OrderByDescending(r => r.PublishedAt).First();

            if (latestRelease.Assets.Count == 0)
            {
                Console.WriteLine("No assets found for the latest release.");
                return null;
            }

            string latestReleaseUrl = latestRelease.Assets[0].BrowserDownloadUrl;

            return latestReleaseUrl;
        }
    }
}
