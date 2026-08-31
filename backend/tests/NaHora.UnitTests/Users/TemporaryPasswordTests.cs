using NaHora.Domain.Users;
using NaHora.Domain.ValueObjects;

namespace NaHora.UnitTests.Users;

/// <summary>
/// The password a new employee is sent.
/// </summary>
public sealed class TemporaryPasswordTests
{
    [Fact]
    public void Generate_AlwaysSatisfiesThePolicy()
    {
        // Arrange — by construction, not by luck: a generator that fails the
        // policy one time in fifty produces a hire nobody can sign in as
        var cpf = Cpf.Create("52998224725").Value;
        var email = EmailAddress.Create("joana@nahora.dev").Value;

        // Act & Assert
        for (var attempt = 0; attempt < 200; attempt++)
        {
            var password = TemporaryPassword.Generate();

            PasswordPolicy.Validate(password, cpf, "Joana Funcionária", email)
                .IsSuccess.ShouldBeTrue($"'{password}' failed the policy");
        }
    }

    [Fact]
    public void Generate_LeavesOutTheGlyphsThatArgueOverThePhone()
    {
        // Arrange — l, I, 1, O and 0 turn a password read aloud into three
        // attempts and a reset
        const string ambiguous = "lI1O0";

        // Act & Assert
        for (var attempt = 0; attempt < 200; attempt++)
        {
            TemporaryPassword.Generate().ShouldNotContain(character => ambiguous.Contains(character));
        }
    }

    [Fact]
    public void Generate_DoesNotRepeatItself()
    {
        // Arrange & Act
        var passwords = Enumerable.Range(0, 100).Select(_ => TemporaryPassword.Generate()).ToList();

        // Assert
        passwords.Distinct().Count().ShouldBe(passwords.Count);
    }

    [Fact]
    public void Generate_DoesNotPutTheCharacterClassesInAFixedOrder()
    {
        // Arrange — seeding one of each class and stopping there would make the
        // first four positions lower, upper, digit, symbol every time, which is
        // a pattern worth not publishing
        var firstIsLower = Enumerable.Range(0, 100)
            .Select(_ => TemporaryPassword.Generate()[0])
            .Count(char.IsLower);

        // Assert
        firstIsLower.ShouldBeLessThan(100);
    }
}
