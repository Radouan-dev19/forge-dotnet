using ForgeDotNet.Domain.SqlLab;

namespace ForgeDotNet.UnitTests;

public sealed class SqlLabRulesTests
{
    [Fact]
    public void StatementGuardAcceptsOneDmlStatementAndIgnoresLiteralsAndComments()
    {
        IReadOnlyList<string> issues = SqlStatementGuard.Validate(
            "SELECT 'DROP LOGIN', OrderId FROM dbo.Orders -- USE master\nORDER BY OrderId;",
            16_384);

        Assert.Empty(issues);
    }

    [Theory]
    [InlineData("CREATE LOGIN attacker WITH PASSWORD = 'Unsafe1!';", "CREATE")]
    [InlineData("EXEC master.dbo.xp_cmdshell 'whoami';", "EXEC")]
    [InlineData("USE master;", "USE")]
    [InlineData("SELECT * FROM other_database.dbo.Orders;", "inter-base")]
    [InlineData("SELECT 1; SELECT 2;", "Une seule instruction")]
    public void StatementGuardRejectsServerCrossDatabaseAndBatchCommands(string query, string expectedIssue)
    {
        IReadOnlyList<string> issues = SqlStatementGuard.Validate(query, 16_384);

        Assert.Contains(issues, issue => issue.Contains(expectedIssue, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StatementGuardRejectsUnterminatedInputAndOversizedQueries()
    {
        Assert.Contains(
            SqlStatementGuard.Validate("SELECT 'unterminated", 16_384),
            issue => issue.Contains("non terminé", StringComparison.Ordinal));
        Assert.Contains(
            SqlStatementGuard.Validate("SELECT 12345", 5),
            issue => issue.Contains("limite", StringComparison.Ordinal));
    }

    [Fact]
    public void OrderedValidationChecksColumnOrderRowOrderAndNumericTolerance()
    {
        var expectation = new SqlLabExpectedResult(
            ["Id", "Total"],
            [[new("1"), new("10.00")], [new("2"), new("20.00")]],
            Ordered: true,
            NumericTolerance: 0.01m);
        var accepted = new SqlLabResultSet(
            [new("Id", "int", false), new("Total", "decimal", false)],
            [[new("1"), new("10.009")], [new("2"), new("20")]]);
        var reversed = accepted with { Rows = accepted.Rows.Reverse().ToArray() };

        Assert.True(SqlResultValidator.Validate(expectation, accepted).Passed);
        Assert.False(SqlResultValidator.Validate(expectation, reversed).Passed);
    }

    [Fact]
    public void UnorderedValidationAcceptsPermutationButRejectsWrongValue()
    {
        var expectation = new SqlLabExpectedResult(
            ["Name"],
            [[new("Ada")], [new("Grace")]],
            Ordered: false,
            NumericTolerance: 0m);
        var permutation = new SqlLabResultSet(
            [new("Name", "nvarchar", false)],
            [[new("Grace")], [new("Ada")]]);
        var wrong = permutation with { Rows = [[new("Grace")], [new("Linus")]] };

        Assert.True(SqlResultValidator.Validate(expectation, permutation).Passed);
        Assert.False(SqlResultValidator.Validate(expectation, wrong).Passed);
    }

    [Fact]
    public void ValidationDistinguishesNullFromEmptyAndReportsMissingResult()
    {
        var expectation = new SqlLabExpectedResult(
            ["Value"],
            [[new(null, IsNull: true)]],
            Ordered: true,
            NumericTolerance: 0m);
        var empty = new SqlLabResultSet(
            [new("Value", "nvarchar", true)],
            [[new(string.Empty)]]);

        Assert.False(SqlResultValidator.Validate(expectation, empty).Passed);
        Assert.False(SqlResultValidator.Validate(expectation, null).Passed);
    }
}
