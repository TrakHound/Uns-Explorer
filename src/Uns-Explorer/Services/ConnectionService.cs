using Uns.Extensions;

namespace Uns.Explorer.Services
{
    public class ConnectionService
    {
        private const string _directory = "connections";
        private const string _configurationFilename = "config.yaml";

        private readonly Dictionary<string, ConnectionConfiguration> _connections = new Dictionary<string, ConnectionConfiguration>();
        private readonly object _lock = new object();


        //public ConnectionService() 
        //{
        //    Load();
        //}

        public void Load()
        {
            var directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, _directory);
            if (Directory.Exists(directoryPath))
            {
                var connectionDirectories = Directory.GetDirectories(directoryPath);
                if (!connectionDirectories.IsNullOrEmpty())
                {
                    foreach (var connectionDirectory in connectionDirectories)
                    {
                        var configurationPath = Path.Combine(connectionDirectory, _configurationFilename);
                        if (File.Exists(configurationPath))
                        {
                            var configuration = ConnectionConfiguration.ReadYaml(configurationPath);
                            if (configuration != null && !string.IsNullOrEmpty(configuration.Id))
                            {
                                lock (_lock)
                                {
                                    _connections.Remove(configuration.Id);
                                    _connections.Add(configuration.Id, configuration);
                                }
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<ConnectionConfiguration> GetConnections()
        {
            lock (_lock)
            {
                if (!_connections.IsNullOrEmpty())
                {
                    return _connections.Values.ToList();
                }
            }

            return null;
        }

        public ConnectionConfiguration GetConnection(string connectionId)
        {
            if (!string.IsNullOrEmpty(connectionId))
            {
                lock (_lock)
                {
                    return _connections.GetValueOrDefault(connectionId);
                }
            }

            return null;
        }
    }
}
