using System;
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

}
