using System;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Tracing;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ChunkedEnumerator;
using Nest;
using System.Globalization;

namespace DemoEventSource
{
    public class EventData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTimeOffset Timestamp { get; set; }

        public string ProviderName { get; set; }

        public int EventId { get; set; }

        public string Message { get; set; }

        public string Level { get; set; }

        public string Keywords { get; set; }

        public string EventName { get; set; }

        public EventOpcode OpCode { get; set; }

        public IDictionary<string, object> Payload { get; set; }
    }


    public class ESEventListener : Microsoft.Diagnostics.Tracing.EventListener
    {
        private const string EventDocumentTypeName = "event";
        private BlockingCollection<EventData> events = new BlockingCollection<EventData>(1000);
        private CancellationTokenSource cts;
        private Uri _elasticLocation = null;
        private string _indexName;

        public ESEventListener()
        {
            this.cts = new CancellationTokenSource();
            Task.Run(() => this.EventConsumerAsync(this.cts.Token));
            _elasticLocation = new Uri("http://localhost:9200");
            var pid = Process.GetCurrentProcess()?.Id;
            string index = $"LISBON-RETREAT-{Dns.GetHostName()}-{pid}";
            _indexName = index.ToLower(); //indexnames must be lower case
        }


        private async Task EventConsumerAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (events.Count > 0)
                {
                    var oldEvents = Interlocked.Exchange<BlockingCollection<EventData>>(ref events, new BlockingCollection<EventData>());
                    await SendEventsAsync(oldEvents, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Override this method to get a list of all the eventSources that exist.  
        /// </summary>
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            {
                EnableEvents(eventSource, EventLevel.LogAlways, EventKeywords.All);
            }
        }

        /// <summary>
        /// We override this method to get a callback on every event we subscribed to with EnableEvents
        /// </summary>
        /// <param name="eventData"></param>
        protected override void OnEventWritten(EventWrittenEventArgs eventArgs)
        {
            var data = eventArgs.ToEventData();
            events.TryAdd(data);
        }

        private async Task SendEventsAsync(IEnumerable<EventData> events, CancellationToken cancellationToken)
        {
            if (events == null)
            {
                return;
            }

            try
            {
                var node = _elasticLocation;
                var settings = new ConnectionSettings(node)
                    .DefaultIndex(_indexName)
                    .BasicAuthentication("elastic", "changeme");
                using (settings)
                {
                    var client = new ElasticClient(settings);

                    try
                    {
                        var chunks = events.Chunk(500);
                        foreach (var chunk in chunks)
                        {
                            BulkRequest request = new BulkRequest();
                            request.Refresh = Elasticsearch.Net.Refresh.True;

                            List<IBulkOperation> operations = new List<IBulkOperation>();
                            foreach (EventData eventData in chunk)
                            {
                                BulkCreateOperation<EventData> operation = new BulkCreateOperation<EventData>(eventData);
                                operation.Index = _indexName;
                                operation.Type = EventDocumentTypeName;
                                operations.Add(operation);
                            }

                            request.Operations = operations;

                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }

                            // Note: the NEST client is documented to be thread-safe so it should be OK to just reuse the this.esClient instance
                            // between different SendEventsAsync callbacks.
                            // Reference: https://www.elastic.co/blog/nest-and-elasticsearch-net-1-3
                            IBulkResponse response = await client.BulkAsync(request).ConfigureAwait(false);
                            if (!response.IsValid)
                            {
                                Console.WriteLine("failed to send");
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex);
                    }
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }


        #region Private members

        private static string ShortGuid(Guid guid)
        {
            return guid.ToString().Substring(0, 8);
        }

        #endregion
    }
    internal static class EventDataExtensions
    {
        private static string HexadecimalNumberPrefix = "0x";
        // Micro-optimization: Enum.ToString() uses type information and does a binary search for the value,
        // which is kind of slow. We are going to to the conversion manually instead.
        private static readonly string[] EventLevelNames = new string[]
        {
            "Always",
            "Critical",
            "Error",
            "Warning",
            "Informational",
            "Verbose"
        };

        public static EventData ToEventData(this EventWrittenEventArgs eventSourceEvent)
        {
            EventData eventData = new EventData
            {
                ProviderName = eventSourceEvent.EventSource.Name,
                Timestamp = DateTime.UtcNow,
                EventId = eventSourceEvent.EventId,
                Level = EventLevelNames[(int)eventSourceEvent.Level],
                Keywords = HexadecimalNumberPrefix + ((ulong)eventSourceEvent.Keywords).ToString("X16", CultureInfo.InvariantCulture),
                EventName = eventSourceEvent.EventName,
                OpCode = eventSourceEvent.Opcode,
            };

            try
            {
                if (eventSourceEvent.Message != null)
                {
                    // If the event has a badly formatted manifest, the FormattedMessage property getter might throw
                    eventData.Message = string.Format(CultureInfo.InvariantCulture, eventSourceEvent.Message, eventSourceEvent.Payload.ToArray());
                }
            }
            catch
            {
            }

            eventData.Payload = eventSourceEvent.GetPayloadData();

            return eventData;
        }

        //public static MessagingEventData ToMessagingEventData(this EventData eventData)
        //{
        //    string eventDataSerialized = JsonConvert.SerializeObject(eventData);
        //    MessagingEventData messagingEventData = new MessagingEventData(Encoding.UTF8.GetBytes(eventDataSerialized));
        //    return messagingEventData;
        //}

        private static IDictionary<string, object> GetPayloadData(this EventWrittenEventArgs eventSourceEvent)
        {
            Dictionary<string, object> payloadData = new Dictionary<string, object>();

            if (eventSourceEvent.Payload == null || eventSourceEvent.PayloadNames == null)
            {
                return payloadData;
            }

            IEnumerator<object> payloadEnumerator = eventSourceEvent.Payload.GetEnumerator();
            IEnumerator<string> payloadNamesEnunmerator = eventSourceEvent.PayloadNames.GetEnumerator();
            while (payloadEnumerator.MoveNext())
            {
                payloadNamesEnunmerator.MoveNext();
                payloadData.Add(payloadNamesEnunmerator.Current, payloadEnumerator.Current);
            }

            return payloadData;
        }
    }

}
