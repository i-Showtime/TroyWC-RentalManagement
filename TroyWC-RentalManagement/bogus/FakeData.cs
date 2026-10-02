using System;
using System.Security.Cryptography;
using System.Text;
using Bogus;
using TroyWC_RentalManagement.Models;

namespace TroyWC_RentalManagement.bogus;

public static class FakeData
{
    public static List<Address> CreateAddresses(int count)
    {
        var addressFaker = new Faker<Address>()
            .UseSeed(12345) // Keeps migration seed data consistent
            .RuleFor(a => a.Line1, f => f.Address.StreetAddress())
            .RuleFor(a => a.Line2, f => f.Address.SecondaryAddress())
            .RuleFor(a => a.City, f => f.Address.City())
            .RuleFor(a => a.State, f => f.Address.State())
            .RuleFor(a => a.PostalCode, f => f.Address.ZipCode());

        return addressFaker.Generate(count);
    }

    /// <summary>
    /// A Faker with its own seeded randomizer. Never rely on Bogus's shared Randomizer.Seed or on
    /// DateTime.Now-based helpers (Date.Past, Date.Recent...) when the output must be repeatable.
    /// </summary>
    public static Faker CreateFaker(int seed) => new("en") { Random = new Randomizer(seed) };

    /// <summary>Seed derived from <paramref name="text"/> that is the same in every process and on every machine
    /// (unlike string.GetHashCode, which is randomized per process).</summary>
    public static int StableSeed(string text) =>
        BitConverter.ToInt32(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToLowerInvariant())), 0);

    /// <summary>Guid derived from <paramref name="text"/>, so seeded rows get the same key everywhere.</summary>
    public static Guid StableGuid(string text) =>
        new(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToLowerInvariant()))[..16]);

    public static Address Address(Faker f) => new()
    {
        Line1 = f.Address.StreetAddress(),
        Line2 = f.Random.Bool(0.3f) ? f.Address.SecondaryAddress() : null,
        City = f.Address.City(),
        State = f.Address.StateAbbr(),
        PostalCode = f.Address.ZipCode("#####"),
    };

    /// <summary>A format the [Phone] validation on the application forms accepts.</summary>
    public static string Phone(Faker f) => f.Phone.PhoneNumber("518-###-####");

    public static Address Copy(Address address) => new()
    {
        Line1 = address.Line1,
        Line2 = address.Line2,
        City = address.City,
        State = address.State,
        PostalCode = address.PostalCode,
    };
}
