using System.Net.Sockets;
using System.Text;
using Kvieta.App.Services;

internal static class LocalNetworkTimeoutChecks
{
    public static async Task RunAsync()
    {
        await using var endpoint = LocalManagerDeviceEnrollmentEndpoint.Start(_ => Task.FromResult(false), DateTimeOffset.UtcNow);
        using var stalled = new TcpClient();
        await stalled.ConnectAsync(endpoint.PairingUri.Host, endpoint.PairingUri.Port);
        await stalled.GetStream().WriteAsync(Encoding.ASCII.GetBytes("GET / HTTP/1.1\r\nHost: "));
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(5) };
        using var concurrent = await client.GetAsync(endpoint.PairingUri);
        concurrent.EnsureSuccessStatusCode();
        await Task.Delay(TimeSpan.FromSeconds(13));
        using var afterTimeout = await client.GetAsync(endpoint.PairingUri);
        afterTimeout.EnsureSuccessStatusCode();
        Console.WriteLine("Local pairing survives a partial request, concurrent client and request timeout.");
    }
}
