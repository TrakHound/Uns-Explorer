using Uns.Extensions;

namespace Uns.Explorer.Services
{
    public class ClientService : IDisposable
    {
        private const int _updateInterval = 500;
        private const int _maxUpdateCount = 500;


        private readonly Dictionary<string, UnsEventMessage> _messages = new Dictionary<string, UnsEventMessage>(); // Path => UnsEventMessage
        private readonly ListDictionary<string, string> _pathParents = new ListDictionary<string, string>(); // ParentPath => Path
        private readonly Dictionary<string, string> _values = new Dictionary<string, string>(); // Path => UnsEventMessage.Content (as UTF8 string)
        private readonly ItemIntervalQueue<string> _updatedPaths = new ItemIntervalQueue<string>(_updateInterval, _maxUpdateCount);
        private readonly ItemIntervalQueue<string> _updatedMessageCounts = new ItemIntervalQueue<string>(_updateInterval, _maxUpdateCount);
        private readonly Dictionary<string, long> _messageCounts = new Dictionary<string, long>();
        private readonly HashSet<string> _expandedPaths = new HashSet<string>();
        private readonly ThrottleEvent _updateDelay = new ThrottleEvent(_updateInterval);
        private object _lock = new object();

        private readonly ConnectionService _connectionService;
        private readonly UnsClient _client;
        private ConnectionConfiguration _connectionConfiguration;
        private string _subscribePath;
        private string _previousSubscribePath;
        private UnsConsumer<UnsEventMessage> _consumer;
        private string _selectedPath;

        private readonly System.Timers.Timer _metricsTimer;



        public ConnectionConfiguration ConnectionConfiguration => _connectionConfiguration;

        public string SubscribePath => _subscribePath;

        public string SelectedPath => _selectedPath;


        public event EventHandler<string> PathSelected;

        public event EventHandler<IEnumerable<UnsEventMessage>> MessagesReceived;

        public event ValueUpdatedHandler ValueUpdated;

        public event MessageCountUpdatedHandler MessageCountUpdated;

        public event ExpandedHandler ExpandedChanged;

        public event EventHandler Updated;


        public ClientService(ConnectionService connectionService)
        {
            _connectionService = connectionService;

            _updateDelay.Elapsed += UpdateDelayElapsed;
            _updatedPaths.ItemsReceived += UpdateValues;
            _updatedMessageCounts.ItemsReceived += UpdateMessageCounts;

            _client = new UnsClient();

            var consumer = _client.Subscribe("#");
            consumer.Received += EventMessageReceived;
            _consumer = consumer;

            _metricsTimer = new System.Timers.Timer();
            _metricsTimer.Interval = 5000;
            _metricsTimer.Elapsed += MetricsTimerElapsed;
            _metricsTimer.Start();
        }

        public void Dispose()
        {
            if (_metricsTimer != null) _metricsTimer.Dispose();

            if (_consumer != null) _consumer.Dispose();

            if (_client != null) _client.Stop();
        }


        public async Task Load(string connectionId)
        {
            var connectionConfiguration = _connectionService.GetConnection(connectionId);
            if (connectionConfiguration != null && connectionConfiguration.Id != null && connectionConfiguration.Type != null)
            {
                _connectionConfiguration = connectionConfiguration;

                switch (connectionConfiguration.Type.ToLower())
                {
                    case "mqtt":

                        var mqttServer = connectionConfiguration.GetParameter("server");
                        var mqttPort = connectionConfiguration.GetParameter<int>("port");
                        var mqttClientId = connectionConfiguration.GetParameter("clientId");

                        var mqttConnection = new UnsMqttConnection(mqttServer, mqttPort, mqttClientId);
                        mqttConnection.AddSubscription("#"); // Needs to be done differently. Need to subscribe to result of Subscribe(path) method
                        mqttConnection.AddDestination();
                        _client.AddConnection(mqttConnection);
                        break;


                    case "sparkplug-b":

                        var sparkplugConnection = new UnsSparkplugConnection("localhost", 1883, "sparkplug-example");
                        sparkplugConnection.AddApplication();
                        _client.AddConnection(sparkplugConnection);
                        break;
                }

                await _client.Start();
            }
            else
            {
                // Raise Unknown Connection event
            }
        }


        private void MetricsTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            //Console.WriteLine($"_updatedPaths.Count = {_updatedPaths.}");
            Console.WriteLine($"_messageCounts.Count = {_messageCounts.Count}");
            Console.WriteLine($"_pathParents.Count = {_pathParents.Count}");
        }

        public void Subscribe(string path)
        {
            if (path != _previousSubscribePath)
            {
                _subscribePath = path;

                if (_consumer != null)
                {
                    _consumer.Received -= EventMessageReceived;
                    _consumer.Dispose();
                }

                if (!string.IsNullOrEmpty(path))
                {
                    var consumer = _client.Subscribe(path);
                    consumer.Received += EventMessageReceived;
                    _consumer = consumer;
                }
                else
                {
                    var consumer = _client.Subscribe("#");
                    consumer.Received += EventMessageReceived;
                    _consumer = consumer;
                }

                _previousSubscribePath = path;

                if (Updated != null) Updated.Invoke(this, new EventArgs());
            }
        }

