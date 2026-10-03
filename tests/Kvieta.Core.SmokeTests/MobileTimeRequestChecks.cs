using Kvieta.App.Services;

internal static class MobileTimeRequestChecks
{
    public static void Run()
    {
        string directory = Path.Combine(Path.GetTempPath(), "Kvieta-MobileChecks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            DateTimeOffset now = new(2026, 9, 22, 12, 0, 0, TimeSpan.FromHours(3));
            string? room = "test-room";
            MobileTimeRequestRepository repository = new(Path.Combine(directory, "request.bin"), () => now, () => room);
            void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
            MobileTimeDecision Approve(MobileTimeRequest request, int minutes = 30) =>
                new(Guid.NewGuid().ToString(), request.Id, "approve", minutes, now.ToUniversalTime());
            var request = repository.Create(30, "Homework");
            Check(!repository.AcceptDecision(Approve(request, 181)), "Out-of-range grant accepted.");
            Check(repository.AcceptDecision(Approve(request)), "Valid grant rejected.");
            Check(!repository.AcceptDecision(Approve(request)), "Duplicate grant accepted.");
            var claim = repository.TryClaimApproved(15);
            Check(claim?.TargetBonusMinutes == 45, "Incorrect target.");
            Check(repository.TryClaimApproved(15) is null, "Concurrent claim accepted.");
            repository.ReleaseClaim(request.Id);
            Check(repository.TryClaimApproved(45)?.TargetBonusMinutes == 45, "Retry would double credit.");
            now = now.AddMinutes(3);
            Check(repository.TryClaimApproved(45)?.TargetBonusMinutes == 45, "Crash recovery would double credit.");
            repository.MarkApplied(request.Id);
            Check(repository.Current()?.Status == MobileTimeRequestStatus.Applied, "Not applied.");
            Check(repository.TryClaimApproved(45) is null, "Applied grant claimed twice.");

            request = repository.Create(30, null);
            room = "replacement-room";
            Check(!repository.AcceptDecision(Approve(request)), "Revoked room accepted.");
            Check(repository.Current()?.Status == MobileTimeRequestStatus.Expired, "Revoked request not expired.");
            request = repository.Create(30, null);
            now = now.AddMinutes(31);
            Check(!repository.AcceptDecision(Approve(request)), "Expired request accepted.");

            now = new(2026, 9, 22, 23, 55, 0, TimeSpan.FromHours(3));
            request = repository.Create(30, null);
            Check(repository.AcceptDecision(Approve(request)), "Before-midnight approval failed.");
            now = now.AddMinutes(10);
            Check(repository.TryClaimApproved(0) is null, "Yesterday's grant crossed into a new ledger.");

            request = repository.Create(180, null);
            Check(repository.AcceptDecision(Approve(request, 180)), "Maximum valid grant failed.");
            Check(repository.TryClaimApproved(1400)?.TargetBonusMinutes == 1440, "Daily cap exceeded.");
            Console.WriteLine("Mobile request checks passed: duplicate, retry, crash recovery, expiry, revocation, day rollover, cap.");
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
