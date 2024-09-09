using MQTTnet;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class MqttUnityClient : MonoBehaviour
{
    private IMqttClient _mqttClient;

    private async void Start()
    {
        await ConnectAsync();
        // Đăng ký chủ đề để nhận tin nhắn
        await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic("unity/topic").Build());
    }

    private async Task ConnectAsync()
    {
        var factory = new MqttFactory();
        _mqttClient = factory.CreateMqttClient();

        var options = new MqttClientOptionsBuilder()
            .WithClientId("UnityClient")
            .WithTcpServer("broker.hivemq.com", 1883)
            .WithCleanSession()
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += e =>
        {
            // Xử lý tin nhắn nhận được
            var message = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            Debug.Log($"Received: {message}");
            return Task.CompletedTask;
        };

        await _mqttClient.ConnectAsync(options);
    }

    public async Task PublishAsync(string topic, string payload)
    {
        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtMostOnce)
            .Build();

        if (_mqttClient.IsConnected)
        {
            await _mqttClient.PublishAsync(message);
            Debug.Log($"Published: {payload} to {topic}");
        }
    }
}