        public async Task Publish(string path, string content)
        {
            if (!string.IsNullOrEmpty(path))
            {
                await _client.Publish(path, content);
            }
        }


        public IEnumerable<string> GetFilteredPaths(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                var parentPath = UnsPath.GetParentPath(path);
                if (parentPath != null)
                {
                    var childPaths = GetChildPaths(parentPath);
                    if (childPaths != null)
                    {
                        var filteredPaths = new List<string>();

                        foreach (var childPath in childPaths)
                        {
                            if (childPath.ToLower().StartsWith(path.ToLower()))
                            {
                                filteredPaths.Add(childPath);
                            }
                        }

                        return filteredPaths;
                    }
                }
                else
                {
                    IEnumerable<string> childPaths;
                    lock (_lock) childPaths = _pathParents.Get("/")?.ToList();
                    if (childPaths != null)
                    {
                        var filteredPaths = new List<string>();

                        foreach (var childPath in childPaths)
                        {
                            if (childPath.ToLower().StartsWith(path.ToLower()))
                            {
                                filteredPaths.Add(childPath);
                            }
                        }

                        return filteredPaths;
                    }
                }
            }
            else
            {
                lock (_lock)
                {
                    return _pathParents.Get("/")?.ToList();
                }
            }

            return null;
        }

        public IEnumerable<string> GetChildPaths(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                lock (_lock)
                {
                    return _pathParents.Get(path.ToLower())?.ToList();
                }
            }

            return null;
        }

        public string GetValue(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                lock (_lock)
                {
                    return _values.GetValueOrDefault(path);
                }
            }

            return null;
        }

        public UnsEventMessage GetMessage(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                lock (_lock)
                {
                    return _messages.GetValueOrDefault(path);
                }
            }

            return null;
        }

        public long GetMessageCount(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                lock (_lock)
                {
                    return _messageCounts.GetValueOrDefault(path);
                }
            }

            return 0;
        }


        public void Select(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                _selectedPath = path;

                if (PathSelected != null) PathSelected.Invoke(this, path);
            }
        }

        public bool GetExpanded(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                lock (_lock)
                {
                    return _expandedPaths.Contains(path);
                }
            }

            return false;
        }

        public void SetExpanded(string path, bool expanded)
        {
            if (!string.IsNullOrEmpty(path))
            {
                if (expanded)
                {
                    lock (_lock)
                    {
                        if (!_expandedPaths.Contains(path)) _expandedPaths.Add(path);
                    }
                }
                else
                {
                    lock (_lock)
                    {
                        _expandedPaths.Remove(path);
                    }
                }

                if (ExpandedChanged != null) ExpandedChanged.Invoke(path, expanded);
            }
        }


        private void EventMessageReceived(object sender, UnsEventMessage message)
        {
            if (message != null && message.Path != null)
            {
                var alreadyExists = false;

                string value = null;
                if (message.Content != null)
                {
                    try
                    {
                        value = System.Text.Encoding.UTF8.GetString(message.Content);
                    }
                    catch { }
                }

                lock (_lock)
                {
                    alreadyExists = _messages.ContainsKey(message.Path);

                    _messages.Remove(message.Path);
                    _messages.Add(message.Path, message);

                    _values.Remove(message.Path);
                    _values.Add(message.Path, value);


                    foreach (var path in UnsPath.GetPaths(message.Path))
                    {
                        var parentPath = UnsPath.GetParentPath(path);
                        if (parentPath != null)
                        {
                            _pathParents.Add(parentPath.ToLower(), path);
                        }
                        else
                        {
                            _pathParents.Add("/", path);
                        }

                        // Update Message Count for Path
                        if (_messageCounts.ContainsKey(path))
                        {
                            _messageCounts[path] += 1;
                        }
                        else
                        {
                            _messageCounts.Add(path, 1);
                        }
                        _updatedMessageCounts.Add(path, path);
                    }
                }

                _updatedPaths.Add(message.Path, message.Path);

                if (!alreadyExists) _updateDelay.Set();
            }
        }

        private void UpdateDelayElapsed(object sender, EventArgs e)
        {
            if (Updated != null) Updated.Invoke(this, new EventArgs());
        }

        private void UpdateValues(object sender, IEnumerable<string> paths)
        {
            if (paths != null)
            {
                foreach (var path in paths)
                {
                    var value = GetValue(path);
                    if (ValueUpdated != null) ValueUpdated.Invoke(path, value);
                }
            }
        }

        private void UpdateMessageCounts(object sender, IEnumerable<string> paths)
        {
            if (paths != null)
            {
                foreach (var path in paths)
                {
                    var count = GetMessageCount(path);
                    if (MessageCountUpdated != null) MessageCountUpdated.Invoke(path, count);
                }
            }
        }
    }
}
