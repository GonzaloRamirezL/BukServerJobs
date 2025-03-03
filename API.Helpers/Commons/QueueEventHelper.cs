using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace API.Helpers.Commons
{
    public sealed class QueueEventHelper
    {

        private static readonly int TotalAttempts = Int32.Parse(ConfigurationHelper.Value("maxAttemptsQueue") ?? "3");

        private static readonly string ConnectionStringSNClocks = ConfigurationHelper.Value("ServiceBus") ?? "Endpoint=sb://integracioneslogsqueue.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=sZnkjIKCUne+fdF7wwEkLbeS0bqLOnkzQt4XeqK64Es=";
        private static readonly string QueueIntegracionesLogs = ConfigurationHelper.Value("SNClockQueue") ?? "integracioneslogsqueue";
       

        private static readonly string QueueSNClocksDeadLetter = (ConfigurationHelper.Value("SNClockQueue") ?? "integracioneslogsqueue") + "/$deadletterqueue";
        

        private static readonly ServiceBusSender IntegracionesLogsSender = null;
        
        private static readonly ServiceBusReceiver IntegracionesLogsDeadLetterReceiver = null;
        

        private readonly ServiceBusSender Sender = null;
        private readonly ServiceBusReceiver Receiver = null;

        private static readonly int BatchSize = Int32.Parse(ConfigurationHelper.Value("BatchSize") ?? "100");

        static QueueEventHelper()
        {
            ServiceBusClientOptions optionsServiceBus = new ServiceBusClientOptions()
            {
                RetryOptions = new ServiceBusRetryOptions()
                {
                    MaxRetries = TotalAttempts,
                    Delay = TimeSpan.FromSeconds(1),
                    MaxDelay = TimeSpan.FromSeconds(10),
                    Mode = ServiceBusRetryMode.Exponential
                },

            };

            ServiceBusClient Client = new ServiceBusClient(ConnectionStringSNClocks, optionsServiceBus);

            IntegracionesLogsSender = Client.CreateSender(QueueIntegracionesLogs);
            
            IntegracionesLogsDeadLetterReceiver = Client.CreateReceiver(QueueSNClocksDeadLetter);
            
        }

        public QueueEventHelper(BaseQueue baseQueue)
        {
            switch (baseQueue)
            {
                case BaseQueue.IntegracionesLogs:
                    Sender = IntegracionesLogsSender;
                    break;
                
                case BaseQueue.IntegracionesLogsDeadLetter:
                    Receiver = IntegracionesLogsDeadLetterReceiver;
                    break;
                
            }
        }


        /// <summary>
        /// Inserta un mensaje a la cola
        /// </summary>
        /// <param name="eventoQ">Objeto Envento definido en la llamada </param>
        /// <param name="delay">Delay de tiempo </param>
        public void SendMessages<U>(U eventoQ, int delay = 0)
        {
            ServiceBusMessage message = new ServiceBusMessage(JsonConvert.SerializeObject(eventoQ))
            {
                ScheduledEnqueueTime = DateTime.UtcNow.AddMinutes(delay)
            };

            AsyncHelpers.RunSync(() => Sender.SendMessageAsync(message));
        }

        /// <summary>
        /// Sube el objeto entregado, a la cola de esta instancia.
        /// </summary>
        /// <param name="messages"></param>
        public void Push(IEnumerable<ServiceBusMessage> messages)
        {
            while (messages.Any())
            {
                var batch = messages.Take(BatchSize);

                AsyncHelpers.RunSync(() => Sender.SendMessagesAsync(batch));

                messages = messages.Skip(BatchSize);
            }
        }

        public List<U> GetDeadClock<U>()
        {
            List<U> messages = new List<U>();
            var deadClockmessage = AsyncHelpers.RunSync(() => Receiver.ReceiveMessageAsync());
            while (deadClockmessage != null)
            {
                messages.Add(JsonConvert.DeserializeObject<U>(deadClockmessage.Body.ToString()));
                AsyncHelpers.RunSync(() => Receiver.CompleteMessageAsync(deadClockmessage));
                deadClockmessage = AsyncHelpers.RunSync(() => Receiver.ReceiveMessageAsync());
            }
            return messages;
        }

        
    }

    public enum BaseQueue
    {
        IntegracionesLogs,
        IntegracionesLogsDeadLetter,
        
    }
}
