using System.Runtime.CompilerServices;
using System.Text;
using GM.Messaging.Domain.Events;

namespace GM.Messaging.Sample.Tests;

/// <summary>
/// A stand-in protector for the unit tests (base64, not encryption): the hosts use AddGMSensitiveData with a
/// real key ring. Set once, before any test writes an event with [SensitiveData] properties.
/// </summary>
internal sealed class TestSensitiveDataProtector : ISensitiveDataProtector
{
    public string Protect(string plaintext, string? subject) => Convert.ToBase64String(Encoding.UTF8.GetBytes(plaintext));

    public string? Unprotect(string ciphertext) => Encoding.UTF8.GetString(Convert.FromBase64String(ciphertext));

    [ModuleInitializer]
    internal static void Register() => IntegrationEventRegistry.Shared.SensitiveData ??= new TestSensitiveDataProtector();
}
