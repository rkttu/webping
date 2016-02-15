using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace ClouDeveloper.WebPing
{
    public sealed class WebPingService : ServiceBase
    {
        public WebPingService(CancellationTokenSource cancellationTokenSource, bool ownsToken)
            : base()
        {
            if (cancellationTokenSource == null)
                throw new ArgumentNullException("cancellationTokenSource");

            this.cts = cancellationTokenSource;
            this.clientFactory = new Lazy<HttpClient>(this.CreateHttpClient, true);
            this.mres = new ManualResetEventSlim(true);
            this.ownsToken = ownsToken;
        }

        public WebPingService()
            : this(new CancellationTokenSource(), true)
        {
        }

        private Lazy<HttpClient> clientFactory = null;
        private CancellationTokenSource cts = null;
        private ManualResetEventSlim mres = null;
        private bool ownsToken = false;
        private Task task = null;

        public CancellationTokenSource CancellationTokenSource
        {
            get { return this.cts; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.clientFactory != null &&
                    this.clientFactory.IsValueCreated)
                {
                    HttpClient client = this.clientFactory.Value;

                    if (client != null)
                        client.Dispose();

                    this.clientFactory = null;
                }

                if (this.ownsToken &&
                    this.cts != null)
                {
                    this.cts.Dispose();
                    this.cts = null;
                }

                if (this.mres != null)
                {
                    this.mres.Dispose();
                    this.mres = null;
                }

                if (this.task != null)
                {
                    this.task.Dispose();
                    this.task = null;
                }
            }

            base.Dispose(disposing);
        }

        public void Run(string[] args)
        {
            ServicePointManager.ServerCertificateValidationCallback += this.ServicePointManager_ServerCertificateValidationCallback;
            TimeSpan interval = ConfigurationAccessor.IntervalTimeSpan;
            HttpClient client = clientFactory.Value;

            while (!this.cts.Token.IsCancellationRequested)
            {
                this.mres.Wait(this.cts.Token);
                IEnumerable<SiteListItem> uriList = ConfigurationAccessor.GetSiteListFromSiteListFilePath();

                int count = uriList.Count();
                if (count < 1)
                    Trace.TraceWarning("No URL list specified. Please check your list file.");
                else
                    Trace.TraceInformation("Invoking {0} URIs.", count);

                List<Task> taskList = new List<Task>();
                foreach (SiteListItem eachItem in uriList)
                {
                    Trace.TraceInformation(eachItem.ToString());
                    HttpRequestMessage requestMessage = new HttpRequestMessage(
                        eachItem.Method,
                        eachItem.Uri);
                    Task t = client.SendAsync(requestMessage, this.cts.Token)
                        .ContinueWith(x =>
                        {
                            switch (x.Status)
                            {
                                case TaskStatus.Canceled:
                                    Trace.TraceWarning("Task has been cancelled.");
                                    break;
                                case TaskStatus.Faulted:
                                    Trace.TraceError("Task has an error/exception. {0}", x.Exception);
                                    break;
                                case TaskStatus.RanToCompletion:
                                    if (x.Result.IsSuccessStatusCode)
                                        Trace.TraceInformation("Task has been done with {0} {1}", (int)x.Result.StatusCode, x.Result.ReasonPhrase);
                                    else
                                        Trace.TraceWarning("Task has been done with {0} {1}", (int)x.Result.StatusCode, x.Result.ReasonPhrase);
                                    break;
                                default:
                                    Trace.TraceWarning("Unexpected task status occurred. {0}", x.Status);
                                    break;
                            }
                        });
                    taskList.Add(t);
                }
                Trace.TraceInformation("Waiting remained tasks.");
                if (!Task.WaitAll(taskList.ToArray(), (int)ConfigurationAccessor.WaitTimeout.TotalMilliseconds, this.cts.Token))
                    Trace.TraceWarning("Waiting timeout occurred.");

                Trace.TraceInformation("Perform sleep - {0}", interval);
                this.cts.Token.WaitHandle.WaitOne(interval);
            }
        }

        private bool ServicePointManager_ServerCertificateValidationCallback(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                Trace.TraceWarning("Certification error found: {0}", sslPolicyErrors.ToString());
                return ConfigurationAccessor.IgnoreCertificationError;
            }

            return true;
        }

        private HttpClient CreateHttpClient()
        {
            HttpClient client = new HttpClient();
            client.Timeout = ConfigurationAccessor.HttpTimeout;

            foreach (KeyValuePair<string, string> eachKVP in ConfigurationAccessor.GetCustomHeaders())
            {
                Trace.TraceInformation("Adding custom header {0}: {1}",
                    eachKVP.Key,
                    eachKVP.Value);
                bool result = client.DefaultRequestHeaders.TryAddWithoutValidation(eachKVP.Key, eachKVP.Value);
                if (!result)
                {
                    Trace.TraceWarning("Cannot add custom header {0}: {1}",
                        eachKVP.Key,
                        eachKVP.Value);
                }
            }

            return client;
        }

        protected override void OnStart(string[] args)
        {
            this.task = Task.Factory.StartNew(
                (x) => this.Run((x as string[]) ?? new string[] {}),
                args, cts.Token, TaskCreationOptions.LongRunning,
                TaskScheduler.Current ?? TaskScheduler.Default);
            base.OnStart(args);
        }

        protected override void OnPause()
        {
            this.mres.Reset();
            base.OnPause();
        }

        protected override void OnContinue()
        {
            this.mres.Set();
            base.OnContinue();
        }

        protected override void OnStop()
        {
            this.cts.Cancel();
            this.cts.Token.WaitHandle.WaitOne(TimeSpan.FromSeconds(3d));
            base.OnStop();
        }
    }
}
