﻿// 
// Copyright 2013-2014 Hans Wolff
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// 

using NUnit.Framework;
using RedFoxMQ.Transports;
using System;
using System.Diagnostics;
using System.Threading;

namespace RedFoxMQ.Tests
{
    [TestFixture]
    public class PublisherSubscriberTests
    {
        public static readonly TimeSpan Timeout = !Debugger.IsAttached ? TimeSpan.FromSeconds(10) : TimeSpan.FromMilliseconds(-1);

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscribe_to_Publisher_receive_single_broadcasted_message(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                publisher.Bind(endpoint);
                subscriber.Connect(endpoint);

                Thread.Sleep(100);

                var broadcastedMessage = new TestMessage { Text = "Hello" };

                publisher.Broadcast(broadcastedMessage);

                Assert.AreEqual(broadcastedMessage, subscriber.TestMustReceiveMessageWithin(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscribe_to_Publisher_twice_on_same_endpoint(RedFoxTransport transport)
        {
            for (var i = 0; i < 2; i++)
            {
                using (var publisher = new Publisher())
                using (var subscriber = new TestSubscriber())
                {
                    var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                    publisher.Bind(endpoint);
                    subscriber.Connect(endpoint);

                    var broadcastedMessage = new TestMessage { Text = "Hello" };
                    publisher.Broadcast(broadcastedMessage);
                }
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscribe_to_Publisher_receive_two_single_broadcasted_messages(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                publisher.Bind(endpoint);
                subscriber.Connect(endpoint);

                Thread.Sleep(100);

                var broadcastedMessage = new TestMessage { Text = "Hello" };

                publisher.Broadcast(broadcastedMessage);
                publisher.Broadcast(broadcastedMessage);

                Assert.AreEqual(broadcastedMessage, subscriber.TestMustReceiveMessageWithin(Timeout));
                Assert.AreEqual(broadcastedMessage, subscriber.TestMustReceiveMessageWithin(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscribe_to_Publisher_receive_two_broadcasted_messages_from_batch(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                publisher.Bind(endpoint);
                subscriber.Connect(endpoint);

                Thread.Sleep(100);

                var broadcastedMessage = new TestMessage { Text = "Hello" };

                var batch = new[] { broadcastedMessage, broadcastedMessage };
                publisher.Broadcast(batch);

                Assert.AreEqual(broadcastedMessage, subscriber.TestMustReceiveMessageWithin(Timeout));
                Assert.AreEqual(broadcastedMessage, subscriber.TestMustReceiveMessageWithin(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Publisher_ClientConnected_event_fires(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                var eventFired = new ManualResetEventSlim();

                publisher.Bind(endpoint);
                publisher.ClientConnected += (s, c) => eventFired.Set();

                subscriber.Connect(endpoint);

                Assert.IsTrue(eventFired.Wait(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Publisher_ClientDisconnected_event_fires(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                var eventFired = new ManualResetEventSlim();

                publisher.ClientDisconnected += s => eventFired.Set();
                publisher.Bind(endpoint);

                subscriber.Connect(endpoint);
                subscriber.Disconnect();

                Assert.IsTrue(eventFired.Wait(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscriber_sends_message_Publisher_receives_message(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                var eventFired = new ManualResetEventSlim();

                IMessage messageReceived = null;
                ISocket messageSocket = null;
                publisher.MessageReceived += (s, m) =>
                {
                    messageSocket = s;
                    messageReceived = m;
                    eventFired.Set();
                };

                ISocket connectedSocket = null;
                publisher.ClientConnected += (s, m) => { connectedSocket = s; };
                publisher.Bind(endpoint);
                
                subscriber.Connect(endpoint);

                IMessage messageSent = new TestMessage("test");
                subscriber.SendMessage(messageSent);

                Assert.IsTrue(eventFired.Wait(Timeout));
                Assert.AreEqual(messageSent, messageReceived);
                Assert.AreEqual(connectedSocket, messageSocket);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscriber_Disconnected_event_fires(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                var eventFired = new ManualResetEventSlim();

                publisher.Bind(endpoint);

                subscriber.Disconnected += eventFired.Set;
                subscriber.Connect(endpoint);
                subscriber.Disconnect();

                Assert.IsTrue(eventFired.Wait(Timeout));
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Publisher_Unbound_Subscriber_Disconnected_event_fires(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                var eventFired = new ManualResetEventSlim();

                publisher.Bind(endpoint);

                subscriber.Disconnected += eventFired.Set;
                subscriber.Connect(endpoint);

                publisher.Unbind(endpoint);

                Assert.IsTrue(eventFired.Wait(Timeout));
                Assert.IsTrue(subscriber.IsDisconnected);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscriber_Disconnect_doesnt_hang(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                publisher.Bind(endpoint);

                subscriber.Connect(endpoint);
                subscriber.Disconnect(true, Timeout);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscriber_IsDisconnected_should_be_false_when_connected(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                publisher.Bind(endpoint);
                subscriber.Connect(endpoint);

                Assert.IsFalse(subscriber.IsDisconnected);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void Subscriber_IsDisconnected_should_be_true_when_disconnected(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);
                publisher.Bind(endpoint);
                subscriber.Connect(endpoint);
                subscriber.Disconnect();

                Assert.IsTrue(subscriber.IsDisconnected);
            }
        }

        [TestCase(RedFoxTransport.Inproc)]
        [TestCase(RedFoxTransport.Tcp)]
        public void one_subscriber_connects_to_one_publisher_receives_message_then_second_subscriber_connects_both_receive_message(RedFoxTransport transport)
        {
            using (var publisher = new Publisher())
            using (var subscriber1 = new TestSubscriber())
            using (var subscriber2 = new TestSubscriber())
            {
                var endpoint = TestHelpers.CreateEndpointForTransport(transport);

                publisher.Bind(endpoint);
                subscriber1.Connect(endpoint);

                Thread.Sleep(100);

                var broadcastMessage = new TestMessage { Text = "Hello" };
                publisher.Broadcast(broadcastMessage);

                Assert.AreEqual(broadcastMessage, subscriber1.TestMustReceiveMessageWithin(Timeout));

                subscriber2.Connect(endpoint);

                Thread.Sleep(100);

                publisher.Broadcast(broadcastMessage);

                Assert.AreEqual(broadcastMessage, subscriber1.TestMustReceiveMessageWithin(Timeout));
                Assert.AreEqual(broadcastMessage, subscriber2.TestMustReceiveMessageWithin(Timeout));
            }
        }

        [SetUp]
        public void Setup()
        {
            TestHelpers.InitializeMessageSerialization();
        }
    }
}
