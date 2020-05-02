using System;
using System.Collections.Generic;
using System.Text;

namespace xMQ.PubSubProtocol
{
    internal class PubSubQueue
    {
        private uint persistentCount;

        public string Name { get; internal set; }
        public bool HasPersistentSubscriber { get { return persistentCount > 0; } }
        public bool CanDispose { get { return IdentifierSubscriber.Count == 0; } }

        public Envelope LastMessage { get; set; }

        private Dictionary<object, PairSocketSubscribe> IdentifierSubscriber { get; set; } = new Dictionary<object, PairSocketSubscribe>();

        internal List<Envelope> AddSubscriber(PairSocketSubscribe subsConfig, out bool hasConfiguration)
        {
            lock (this)
            {
                hasConfiguration = false;
                List<Envelope> droppedMessages = null;

                if (subsConfig.PairSocket.ConnectionId != null)
                {
                    if (subsConfig.LostType == PubSubQueueLostType.Persistent)
                    {
                        //Try get droped messages
                        hasConfiguration = IdentifierSubscriber.ContainsKey(subsConfig.PairSocket.ConnectionId);
                        if (!hasConfiguration)
                            persistentCount++;
                        else
                            droppedMessages = IdentifierSubscriber[subsConfig.PairSocket.ConnectionId].GetDropedMessages();

                    } else if(subsConfig.LostType == PubSubQueueLostType.LastMessage && LastMessage != null)
                    {
                        droppedMessages = new List<Envelope>() { LastMessage };
                    }

                    IdentifierSubscriber[subsConfig.PairSocket.ConnectionId] = subsConfig;
                }

                return droppedMessages;
            }
        }

        internal void RemoveSubscriber(PairSocket subscriber, bool forced)
        {
            lock (this)
            {
                if (subscriber.ConnectionId != null)
                {
                    if (!IdentifierSubscriber.ContainsKey(subscriber.ConnectionId))
                        return;

                    var subConfig = IdentifierSubscriber[subscriber.ConnectionId];

                    if (subConfig.LostType == PubSubQueueLostType.Persistent)
                    {
                        if (forced)
                        {
                            IdentifierSubscriber.Remove(subscriber.ConnectionId);
                            persistentCount--;
                        }
                    } else
                    {
                        IdentifierSubscriber.Remove(subscriber.ConnectionId);
                    }
                }
            }
        }

        internal Dictionary<object, PairSocketSubscribe>.ValueCollection GetClients()
        {
            return IdentifierSubscriber.Values;
        }
    }
}
