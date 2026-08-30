using NaHora.Domain.Attendance;

namespace NaHora.UnitTests.Attendance;

/// <summary>
/// The rules that decide what a punch records and how much it can be trusted.
/// </summary>
public sealed class PunchTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 30, 11, 32, 0, TimeSpan.Zero);

    // São Paulo. Chosen rather than UTC because a zero offset would let a bug
    // in the local-date calculation pass unnoticed.
    private static readonly TimeSpan CompanyOffset = TimeSpan.FromHours(-3);

    [Fact]
    public void Register_WithoutLocation_IsRejected()
    {
        // Arrange & Act — the one thing a punch cannot be without
        var result = Punch.Register(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "client-1",
            nsr: 1,
            PunchDirection.Entry,
            PunchOrigin.Online,
            Now,
            Now,
            CompanyOffset,
            location: null,
            DeviceIntegrity.Create("device-1", locationWasMocked: false, Now, Now));

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "LOCATION_REQUIRED");
    }

    [Fact]
    public void Register_WithoutClientId_IsRejected()
    {
        // Arrange & Act — without it a retry becomes a second punch
        var result = Register(clientId: "   ");

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldContain(error => error.Code == "CLIENT_ID_REQUIRED");
    }

    [Fact]
    public void Register_FreezesTheLocalDateAtTheCompanyOffset()
    {
        // Arrange — 11:32 UTC is 08:32 in São Paulo, still the same day
        var result = Register();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.LocalDate.ShouldBe(new DateOnly(2026, 8, 30));
        result.Value.CompanyOffset.ShouldBe(CompanyOffset);
    }

    [Fact]
    public void Register_NearMidnightUtc_UsesTheCompanyDayAndNotTheUtcDay()
    {
        // Arrange — 01:00 UTC on the 30th is 22:00 on the 29th in São Paulo.
        // Getting this wrong moves a night-shift punch into the wrong working
        // day, which is the whole reason the offset is frozen.
        var lateNight = new DateTimeOffset(2026, 8, 30, 1, 0, 0, TimeSpan.Zero);

        // Act
        var result = Register(occurredAt: lateNight);

        // Assert
        result.Value.LocalDate.ShouldBe(new DateOnly(2026, 8, 29));
    }

    [Fact]
    public void Register_WhenEverythingLooksNormal_IsTrusted()
    {
        // Arrange & Act
        var result = Register();

        // Assert
        result.Value.Trust.ShouldBe(PunchTrust.Trusted);
    }

    [Fact]
    public void Register_WithMockedLocation_NeedsReviewAndIsStillRecorded()
    {
        // Arrange — Art. 78 forbids refusing the record, so suspicion changes
        // the classification and never the outcome
        var result = Register(
            integrity: DeviceIntegrity.Create("device-1", locationWasMocked: true, Now, Now));

        // Assert
        result.IsSuccess.ShouldBeTrue("a suspicious punch is still a punch");
        result.Value.Trust.ShouldBe(PunchTrust.NeedsReview);
    }

    [Fact]
    public void Register_WithADeviceClockHoursAhead_NeedsReview()
    {
        // Arrange — the other way to fake a punch: move the phone clock
        var deviceClock = Now.AddHours(2);

        // Act
        var result = Register(
            integrity: DeviceIntegrity.Create("device-1", locationWasMocked: false, deviceClock, Now));

        // Assert
        result.Value.Trust.ShouldBe(PunchTrust.NeedsReview);
        result.Value.Integrity.ClockSkewSeconds.ShouldBe(7200);
    }

    [Fact]
    public void Register_WithASlightlyOffClock_StaysTrusted()
    {
        // Arrange — a phone that has not synced recently is not a fraudster
        var result = Register(
            integrity: DeviceIntegrity.Create(
                "device-1",
                locationWasMocked: false,
                Now.AddSeconds(90),
                Now));

        // Assert
        result.Value.Trust.ShouldBe(PunchTrust.Trusted);
    }

    [Fact]
    public void Register_WithPoorAccuracy_IsDegradedAndStillRecorded()
    {
        // Arrange — inside a warehouse the fix degrades, and the employee still
        // has the right to punch
        var result = Register(accuracyMeters: 350);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Trust.ShouldBe(PunchTrust.Degraded);
    }

    [Fact]
    public void Register_FromTheOfflineQueue_IsDegradedButNotSuspicious()
    {
        // Arrange — offline is what the app does in a basement. It lowers what
        // the record proves without implying anything about the person.
        var result = Register(origin: PunchOrigin.Offline);

        // Assert
        result.Value.Trust.ShouldBe(PunchTrust.Degraded);
    }

    [Fact]
    public void Punch_ExposesNoWayToChangeARecordedValue()
    {
        // Arrange — the immutability Portaria 671 requires. A public setter
        // here would be a way to rewrite a record that is already in a signed
        // AFD, and this test is what stops one being added by habit.
        var settable = typeof(Punch)
            .GetProperties()
            .Where(property => property.SetMethod?.IsPublic == true)
            .Select(property => property.Name)
            .ToArray();

        // Assert
        settable.ShouldBeEmpty($"a recorded punch must never change: {string.Join(", ", settable)}");
    }

    [Fact]
    public void Punch_HasNoMethodThatMutatesIt()
    {
        // Arrange & Act — Register is the only way in, and it is static
        var mutators = typeof(Punch)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance
                        | System.Reflection.BindingFlags.DeclaredOnly)

            // Property getters are compiler-generated methods on the same
            // surface; only hand-written members are of interest here.
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name)
            .ToArray();

        // Assert
        mutators.ShouldBeEmpty($"a punch is written once: {string.Join(", ", mutators)}");
    }

    private static Shared.Results.Result<Punch> Register(
        string clientId = "client-1",
        PunchOrigin origin = PunchOrigin.Online,
        DateTimeOffset? occurredAt = null,
        double accuracyMeters = 12,
        DeviceIntegrity? integrity = null)
    {
        var moment = occurredAt ?? Now;

        return Punch.Register(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            clientId,
            nsr: 1,
            PunchDirection.Entry,
            origin,
            moment,
            moment,
            CompanyOffset,
            GeoLocation.Create(-23.5505, -46.6333, accuracyMeters, moment).Value,
            integrity ?? DeviceIntegrity.Create("device-1", locationWasMocked: false, moment, moment));
    }
}
