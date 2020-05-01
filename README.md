# eXtendable Message Queue
Provides a message queue with the purpose of being simple, performative and effective, and can be easily extended

### Summary

* [Send and Receive mensage](#send-and-receive-mensage)
   - [Asynchronous](#send-and-receive-mensage)
   - [Request/Reply](#send-and-receive-mensage)
   - [Pub/Sub](#send-and-receive-mensage)
* [Delivery guarantee](#send-and-receive-mensage) 
* [Extending the xMQ](#send-and-receive-mensage)
* [Protocol Supported](#send-and-receive-mensage)
   - [TCP](#send-and-receive-mensage)
   - [UDP](#send-and-receive-mensage)
   - [IPC](#send-and-receive-mensage)
* [SSL](#send-and-receive-mensage)
* [Cluster](#send-and-receive-mensage)
* [Properties and Configurations](#send-and-receive-mensage)

   
## Getting Started:


**Create Server**:

```c#
var socket = new PairSocket("tcp://127.0.0.1:5001"); // Server Host Address
socket.OnMessage += (remote, msg) => { };
socket.OnConnected += (remote) => { };
socket.OnDisconnected += (remote) => { };

socket.Bind(); //Call Bind()

Console.ReadKey();
```

**Create Client**:

```c#
var socket = new PairSocket("tcp://127.0.0.1:5001"); // Server address
socket.Id = "my client id :)";
socket.OnMessage += (remote, msg) => { };
socket.OnConnected += (remote) => { };
socket.OnDisconnected += (remote) => { };

socket.Connect(); //Call Connect()

Console.ReadKey();
```
## Send and Receive mensage

### Asynchronous

**Send**:

```c#

/* ... server or client connection setup ... */

var msg = new Message();
msg.Append(0x1); // My command, but it can be a string, a Guid or another primite type :)
msg.Append("Hello");
msg.Append("World");

// If this call is being made on the server, you will have to obtain your client's socket
// Através do método socket.GetClients() ou socket.GetClient("my client id :)")

var success = socket.Send(msg);

```

**Listerner**:

```c#

/* ... server or client connection setup ... */
socket.OnMessage += (remote, msg) => { 
   var myCommand = msg.ReadNext<int>();
   if(myCommand == 0x1) 
   {
      var firstToken = msg.ReadNext<string>();
      var secondToken = msg.ReadNext<string>();
      
      Console.WriteLine(firstToken);
      Console.WriteLine(secondToken);
      
      // If it is necessary to send a new message, 
      // we recommend that you write in the same message received, 
      // so that if the message contains a Reply Id, the server will be able to find the requester
      
      msg.Append(0x5); // My own response command if needed for your protocol
      msg.Append("Thanks");
      
      remote.Send(msg);
   }
}

```

### Request/Reply

**Request**:

```c#

/* ... server or client connection setup ... */

var msg = new Message();
msg.Append(0x1); // My command, but it can be a string, a Guid or another primite type :)
msg.Append("Hello");
msg.Append("World");

int timeout = 500; // -1 for infinty timeout
var response = socket.Request(msg, timeout);
if(response.Success) {
  var firstToken = response.ReadNext<int>(); //What will the server command be? Did he send a 0x5 or 0x6 to me? :)
  var secondToken = response.ReadNext<string>();
  Console.WriteLine(secondToken);
}
```

**Reply**:

```c#

/* ... server or client connection setup ... */
socket.OnMessage += (remote, msg) => { 
   var myCommand = msg.ReadNext<int>();
   if(myCommand == 0x1) 
   {
      var firstToken = msg.ReadNext<string>();
      var secondToken = msg.ReadNext<string>();
      
      Console.WriteLine(firstToken);
      Console.WriteLine(secondToken);
      
      // If it is necessary to send a new message, 
      // we recommend that you write in the same message received, 
      // so that if the message contains a Reply Id, the server will be able to find the requester
      
      msg.Append(0x5); // My own response command if needed for your protocol
      msg.Append("Thanks");
      
      remote.Send(msg);
   }
}


```


## Motivation
For some time I admire the communication in distributed network protocols. The problems of distributed computing have always appealed to me a lot.
Over time, I experimented with many MQ protocols, among them RabbitMQ, Mqtt, Kafka, Socket.io, SignalR and they are all really very incredible and very powerful

However, they all had something that bothered me, it was always a bazooka to kill an ant, and sometimes I needed to make this bazooka work like a gun that was what I needed

## Inspiration
That was when I met ZeroMQ, which proposed to do everything I needed, a simple protocol so that you could implement my own protocol on it.
An incredible project, with an approach without Broker, but I soon saw some limitations.

The project makes an abstraction that masks connection errors, and that for me would already make its use unfeasible.
To get around this, you should implement a pooling on it to check the active connections.
Other limitations also made me discouraged, such as the fact that you need a Socket object for each type of communication. So, if you want to work with Req/Resp and Pub/Sub, you should use 3 Sockets type, one for Req/Resp, one for Producer and one for Subscriber
