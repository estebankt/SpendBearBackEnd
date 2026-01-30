namespace Analytics.Application.Features.Queries.GetMonthlySummary;

public record GetMonthlySummaryQuery(Guid UserId, int Month, int Year);
