using System.Reflection;
using System.Security.Claims;
using fencemark.Client.Components.Layout;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Xunit;

namespace fencemark.Tests;

/// <summary>
/// Regression test for the "Empty Catch Block Swallows All Errors" issue:
/// MainLayout.CheckOnboardingStatus used to have a bare `catch { }` that silently
/// discarded any exception during the onboarding check. It must now log the failure.
///
/// This invokes the private CheckOnboardingStatus method directly via reflection rather
/// than through bUnit/full component rendering (not set up in this repo), which is safe
/// here because the induced failure (AuthenticationStateProvider throwing) happens before
/// the method ever touches rendering-dependent members like StateHasChanged().
/// </summary>
public class MainLayoutOnboardingTests
{
    private class ThrowingAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => throw new InvalidOperationException("Simulated failure fetching authentication state");
    }

    private class CapturingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, exception));
        }

        private class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    private static void SetInjectedProperty(object target, string propertyName, object value)
    {
        var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Expected an injected '{propertyName}' property on {target.GetType()}.");
        property.SetValue(target, value);
    }

    [Fact]
    public async Task CheckOnboardingStatus_WhenAuthStateThrows_LogsWarningInsteadOfSwallowingIt()
    {
        var layout = new MainLayout();
        var logger = new CapturingLogger<MainLayout>();

        SetInjectedProperty(layout, "AuthenticationStateProvider", new ThrowingAuthenticationStateProvider());
        SetInjectedProperty(layout, "Logger", logger);

        var method = typeof(MainLayout).GetMethod("CheckOnboardingStatus", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("CheckOnboardingStatus method not found - has MainLayout.razor been renamed/refactored?");

        // Should not throw - the exception must be caught, not propagated.
        var task = (Task)method.Invoke(layout, null)!;
        await task;

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning && e.Exception is InvalidOperationException);
    }
}
