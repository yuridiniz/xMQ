# eXtendable Message Queue
Provides a message queue with the purpose of being simple, performative and effective, and can be easily extended

## How To:

Request Reply:




## Motivation
For some time I admire the communication in distributed network protocols. The problems of distributed computing have always appealed to me a lot.
Over time, I experimented with many MQ protocols, among them RabbitMQ, Mqtt, Kafka, Socket.io, SignalR and they are all really very incredible and very powerful

However, they all had something that bothered me, it was always a bazooka to kill an ant, and sometimes I needed to make this bazooka work like a gun that was what I needed

## Inspiration
That was when I met ZeroMQ, which proposed to do everything I needed, a simple protocol so that you could implement my own protocol on it.
An incredible project, with an approach without Broker, but I soon saw some limitations.

The project makes an abstraction that masks connection errors, and that for me would already make its use unfeasible.
To get around this, you should implement a pooling on it to check the active connections.
Other limitations also made me discouraged, such as the fact that you need a Socket object for each type of communication. So, if you want to work with Req / Resp and Pub / Sub, you should use 3 Sockets type, one for Req / Resp, one for Producer and one for Subscriber
