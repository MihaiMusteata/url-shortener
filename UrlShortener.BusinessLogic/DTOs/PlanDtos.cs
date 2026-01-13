namespace UrlShortener.BusinessLogic.DTOs;

public class PlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal PriceMonthly { get; set; }
    public int MaxLinksPerMonth { get; set; }
    public bool CustomAliasEnabled { get; set; }
    public bool QrEnabled { get; set; }
}

public class CurrentPlanDto
{
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
    public decimal? PriceMonthly { get; set; }
    public int? MaxLinksPerMonth { get; set; }
}

public class PlanSummaryDto
{
    public string Name { get; set; } = "";
    public decimal PriceMonthly { get; set; }
    public int MaxLinksPerMonth { get; set; }
    public bool CustomAliasEnabled { get; set; }
    public bool QrEnabled { get; set; }
}