﻿namespace ServiceBus.Management.RavenDB
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Lucene.Analyzers;
    using NServiceBus;
    using NServiceBus.Logging;
    using Raven.Client;
    using Raven.Client.Embedded;
    using Raven.Client.Indexes;
    using Raven.Database.Server;

    public class RavenBootstrapper : INeedInitialization
    {
        public void Init()
        {
            Directory.CreateDirectory(Settings.DbPath);

            var documentStore = new EmbeddableDocumentStore
                {
                    DataDirectory = Settings.DbPath,
                    UseEmbeddedHttpServer = true,
                    EnlistInDistributedTransactions = false
                };

            NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(Settings.Port);

            documentStore.Configuration.Port = Settings.Port;
            documentStore.Configuration.HostName = Settings.Hostname;

            documentStore.Configuration.VirtualDirectory = Settings.VirtualDirectory + "/storage";

            documentStore.Initialize();

            var customAnalyzer = typeof (NGramAnalyzer).Assembly.Location;
            File.Copy(customAnalyzer, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Analyzers", Path.GetFileName(customAnalyzer)), true);
            
            var sw = new Stopwatch();

            sw.Start();
            Logger.Info("Index creation started");

            IndexCreation.CreateIndexesAsync(typeof(RavenBootstrapper).Assembly, documentStore)
                .ContinueWith(c =>
                    {
                        sw.Stop();
                        if (c.IsFaulted)
                        {
                            Logger.Error("Index creation failed", c.Exception);
                        }
                        else
                        {
                            Logger.InfoFormat("Index creation completed, total time: {0}", sw.Elapsed);
                        }
                    });

            Configure.Instance.Configurer.RegisterSingleton<IDocumentStore>(documentStore);
            Configure.Component<RavenUnitOfWork>(DependencyLifecycle.InstancePerUnitOfWork);
            Configure.Instance.RavenPersistenceWithStore(documentStore);
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(RavenBootstrapper));
    }
}