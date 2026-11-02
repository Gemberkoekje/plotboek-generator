namespace PlotboekGenerator.core.Models;

public partial class PlotboekPagina
{
    public Guid DomainId { get; private set; }

    public string DomainShorthand { get; private set; }

    public string DomainDescription { get; private set; }

    private Result<PlotboekPagina> AanpassenDomain(Guid domainId, string domainShorthand, string domainDescription, string who)
    {
        if (DomainId == domainId)
        {
            return Result.For(this);
        }

        return ApplyEvent(new PlotboekPaginaDomainAangepast
        {
            DomainId = domainId,
            DomainShorthand = domainShorthand,
            DomainDescription = domainDescription,
            Who = who,
        });
    }

    internal void When(PlotboekPaginaDomainAangepast @event)
    {
        DomainId = @event.DomainId;
        DomainShorthand = @event.DomainShorthand;
        DomainDescription = @event.DomainDescription;
    }
}
