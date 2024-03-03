namespace Uns.Explorer.Benchmark
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var client = new UnsClient();

            var mqttConnection = new UnsMqttConnection("localhost", 1883);
            mqttConnection.AddDestination();
            client.AddConnection(mqttConnection);

            await client.Start();

            char[] alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

            var topicCount = 1000;
            var interval = 1000;

            while (true)
            {
                for (var i = 1; i <= topicCount; i++)
                {
                    var topic = i.ToString();

                    for (var j = 0; j < alphabet.Length; j++)
                    {
                        topic = UnsPath.Combine(topic, alphabet[j].ToString());
                    }

                    Console.WriteLine(topic);

                    await client.Publish(topic, Guid.NewGuid().ToString());
                }

                await Task.Delay(interval);
            }

        }
    }
}
