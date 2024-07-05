using SharpPcap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SteamAutoShutdown
{
    public static class SteamDetect
    {
        public static List<MyProcess> GetProcesses()
        {
            List<MyProcess> processes = new List<MyProcess>();

            Process netStatProc = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo()
            {
                Arguments = "-a -n -o",
                FileName = "netstat.exe",
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            netStatProc.StartInfo = startInfo;
            netStatProc.Start();

            StreamReader stdOutput = netStatProc.StandardOutput;
            StreamReader stdError = netStatProc.StandardError;

            string content = stdOutput.ReadToEnd() + stdError.ReadToEnd();
            string[] rows = Regex.Split(content, "\r\n");

            foreach (string row in rows)
            {
                string[] tokens = Regex.Split(row, "\\s+");

                if (tokens.Length > 4 && (tokens[1].Equals("TCP") || tokens[1].Equals("UDP")))
                {
                    string localAddress = Regex.Replace(tokens[2], @"\[(.*?)\]", "1.1.1.1");

                    int id = tokens[1] == "UDP" ? int.Parse(tokens[4]) : int.Parse(tokens[5]);

                    processes.Add(new MyProcess()
                    {
                        Name = GetProcessNameByID(id),
                        Protocol = tokens[1],
                        ID = id,
                        Port = int.Parse(localAddress.Split(':')[1]),
                    });
                }
            }

            return processes;
        }

        private static string GetProcessNameByID(int id)
        {
            try
            {
                return Process.GetProcessById(id).ProcessName;
            }
            catch { return string.Empty; }
        }
    }

    public class MyProcess
    {
        public string Name { get; set; }

        public string Protocol { get; set; }
        public int ID { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return $" {Name} ({ID}) : {Protocol} - {Port}";
        }
    }
}
