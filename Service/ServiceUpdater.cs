using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ServiceProcess;
using Newtonsoft.Json;

namespace AgentService.Service
{
    public class ServiceUpdater
    {
        private const string Owner = "ProsperTogether";
        private const string Repo = "EP-Agent";
        private const string UpdateUrl = "https://example.com/update/package.zip";
        private const string UpdateFilePath = "path/to/update/package.zip";
        private const string UpdaterPath = "path/to/updater.exe";

        public static void UpdateService(string serviceName)
        {
            StopService(serviceName);
            DownloadLatestRelease();
            //ApplyUpdate();
            //StartService(serviceName);
        }

        private static void StopService(string serviceName)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "service";
                process.StartInfo.Arguments = $"stop {serviceName}";
                process.Start();
                process.WaitForExit();
            }
        }

        private static void DownloadLatestRelease()
        {
            string latestReleaseUrl = GetLatestReleaseUrl();
            string releaseFileName = Path.GetFileName(latestReleaseUrl);
            string releaseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, releaseFileName);

            using (WebClient client = new WebClient())
            {
                client.DownloadFile(latestReleaseUrl, releaseFilePath);
            }

        }

        private static void ApplyUpdate()
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = UpdaterPath;
                process.StartInfo.Arguments = UpdateFilePath;
                process.Start();
                process.WaitForExit();
            }
        }

        private static void StartService(string serviceName)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = "service";
                process.StartInfo.Arguments = $"start {serviceName}";
                process.Start();
                process.WaitForExit();
            }
        }

        private static string GetLatestReleaseUrl()
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("User-Agent", "Mozilla/5.0"); // GitHub API requires a User-Agent header

                string apiUrl = $"https://api.github.com/repos/{Owner}/{Repo}/releases/latest";
                string response = client.DownloadString(apiUrl);

                dynamic release = JsonConvert.DeserializeObject(response);
                string latestReleaseUrl = release.assets[0].browser_download_url;

                return latestReleaseUrl;
            }
        }
    }
}
