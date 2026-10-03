using System.Text.Json;
using Kvieta.App.Services;

internal static class RelayInteropChecks
{
    public static async Task CreateAsync(string path)
    {
        RelayCredentials credentials = RelayCredentials.Create();
        object snapshot = new
        {
            deviceName = "Relay integration test", mode = "Personal", sessionState = "Active",
            localDay = DateTime.UtcNow.ToString("yyyy-MM-dd"), observedAtUtc = DateTimeOffset.UtcNow,
            stale = false, usedSeconds = 123, remainingSeconds = 456,
            applications = new[] { new { name = "Synthetic", seconds = 123 } },
            timeRequest = new { id = "11111111-1111-1111-1111-111111111111", requestedMinutes = 30, note = "Synthetic request",
                createdAtUtc = DateTimeOffset.UtcNow, expiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(30), status = "Pending",
                grantedMinutes = (int?)null, appliedAtUtc = (DateTimeOffset?)null }
        };
        long sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var fixture = new { credentials, envelope = new { sequence, box = DashboardRelay.Encrypt(credentials.Client, snapshot, sequence) } };
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(fixture, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        await DashboardRelay.PublishAsync(credentials, snapshot, CancellationToken.None);
        Console.WriteLine("Synthetic encrypted relay fixture published. No real usage data sent.");
    }
    public static async Task CheckDecisionAsync(string path)
    {
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var credentials = fixture.RootElement.GetProperty("credentials").Deserialize<RelayCredentials>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        MobileTimeDecision? decision = await DashboardRelay.ReadDecisionAsync(credentials, CancellationToken.None);
        if (decision is null || decision.RequestId != "11111111-1111-1111-1111-111111111111" ||
            decision.Action != "approve" || decision.GrantedMinutes != 30)
            throw new InvalidOperationException("Android relay decision did not round-trip to .NET.");
        Console.WriteLine("Android encrypted decision decrypted and validated by the desktop client.");
    }
    public static async Task CleanupAsync(string path)
    {
        using var fixture = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var credentials = fixture.RootElement.GetProperty("credentials").Deserialize<RelayCredentials>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        await DashboardRelay.RevokeAsync(credentials, CancellationToken.None);
        File.Delete(path);
        Console.WriteLine("Synthetic relay fixture revoked and local test keys removed.");
    }
}
