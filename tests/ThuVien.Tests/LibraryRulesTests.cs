using ThuVien.Models;
using ThuVien.Services;
using ThuVien.ViewModels;
using Xunit;

namespace ThuVien.Tests;

public class LoanStatusTests
{
    [Fact]
    public void DueDateIsNotOverdueOnTheSameCalendarDay()
    {
        var today = new DateTime(2026, 10, 23, 18, 30, 0);
        var due = new DateTime(2026, 10, 23, 0, 0, 0);
        Assert.False(LoanStatus.IsOverdue(due, today));
        Assert.Equal(LoanStatus.Borrowing, LoanStatus.Of(null, due, today));
    }

    [Fact]
    public void DueDateBecomesOverdueAtStartOfTheNextCalendarDay()
    {
        var due = new DateTime(2026, 10, 23, 23, 59, 59);
        var nextDay = new DateTime(2026, 10, 24, 0, 0, 0);
        Assert.True(LoanStatus.IsOverdue(due, nextDay));
        Assert.Equal(LoanStatus.Overdue, LoanStatus.Of(null, due, nextDay));
    }

    [Fact]
    public void ReturnedBookIsNotOverdueEvenWhenReturnedLate()
    {
        var due = new DateTime(2026, 10, 20);
        var returnedAt = new DateTime(2026, 10, 24);
        Assert.Equal(LoanStatus.Returned, LoanStatus.Of(returnedAt, due, returnedAt));
    }

    [Theory]
    [InlineData(LoanStatus.Returned, "text-bg-success")]
    [InlineData(LoanStatus.Overdue, "text-bg-danger")]
    [InlineData(LoanStatus.Borrowing, "text-bg-primary")]
    public void StatusHasExpectedBadge(string status, string expectedCss)
    {
        Assert.Equal(expectedCss, LoanStatus.BadgeClass(status));
    }
}

public class DisplayRulesTests
{
    [Theory]
    [InlineData(AppRoles.Admin, "Quản trị viên")]
    [InlineData(AppRoles.Librarian, "Thủ thư")]
    [InlineData(AppRoles.Reader, "Độc giả")]
    public void RoleNamesAreLocalized(string role, string expected)
    {
        Assert.Equal(expected, RoleDisplay.Name(role));
    }

    [Fact]
    public void UnknownRoleFallsBackToItsValue()
    {
        Assert.Equal("Unknown", RoleDisplay.Name("Unknown"));
    }

    [Theory]
    [InlineData(ReservationStatuses.Waiting)]
    [InlineData(ReservationStatuses.Ready)]
    [InlineData(ReservationStatuses.Fulfilled)]
    [InlineData(ReservationStatuses.Cancelled)]
    [InlineData(ReservationStatuses.Expired)]
    public void ReservationStatusHasUserFriendlyDisplay(string status)
    {
        Assert.False(string.IsNullOrWhiteSpace(ReservationStatuses.DisplayName(status)));
    }
}


public class FeeCalculationTests
{
    [Theory]
    [InlineData("2026-10-23", "2026-10-23", 1000, 0)]
    [InlineData("2026-10-23", "2026-10-24", 1000, 1000)]
    [InlineData("2026-10-23", "2026-10-26", 1500, 4500)]
    [InlineData("2026-10-26", "2026-10-23", 1000, 0)]
    public void LateFineUsesCalendarDays(string due, string returned, decimal perDay, decimal expected)
    {
        Assert.Equal(expected, LibraryRules.CalculateLateFine(DateTime.Parse(due), DateTime.Parse(returned), perDay));
    }

    [Theory]
    [InlineData(1, 2000, 2000)]
    [InlineData(3, 2000, 6000)]
    [InlineData(0, 2000, 0)]
    public void RenewalFeeMultipliesOutstandingCopies(int copies, decimal perBook, decimal expected)
    {
        Assert.Equal(expected, LibraryRules.CalculateRenewalFee(copies, perBook));
    }
}
