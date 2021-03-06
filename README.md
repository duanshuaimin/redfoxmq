## RedFoxMQ

RedFoxMQ is a .NET in-memory message queue that uses a simple TCP transport. It is fairly lightweight
and provides full control over message serialization / de-serialization. The performance is very
good (batch broadcasting over TCP reaches >2 million (!) messages per second on my machine).

### Get it on NuGet!

    Install-Package RedFoxMQ

If you need ProtoBuf serialization:

    Install-Package RedFoxMQ.Serialization.ProtoBuf
    
#### Supported Features

- easy integration (no external components needed)
- implement your own message serialization / deserialization
- Publisher / Subscriber scenario
- Request / Response scenario
- ServiceQueue scenario
- TCP / InProc transport
- message batching

#### Planned Features

- reliable transport (to confirm receipt of messages)
- shared memory transport for faster inter-process communication

Also I'm sure there are still bugs. So do not use it for production yet! (unless you don't care of course)

#### Unsupported Features

- message persistence
- message timestamps
- unique message IDs (e.g. to detect duplicate messages)
- encryption

The features above are not going to be supported to keep the message queue 
lightweight. But there are ways around it. You could implement some these features 
within your specific use case (e.g. just add timestamps to all of your messages, 
if you need timestamps).

#### Usage Example

The easiest way is to look at the unit tests. They are a good source of examples.

This is standalone Publisher / Subscriber example:

```c#
using RedFoxMQ;
using RedFoxMQ.Transports;
using System;
using System.Text;

class Program
{
    static void Main()
    {
        var messageSerialization = InitializeMessageSerialization();
        
        using (var publisher = new Publisher(messageSerialization))
        using (var subscriber1 = new Subscriber(messageSerialization))
        using (var subscriber2 = new Subscriber(messageSerialization))
        {
            var endpoint = new RedFoxEndpoint(RedFoxTransport.Tcp, "localhost", 5555, null);
            publisher.Bind(endpoint); // call Bind multiple times to listen to multiple endpoints

            subscriber1.MessageReceived += (socket, msg) => 
               Console.WriteLine("Subscriber 1: " + ((TestMessage)msg).Text);
            subscriber1.Connect(endpoint);

            subscriber2.MessageReceived += (socket, msg) => 
               Console.WriteLine("Subscriber 2: " + ((TestMessage)msg).Text);
            subscriber2.Connect(endpoint);
            
            foreach (var text in new[] {"Hello", "World"})
            {
                var message = new TestMessage {Text = text};
                publisher.Broadcast(message);
            }

            Console.ReadLine();
        }
    }
    
    static MessageSerialization InitializeMessageSerialization()
    {
        // for ProtoBuf serialization see NuGet package "RedFoxMQ.Serialization.ProtoBuf"
        var messageSerialization = new MessageSerialization();
        
        messageSerialization.RegisterSerializer( // register serializer for each message type
            TestMessage.UniqueIdPerMessageType, 
            new TestMessageSerializer());
            
        messageSerialization.RegisterDeserializer( // register deserializer for each message type
            TestMessage.UniqueIdPerMessageType, 
            new TestMessageDeserializer());
        
        return messageSerialization;
    }
    
    class TestMessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(IMessage message)
        {
            var testMessage = (TestMessage)message;
            return Encoding.UTF8.GetBytes(testMessage.Text);
        }
    }

    class TestMessageDeserializer : IMessageDeserializer
    {
        public IMessage Deserialize(byte[] rawMessage)
        {
            return new TestMessage { Text = Encoding.UTF8.GetString(rawMessage) };
        }
    }

    class TestMessage : IMessage
    {
        public const ushort UniqueIdPerMessageType = 1;

        public ushort MessageTypeId { get { return UniqueIdPerMessageType; } }
        public string Text { get; set; }
    }
}
```

Or have a look at a standalone Request / Response example:

```c#
using RedFoxMQ;
using RedFoxMQ.Transports;
using System;
using System.Text;

class Program
{
    static void Main()
    {
        var messageSerialization = InitializeMessageSerialization();
        
        var workerFactory = new ResponderWorkerFactoryBuilder().Create(new TestHub());

        using (var responder = new Responder(workerFactory, messageSerialization))
        using (var requester = new Requester(messageSerialization))
        {
            var endpoint = new RedFoxEndpoint(RedFoxTransport.Tcp, "localhost", 5555, null);
            responder.Bind(endpoint); // call Bind multiple times to listen to multiple endpoints

            requester.Connect(endpoint);

            foreach (var text in new[] {"Hello", "World"})
            {
                var requestMessage = new TestMessage {Text = text};
                var responseMessage = (TestMessage) requester.Request(requestMessage);

                Console.WriteLine(responseMessage.Text);
            }

            Console.ReadLine();
        }
    }
    
    static MessageSerialization InitializeMessageSerialization()
    {
        // for ProtoBuf serialization see NuGet package "RedFoxMQ.Serialization.ProtoBuf"
        var messageSerialization = new MessageSerialization();
        
        messageSerialization.RegisterSerializer( // register serializer for each message type
            TestMessage.UniqueIdPerMessageType, 
            new TestMessageSerializer());
            
        messageSerialization.RegisterDeserializer( // register deserializer for each message type
            TestMessage.UniqueIdPerMessageType, 
            new TestMessageDeserializer());
        
        return messageSerialization;
    }

    class TestMessageSerializer : IMessageSerializer
    {
        public byte[] Serialize(IMessage message)
        {
            var testMessage = (TestMessage)message;
            return Encoding.UTF8.GetBytes(testMessage.Text);
        }
    }

    class TestMessageDeserializer : IMessageDeserializer
    {
        public IMessage Deserialize(byte[] rawMessage)
        {
            return new TestMessage { Text = Encoding.UTF8.GetString(rawMessage) };
        }
    }

    class TestMessage : IMessage
    {
        public const ushort UniqueIdPerMessageType = 1;

        public ushort MessageTypeId { get { return UniqueIdPerMessageType; } }
        public string Text { get; set; }
    }
    
    class TestHub
    {
        public IMessage AnyMethodNameYouLike(TestMessage message)
        {
            return new TestMessage { Text = "Response: " + message.Text };
        }

        ///// <summary>
        ///// ResponderWorkerFactoryBuilder maps all methods with single parameter derived
        ///// from IMessage and IMessage result (-> types must have different MessageTypeIds)
        ///// </summary>
        // public IMessage OtherMethodName(OtherMessage message)
        // {
        //     return new TestMessage { Text = "Other Response: " + message.Text };
        // }
		
        ///// <summary>
        ///// Default responder when message is not handled by any other specific implementation
        ///// </summary>
        // public IMessage OtherMethodName(IMessage message)
        // {
        //     return new TestMessage { Text = "Default Response: " + message };
        // }		
    } 
}
```

I recommend using [Protocol Buffers](https://code.google.com/p/protobuf-net/)
for message serialization, but it is entirely up to you!

#### Contact

Please let me know if there are bugs or if you have suggestions how to improve the code.
I accept pull requests.

And maybe follow me on Twitter [@quadfinity](https://twitter.com/quadfinity) :)
