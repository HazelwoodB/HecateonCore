using Lullaby.Models;
using Lullaby.Services;

namespace Lullaby;

/// <summary>
/// Test data generator for Nyphos system testing
/// Creates realistic health event scenarios to test state transitions
/// </summary>
public class TestDataGenerator
{
    private readonly HealthTrackingService _healthService;
    private readonly string _testDeviceId = "TEST_DEVICE_001";

    public TestDataGenerator(HealthTrackingService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>
    /// Scenario 1: Stable Green State
    /// Consistent 7-8 hours sleep, stable mood, regular routines
    /// </summary>
    public async Task GenerateGreenStateScenario()
    {
        Console.WriteLine("[TEST] Generating Green State scenario...");
        
        var now = DateTime.UtcNow;
        for (int daysAgo = 7; daysAgo >= 0; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            
            // Sleep: 7-8 hours, good quality
            var sleepStart = date.Date.AddHours(23);
            var sleepEnd = sleepStart.AddHours(7.5);
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Sleep,
                RecordedAtUtc = sleepEnd,
                DeviceId = _testDeviceId,
                SleepStartUtc = sleepStart,
                SleepEndUtc = sleepEnd,
                SleepQuality = 4,
                Note = "Good night's sleep"
            }, _testDeviceId);

            // Mood: Stable, neutral-positive
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Mood,
                RecordedAtUtc = date.Date.AddHours(10),
                DeviceId = _testDeviceId,
                MoodScore = 7,
                MoodLabel = "Good",
                Note = "Feeling stable and energized"
            }, _testDeviceId);

            // Routine: Morning routine completed
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Routine,
                RecordedAtUtc = date.Date.AddHours(8),
                DeviceId = _testDeviceId,
                RoutineName = "Morning Routine",
                RoutineCompleted = true,
                Note = "Completed on time"
            }, _testDeviceId);
        }
        
        Console.WriteLine("[TEST] Green State scenario generated (7 days of stable data)");
    }

    /// <summary>
    /// Scenario 2: Yellow State (Attention)
    /// Declining sleep, mild mood variation
    /// </summary>
    public async Task GenerateYellowStateScenario()
    {
        Console.WriteLine("[TEST] Generating Yellow State scenario...");
        
        var now = DateTime.UtcNow;
        for (int daysAgo = 7; daysAgo >= 0; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            
            // Sleep: Gradually declining (7h → 6h → 5.5h)
            var sleepHours = 7.0 - (daysAgo < 4 ? (3 - daysAgo) * 0.5 : 0);
            var sleepStart = date.Date.AddHours(23.5);
            var sleepEnd = sleepStart.AddHours(sleepHours);
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Sleep,
                RecordedAtUtc = sleepEnd,
                DeviceId = _testDeviceId,
                SleepStartUtc = sleepStart,
                SleepEndUtc = sleepEnd,
                SleepQuality = daysAgo < 3 ? 2 : 3,
                Note = daysAgo < 3 ? "Restless night" : "Okay sleep"
            }, _testDeviceId);

            // Mood: Slightly declining
            var moodScore = daysAgo < 3 ? 5 : 6;
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Mood,
                RecordedAtUtc = date.Date.AddHours(10),
                DeviceId = _testDeviceId,
                MoodScore = moodScore,
                MoodLabel = moodScore < 6 ? "Meh" : "Okay",
                Note = daysAgo < 3 ? "Feeling a bit off" : "Doing okay"
            }, _testDeviceId);

            // Routine: Some missed routines
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Routine,
                RecordedAtUtc = date.Date.AddHours(8),
                DeviceId = _testDeviceId,
                RoutineName = "Morning Routine",
                RoutineCompleted = daysAgo >= 2,
                Note = daysAgo < 2 ? "Skipped - running late" : "Completed"
            }, _testDeviceId);
        }
        
        Console.WriteLine("[TEST] Yellow State scenario generated (declining sleep pattern)");
    }

    /// <summary>
    /// Scenario 3: Orange State (Downshift)
    /// Significant sleep disruption, mood elevation
    /// </summary>
    public async Task GenerateOrangeStateScenario()
    {
        Console.WriteLine("[TEST] Generating Orange State scenario...");
        
        var now = DateTime.UtcNow;
        for (int daysAgo = 7; daysAgo >= 0; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            
            // Sleep: Severely disrupted (<5 hours recent nights)
            var sleepHours = daysAgo < 3 ? 4.0 : (daysAgo < 5 ? 5.5 : 6.5);
            var sleepStart = date.Date.AddHours(daysAgo < 3 ? 1 : 23); // Late bedtime recent nights
            var sleepEnd = sleepStart.AddHours(sleepHours);
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Sleep,
                RecordedAtUtc = sleepEnd,
                DeviceId = _testDeviceId,
                SleepStartUtc = sleepStart,
                SleepEndUtc = sleepEnd,
                SleepQuality = daysAgo < 3 ? 1 : 2,
                Note = daysAgo < 3 ? "Couldn't fall asleep, mind racing" : "Restless"
            }, _testDeviceId);

            // Mood: Elevated energy (manic indicator)
            var moodScore = daysAgo < 3 ? 9 : 7;
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Mood,
                RecordedAtUtc = date.Date.AddHours(10),
                DeviceId = _testDeviceId,
                MoodScore = moodScore,
                MoodLabel = daysAgo < 3 ? "Energized" : "Good",
                Note = daysAgo < 3 ? "Feeling amazing, lots of ideas!" : "Pretty good"
            }, _testDeviceId);

            // Activity: Increased activity recent days
            if (daysAgo < 4)
            {
                await _healthService.RecordHealthEventAsync(new HealthEvent
                {
                    EventType = HealthEventType.Activity,
                    RecordedAtUtc = date.Date.AddHours(14),
                    DeviceId = _testDeviceId,
                    ActivityType = "Exercise",
                    DurationMinutes = daysAgo < 2 ? 90 : 60,
                    Note = daysAgo < 2 ? "Intense workout, felt great!" : "Good session"
                }, _testDeviceId);
            }

            // Routine: Skipped or rushed
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Routine,
                RecordedAtUtc = date.Date.AddHours(8),
                DeviceId = _testDeviceId,
                RoutineName = "Morning Routine",
                RoutineCompleted = daysAgo >= 3,
                Note = daysAgo < 3 ? "Too much to do, skipped" : "Rushed through it"
            }, _testDeviceId);
        }
        
        Console.WriteLine("[TEST] Orange State scenario generated (sleep disruption + mood elevation)");
    }

    /// <summary>
    /// Scenario 4: Red State (Crisis)
    /// Critical sleep deprivation, high mood instability
    /// </summary>
    public async Task GenerateRedStateScenario()
    {
        Console.WriteLine("[TEST] Generating Red State scenario...");
        
        var now = DateTime.UtcNow;
        for (int daysAgo = 7; daysAgo >= 0; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            
            // Sleep: Critical deprivation (<4 hours multiple nights)
            var sleepHours = daysAgo < 4 ? 3.0 : (daysAgo < 6 ? 4.5 : 5.5);
            var sleepStart = date.Date.AddHours(daysAgo < 4 ? 2 : 0.5); // Very late bedtimes
            var sleepEnd = sleepStart.AddHours(sleepHours);
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Sleep,
                RecordedAtUtc = sleepEnd,
                DeviceId = _testDeviceId,
                SleepStartUtc = sleepStart,
                SleepEndUtc = sleepEnd,
                SleepQuality = 1,
                Note = daysAgo < 4 ? "Barely slept, can't shut mind off" : "Very restless"
            }, _testDeviceId);

            // Mood: Highly elevated or volatile
            var moodScore = daysAgo < 4 ? 10 : 8;
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Mood,
                RecordedAtUtc = date.Date.AddHours(10),
                DeviceId = _testDeviceId,
                MoodScore = moodScore,
                MoodLabel = daysAgo < 4 ? "Euphoric" : "Energized",
                Note = daysAgo < 4 ? "Feel incredible, unstoppable!" : "Really good energy"
            }, _testDeviceId);

            // Multiple mood logs showing volatility (recent days)
            if (daysAgo < 3)
            {
                await _healthService.RecordHealthEventAsync(new HealthEvent
                {
                    EventType = HealthEventType.Mood,
                    RecordedAtUtc = date.Date.AddHours(16),
                    DeviceId = _testDeviceId,
                    MoodScore = 4,
                    MoodLabel = "Irritable",
                    Note = "Suddenly feeling frustrated"
                }, _testDeviceId);
            }

            // Activity: Excessive activity
            if (daysAgo < 5)
            {
                await _healthService.RecordHealthEventAsync(new HealthEvent
                {
                    EventType = HealthEventType.Activity,
                    RecordedAtUtc = date.Date.AddHours(14),
                    DeviceId = _testDeviceId,
                    ActivityType = "Exercise",
                    DurationMinutes = 120,
                    Note = "Long run, felt amazing"
                }, _testDeviceId);
            }

            // Routine: Completely disrupted
            await _healthService.RecordHealthEventAsync(new HealthEvent
            {
                EventType = HealthEventType.Routine,
                RecordedAtUtc = date.Date.AddHours(8),
                DeviceId = _testDeviceId,
                RoutineName = "Morning Routine",
                RoutineCompleted = false,
                Note = "No time, too busy with projects"
            }, _testDeviceId);
        }
        
        Console.WriteLine("[TEST] Red State scenario generated (critical sleep + high volatility)");
    }

    /// <summary>
    /// Scenario 5: State Transition Test
    /// Shows progression through states with hysteresis
    /// </summary>
    public async Task GenerateStateTransitionScenario()
    {
        Console.WriteLine("[TEST] Generating State Transition scenario...");
        
        var now = DateTime.UtcNow;
        
        // Days 14-10: Green (stable baseline)
        for (int daysAgo = 14; daysAgo >= 10; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            await CreateGreenDayData(date);
        }
        
        // Days 9-7: Decline to Yellow
        for (int daysAgo = 9; daysAgo >= 7; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            await CreateYellowDayData(date);
        }
        
        // Days 6-4: Worsen to Orange
        for (int daysAgo = 6; daysAgo >= 4; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            await CreateOrangeDayData(date);
        }
        
        // Days 3-1: Brief improvement (test hysteresis - should stay Orange due to cooldown)
        for (int daysAgo = 3; daysAgo >= 1; daysAgo--)
        {
            var date = now.AddDays(-daysAgo);
            await CreateYellowDayData(date);
        }
        
        // Day 0: Recovery
        await CreateGreenDayData(now);
        
        Console.WriteLine("[TEST] State Transition scenario generated (14 days showing progression)");
    }

    private async Task CreateGreenDayData(DateTime date)
    {
        // 7-8 hours sleep, quality 4-5
        var sleepStart = date.Date.AddHours(23);
        await _healthService.RecordHealthEventAsync(new HealthEvent
        {
            EventType = HealthEventType.Sleep,
            RecordedAtUtc = sleepStart.AddHours(7.5),
            DeviceId = _testDeviceId,
            SleepStartUtc = sleepStart,
            SleepEndUtc = sleepStart.AddHours(7.5),
            SleepQuality = 4,
            Note = "Good sleep"
        }, _testDeviceId);

        await _healthService.RecordHealthEventAsync(new HealthEvent
        {
            EventType = HealthEventType.Mood,
            RecordedAtUtc = date.Date.AddHours(10),
            DeviceId = _testDeviceId,
            MoodScore = 7,
            MoodLabel = "Good"
        }, _testDeviceId);
    }

    private async Task CreateYellowDayData(DateTime date)
    {
        // 5-6 hours sleep, quality 2-3
        var sleepStart = date.Date.AddHours(23.5);
        await _healthService.RecordHealthEventAsync(new HealthEvent
        {
            EventType = HealthEventType.Sleep,
            RecordedAtUtc = sleepStart.AddHours(5.5),
            DeviceId = _testDeviceId,
            SleepStartUtc = sleepStart,
            SleepEndUtc = sleepStart.AddHours(5.5),
            SleepQuality = 2,
            Note = "Restless"
        }, _testDeviceId);

        await _healthService.RecordHealthEventAsync(new HealthEvent
        {
            EventType = HealthEventType.Mood,
            RecordedAtUtc = date.Date.AddHours(10),
            DeviceId = _testDeviceId,
            MoodScore = 5,
            MoodLabel = "Meh"
        }, _testDeviceId);
    }

    private async Task CreateOrangeDayData(DateTime date)
    {
        // <5 hours sleep, quality 1-2
        var sleepStart = date.Date.AddHours(1);
        await _healthService.RecordHealthEventAsync(new HealthEvent
        {
            EventType = HealthEventType.Sleep,
            RecordedAtUtc = sleepStart.AddHours(4),
            DeviceId = _testDeviceId,
            SleepStartUtc = sleepStart,
            SleepEndUtc = sleepStart.AddHours(4),
            SleepQuality = 1,
            Note = "Can't sleep"
        }, _testDeviceId);

        await _healthService.RecordHealthEventAsync(new HealthEvent
        {
            EventType = HealthEventType.Mood,
            RecordedAtUtc = date.Date.AddHours(10),
            DeviceId = _testDeviceId,
            MoodScore = 9,
            MoodLabel = "Energized"
        }, _testDeviceId);
    }
}
