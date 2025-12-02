using MQTTnet;
using MQTTnet.Server;
using System.Net;

namespace CubeApp.Helpers
{
    public class MqttClientHelper
    {
        #region Client setup

        private IMqttClient? _client;

        private readonly MqttClientFactory ClientFactory = new MqttClientFactory();
        private MqttClientOptions ClientOptions() => new MqttClientOptionsBuilder()
            .WithEndPoint(new DnsEndPoint("localhost", 1883))
            .Build();

        public IMqttClient Client
        {
            get
            {
                if (_client == null)
                {
                    _client = ClientFactory.CreateMqttClient();
                    _client.ApplicationMessageReceivedAsync += async (message) =>
                    {
                        foreach (var applicableFunction in TopicEventListeners.Where(x => x.Key == message.ApplicationMessage.Topic))
                        {
                            await applicableFunction.Value.Invoke(message.ApplicationMessage);
                        }

                        message.AutoAcknowledge = true;
                    };
                }

                if (!_client.IsConnected)
                {
                    _client.ConnectAsync(ClientOptions(), CancellationToken.None).Wait();
                }


                return _client;
            }
        }

        #endregion

        #region Data Publishing 
        public async Task<MqttClientPublishResult?> PublishData(string topic, string data)
        {
            if (string.IsNullOrWhiteSpace(data) || string.IsNullOrWhiteSpace(topic))
                return null;
            try
            {
                return await Client.PublishAsync(new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(data)
                    .Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }
        #endregion

        public List<string> SubscribedTopics { get; set; } = new List<string>();

        private Dictionary<string, Func<MqttApplicationMessage, Task>> TopicEventListeners { get; set; }
            = new Dictionary<string, Func<MqttApplicationMessage, Task>>();

        public async Task AddTopicEventListener(string topic, Func<MqttApplicationMessage, Task> eventListener)
        {
            if (!SubscribedTopics.Any(x => x == topic))
                await Client.SubscribeAsync(topic);

            TopicEventListeners.Add(topic, eventListener);
        }
    }
}