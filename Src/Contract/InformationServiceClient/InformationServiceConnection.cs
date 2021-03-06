using System;
using System.Data.Common;
using SolarWinds.InformationService.Contract2;
using System.Data;
using System.ServiceModel;

namespace SolarWinds.InformationService.InformationServiceClient
{
    /// <summary>
    /// Represents a connection to a SolarWinds Information Service
    /// </summary>
    public sealed class InformationServiceConnection : DbConnection
    {
        private string endpointName;
        private string remoteAddress;
        private InfoServiceProxy proxy;
        private ServiceCredentials credentials;
        private bool bProxyOwner = true;
        private bool open = false;

        public InformationServiceConnection()
            : this(string.Empty)
        {
        }

        public InformationServiceConnection(string endpointName)
        {
            if (endpointName == null)
                throw new ArgumentNullException("endpointName");

            Initialize(endpointName, null, null);
        }

        //This is required by NCM. NCM provide it's own proxy object
        public InformationServiceConnection(InfoServiceProxy proxy) : this(proxy, false)
        {
        }

        public InformationServiceConnection(InfoServiceProxy proxy, bool takeOwnership)
        {
            Service = proxy;
            bProxyOwner = takeOwnership;
            if (bProxyOwner)
            {
                this.proxy = proxy;
            }
        }

        public InformationServiceConnection(IInformationService service)
        {
            if (service == null)
                throw new ArgumentNullException("service");

            Service = service;
            bProxyOwner = false;
        }

        public InformationServiceConnection(string endpointName, string remoteAddress)
        {
            if (endpointName == null)
                throw new ArgumentNullException("endpointName");
            if (remoteAddress == null)
                throw new ArgumentNullException("remoteAddress");

            Initialize(endpointName, remoteAddress, null);
        }

        public InformationServiceConnection(string endpointName, string remoteAddress, ServiceCredentials credentials)
        {
            if (endpointName == null)
                throw new ArgumentNullException("endpointName");
            if (remoteAddress == null)
                throw new ArgumentNullException("remoteAddress");
            if (credentials == null)
                throw new ArgumentNullException("credentials");

            Initialize(endpointName, remoteAddress, credentials);
        }

        public InformationServiceConnection(string endpointName, ServiceCredentials credentials)
        {
            if (endpointName == null)
                throw new ArgumentNullException("endpointName");
            if (credentials == null)
                throw new ArgumentNullException("credentials");

            Initialize(endpointName, null, credentials);
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotSupportedException();
        }

        public override void ChangeDatabase(string databaseName)
        {
            throw new NotSupportedException();
        }

        public override void Close()
        {
            if (!bProxyOwner)
                return;
            if (proxy != null)
            {
                try
                {
                    proxy.Dispose();
                }
                catch (TimeoutException)
                {
                    proxy.Abort();
                }
                catch (CommunicationException)
                {
                    proxy.Abort();
                }
            }
            proxy = null;
            Service = null;
            open = false;

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
            base.Dispose(disposing);
        }

        public override string ConnectionString
        {
            get
            {
                return endpointName;
            }
            set
            {
                Initialize(value, remoteAddress, credentials);
            }
        }

        public ServiceCredentials Credentials
        {
            get
            {
                return credentials;
            }
            set
            {
                Initialize(endpointName, remoteAddress, value);
            }
        }

        public new InformationServiceCommand CreateCommand()
        {
            return new InformationServiceCommand(this);
        }

        protected override DbCommand CreateDbCommand()
        {
            return CreateCommand();
        }

        public override string DataSource
        {
            get
            {
                return string.Empty;
            }
        }

        public override string Database
        {
            get
            {
                return string.Empty;
            }
        }

        public override void Open()
        {
            if (proxy == null && Service != null)
                return; // Must be in-process. Nothing to do for Open().

            if (proxy == null && !open)
                CreateProxy();

            if ((proxy.Channel != null) && (proxy.Channel.State != CommunicationState.Created))
                throw new InvalidOperationException("Cannot open an opened or previously closed connection");

            proxy.Open();

            if (proxy.Channel.State != CommunicationState.Opened)
                throw new InvalidOperationException("Could not open the connection");

            open = true;
        }

        public override string ServerVersion
        {
            get
            {
                return "1.0";
            }
        }

        public override ConnectionState State
        {
            get
            {
                if ((proxy != null) && (proxy.Channel != null) && (proxy.Channel.State == CommunicationState.Opened))
                    return ConnectionState.Open;
                else
                    return ConnectionState.Closed;
            }
        }

        private void Initialize(string endpointName, string remoteAddress, ServiceCredentials credentials)
        {
            if ((proxy != null) && (bProxyOwner != true))
                throw new InvalidOperationException("The Proxy Connection is not owned by InformationServiceConnection object");

            if ((proxy != null) && (proxy.Channel.State != CommunicationState.Created))
                throw new InvalidOperationException("Cannot change the endpoint for an existing connection");

            if (proxy != null)
                Close();

            this.endpointName = endpointName;
            this.remoteAddress = remoteAddress;
            this.credentials = credentials;

            CreateProxy();
        }

        private void CreateProxy()
        {
            if (endpointName.Length != 0)
            {
                if (remoteAddress != null)
                {
                    if (credentials != null)
                        proxy = new InfoServiceProxy(endpointName, remoteAddress, credentials);
                    else
                        proxy = new InfoServiceProxy(endpointName, remoteAddress);
                }
                else
                {
                    if (credentials != null)
                        proxy = new InfoServiceProxy(endpointName, credentials);
                    else
                        proxy = new InfoServiceProxy(endpointName);
                }

                Service = proxy;
            }
        }

        internal IInformationService Service { get; private set; }
    }
}
