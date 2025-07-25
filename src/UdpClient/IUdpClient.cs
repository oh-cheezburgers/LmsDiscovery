using System.Net;

namespace LyrionDiscovery.UdpClient;

/// <summary>
/// An abstraction of the <see cref="UdpClient"/> class to facilitate testing and abstraction.
/// This interface allows for mocking and testing without relying on the actual network stack.
/// </summary>
public interface IUdpClient : IDisposable
{
    /// <summary>
    /// Gets or sets the underlying socket used by the UDP client.
    /// </summary>
    ISocket Client { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the UDP client can send broadcast packets.
    /// </summary>
    bool EnableBroadcast { get; set; }

    /// <summary>
    /// Receives a UDP datagram sent by a remote host.
    /// </summary>
    /// <param name="endPoint">The remote endpoint from which the datagram was sent. This parameter is passed by reference and will be updated with the sender's endpoint.</param>
    /// <returns>The datagram received as a byte array.</returns>
    byte[] Receive(ref IPEndPoint endPoint);

    /// <summary>
    /// Sends a UDP datagram to a specified remote endpoint.
    /// </summary>
    /// <param name="datagram">The data to send.</param>
    /// <param name="bytes">The number of bytes to send from the datagram.</param>
    /// <param name="endPoint">The remote endpoint to which the datagram is sent.</param>
    /// <returns>The number of bytes sent.</returns>
    int Send(byte[] datagram, int bytes, IPEndPoint endPoint);
}