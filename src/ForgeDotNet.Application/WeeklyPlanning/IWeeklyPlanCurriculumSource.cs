using ForgeDotNet.Domain.WeeklyPlanning;

namespace ForgeDotNet.Application.WeeklyPlanning;

public interface IWeeklyPlanCurriculumSource
{
    ValueTask<WeeklyPlanCurriculumSnapshot> GetAsync(CancellationToken cancellationToken = default);
}
