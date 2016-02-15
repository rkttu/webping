using System;
using System.Linq;
using System.ServiceProcess;
using System.IO;
using System.Diagnostics;
using System.Reflection;

namespace ClouDeveloper.WebPing
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (ConfigurationAccessor.ChangeCurrentDirectoryToExeFilePath)
            {
                try
                {
                    string asmCodeBase = Assembly.GetExecutingAssembly().GetName().CodeBase;
                    Trace.TraceInformation("Assembly Code Base URI: {0}", asmCodeBase);

                    string translatedCodeBasePath = new Uri(asmCodeBase, UriKind.Absolute).LocalPath;
                    Trace.TraceInformation("Assembly File Path: {0}", translatedCodeBasePath);

                    string destDir = Path.GetDirectoryName(translatedCodeBasePath);
                    Trace.TraceInformation("Chaging current directory to '{0}'", destDir);
                    Environment.CurrentDirectory = destDir;
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning(ex.Message);
                }
            }

            using (WebPingService service = new WebPingService())
            {
                if (args.Contains("--service", StringComparer.OrdinalIgnoreCase))
                    ServiceBase.Run(service);
                else
                {
                    Console.CancelKeyPress += (sender, e) =>
                    {
                        e.Cancel = true;
                        service.CancellationTokenSource.Cancel();
                    };
                    service.Run(args);
                }
                return Environment.ExitCode;
            }
        }
    }
}
