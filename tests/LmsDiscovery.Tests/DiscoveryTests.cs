using FluentAssertions;
using LmsDiscovery.UdpClient;
using Moq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LmsDiscovery.Tests
{
    public class DiscoveryTests
    {
        private Mock<IUdpClient> udpClientMock;
        private const string discoveryPacket = "eIPAD\0NAME\0VERS\0UUID\0JSON\0CLIP\0";

        public DiscoveryTests()
        {
            var socketMock = new Mock<ISocket>();
            udpClientMock = new Mock<IUdpClient>();
            socketMock.Setup(m => m.EnableBroadcast).Returns(true);
            udpClientMock.Setup(m => m.Client.ReceiveTimeout).Returns(1000);
            udpClientMock.Setup(m => m.Client).Returns(socketMock.Object);
        }

        private void MockSend()
        {
            udpClientMock.Setup(m => m.Send(Encoding.UTF8.GetBytes(discoveryPacket), Encoding.UTF8.GetBytes(discoveryPacket).Length, It.IsAny<IPEndPoint>()))
                         .Returns(Encoding.UTF8.GetBytes(discoveryPacket).Length);
        }

        [Fact]
        public void Discover_ValidResponseReceived_ReturnsDiscoveredServer()
        {
            //Arrange
            MockSend();
            int callCount = 0;
            udpClientMock.Setup(m => m.Receive(ref It.Ref<IPEndPoint>.IsAny))
                .Returns((ref IPEndPoint ep) =>
                {
                    if (callCount == 0)
                    {
                        callCount++;
                        ep = new IPEndPoint(IPAddress.Parse("107.70.178.215"), 3483);
                        return Encoding.UTF8.GetBytes("ENAME\fMEDIA-SERVERVERS\u00059.0.2UUID$b34f68fa-e9ae-4238-b2ce-18bb48fa26a6JSON\u00049000CLIP\u00049090");
                    }
                    else
                    {
                        throw new OperationCanceledException();
                    }
                });
            var expected = new MediaServer
            {
                Name = "MEDIA-SERVER",
                Version = new Version("9.0.2"),
                UUID = new Guid("b34f68fa-e9ae-4238-b2ce-18bb48fa26a6"),
                Json = 9000,
                Clip = 9090,
                IPAddress = IPAddress.Parse("107.70.178.215")
            };

            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;

            //Act
            var response = Discovery.Discover(cancellationToken, TimeSpan.FromSeconds(1), udpClientMock.Object);

            //Assert
            response.Should().ContainEquivalentOf(expected);
        }

        [Fact]
        public void Discover_ReceivesDiscoveryPacket_ReturnsEmptyList()
        {
            // Arrange
            int callCount = 0;
            udpClientMock.Setup(m => m.Receive(ref It.Ref<IPEndPoint>.IsAny))
                .Returns((ref IPEndPoint ep) =>
                {
                    if (callCount == 0)
                    {
                        callCount++;
                        ep = new IPEndPoint(IPAddress.Parse("107.70.178.215"), 3483);
                        return Encoding.UTF8.GetBytes(discoveryPacket);
                    }
                    if (callCount == 1)
                    {
                        callCount++;
                        ep = new IPEndPoint(IPAddress.Parse("230.186.191.21"), 3483);
                        return Encoding.UTF8.GetBytes(discoveryPacket);
                    }
                    else
                    {
                        throw new OperationCanceledException();
                    }
                });

            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;

            //Act
            var response = Discovery.Discover(cancellationToken, TimeSpan.FromSeconds(1), udpClientMock.Object);

            //Assert
            response.Should().BeEmpty();

        }

        [Fact]
        public void Discover_ValidResponsesReceived_ReturnsDiscoveredServers()
        {
            //Arrange
            int callCount = 0;
            udpClientMock.Setup(m => m.Receive(ref It.Ref<IPEndPoint>.IsAny))
                .Returns((ref IPEndPoint ep) =>
                {
                    if (callCount == 0)
                    {
                        callCount++;
                        ep = new IPEndPoint(IPAddress.Parse("107.70.178.215"), 3483);
                        return Encoding.UTF8.GetBytes("ENAME\fMEDIA-SERVERVERS\u00059.0.2UUID$b34f68fa-e9ae-4238-b2ce-18bb48fa26a6JSON\u00049000CLIP\u00049090");
                    }
                    if (callCount == 1)
                    {
                        callCount++;
                        ep = new IPEndPoint(IPAddress.Parse("230.186.191.21"), 3483);
                        return Encoding.UTF8.GetBytes("ENAME\u0012LIVING-ROOM-SERVERVERS\u00059.0.2UUID$a12c34ef-b567-8901-d234-56ef78901234JSON\u00049000CLIP\u00049090");
                    }
                    else
                    {
                        throw new OperationCanceledException();
                    }
                });
            var servers = new List<MediaServer>()
            {
                new MediaServer
                {
                    Name = "MEDIA-SERVER",
                    Version = new Version("9.0.2"),
                    UUID = new Guid("b34f68fa-e9ae-4238-b2ce-18bb48fa26a6"),
                    Json = 9000,
                    Clip = 9090,
                    IPAddress = IPAddress.Parse("107.70.178.215")
                },
                new MediaServer
                {
                    Name = "LIVING-ROOM-SERVER",
                    Version = new Version("9.0.2"),
                    UUID = new Guid("a12c34ef-b567-8901-d234-56ef78901234"),
                    Json = 9000,
                    Clip = 9090,
                    IPAddress = IPAddress.Parse("230.186.191.21")
                }
            };

            var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;

            //Act
            var response = Discovery.Discover(cancellationToken, TimeSpan.FromSeconds(1), udpClientMock.Object);

            //Assert
            response.Should().BeEquivalentTo(servers);
        }

        [Fact]
        public void Discover_NoResponseReceived_CallsDispose()
        {
            // Arrange
            MockSend();
            udpClientMock.Setup(m => m.Receive(ref It.Ref<IPEndPoint>.IsAny))
                         .Throws(new SocketException((int)SocketError.TimedOut));

            var cancellationToken = new CancellationTokenSource().Token;

            // Act
            Discovery.Discover(cancellationToken, TimeSpan.FromSeconds(1), udpClientMock.Object);

            // Assert
            udpClientMock.Verify(m => m.Dispose(), Times.Once);

        }

        [Fact]
        public void Discover_NoResponseReceived_ReturnsEmptyList()
        {
            // Arrange
            MockSend();
            udpClientMock.Setup(m => m.Receive(ref It.Ref<IPEndPoint>.IsAny))
                         .Throws(new SocketException((int)SocketError.TimedOut));

            var cancellationToken = new CancellationTokenSource().Token;

            // Act
            var response = Discovery.Discover(cancellationToken, TimeSpan.FromSeconds(1), udpClientMock.Object);

            // Assert
            response.Should().BeEmpty();

        }

        [Fact]
        public void Discover_RequestTimeoutExceeded_ReturnsEmptyList()
        {
            // Arrange
            MockSend();
            udpClientMock.Setup(m => m.Receive(ref It.Ref<IPEndPoint>.IsAny))
                         .Throws(new SocketException((int)SocketError.TimedOut));
            var cancellationToken = new CancellationTokenSource().Token;

            //Act
            var response = Discovery.Discover(cancellationToken, TimeSpan.FromSeconds(1), udpClientMock.Object);

            //Assert
            response.Should().BeEmpty();
        }

        [Fact]
        public void Discover_CancellationTokensCancellationRequested_ReturnsEmptyList()
        {
            // Arrange
            MockSend();
            var cancellationToken = new CancellationTokenSource();
            udpClientMock.Setup(m => m.Receive(ref It.Ref<IPEndPoint>.IsAny))
                .Returns((ref IPEndPoint ep) =>
                {
                    cancellationToken.Cancel();
                    ep = new IPEndPoint(IPAddress.Parse("107.70.178.215"), 3483);
                    return Encoding.UTF8.GetBytes(discoveryPacket);

                });

            //Act
            var response = Discovery.Discover(cancellationToken.Token, TimeSpan.MaxValue, udpClientMock.Object);

            //Assert
            response.Should().BeEmpty();
        }
    }
}