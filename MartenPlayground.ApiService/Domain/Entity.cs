namespace MartenPlayground.ApiService.Domain;

public class OrganisationEntity : Aggregate
{
    public HashSet<IExtract> Extracts { get; } = new(new ExtractEqualityComparer());

    // Required for Marten deserialization
    protected OrganisationEntity() { }

    // Cannot use Create as the method name as it conflicts with Marten's Create method
    public static OrganisationEntity Initialise(EntityCreated @event)
    {
        OrganisationEntity entity = new();
        entity.Apply(@event);
        entity.Enqueue(@event);
        return entity;
    }

    public void InitiateRiskExtract(RiskExtractInitiated @event)
    {
        if (Extracts.OfType<RiskExtract>().Any(e => e.Status != "Completed"))
        {
            throw new InvalidOperationException("Cannot initiate a new risk search while another is in progress.");
        }

        Apply(@event);
        Enqueue(@event);
    }

    public void ReceiveRiskExtract(RiskExtractReceived @event)
    {
        var extract = Extracts.OfType<RiskExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);

        if (extract == null)
        {
            throw new InvalidOperationException("Cannot receive a risk extract that has not been initiated.");
        }
        else if (extract.Status != "Initiated")
        {
            return; // Already received
        }

        Apply(@event);
        Enqueue(@event);
    }

    public void InitiateAsicExtract(AsicExtractInitiated @event)
    {
        if (Extracts.OfType<AsicExtract>().Any(e => e.ExtractId == @event.ExtractId))
        {
            return;
        }

        if (Extracts.OfType<AsicExtract>().Any(e => e.Status != "Completed" && e.Acn == @event.Acn))
        {
            throw new InvalidOperationException("Cannot initiate a new ASIC extract while another is in progress.");
        }

        Apply(@event);
        Enqueue(@event);
    }

    public void CreateAsicExtractOrder(AsicExtractOrderCreated @event)
    {
        var extract = Extracts.OfType<AsicExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);
        if (extract == null)
        {
            throw new InvalidOperationException("Cannot lodge an ASIC extract that has not been initiated.");
        }
        else if (extract.Status != "Initiated")
        {
            return; // Already lodged
        }
        Apply(@event);
        Enqueue(@event);
    }

    public void ReceiveAsicExtract(AsicExtractReceived @event)
    {
        var extract = Extracts.OfType<AsicExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);
        if (extract == null)
        {
            throw new InvalidOperationException("Cannot receive an ASIC extract that has not been lodged.");
        }
        else if (extract.Status != "Lodged")
        {
            return; // Already received
        }
        Apply(@event);
        Enqueue(@event);
    }

    public void CompleteAsicExtractOrder(AsicExtractOrderCompleted @event)
    {
        var extract = Extracts.OfType<AsicExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);
        if (extract == null)
        {
            throw new InvalidOperationException("Cannot complete an ASIC extract order that has not been lodged.");
        }
        else if (extract.Status != "Received")
        {
            return; // Already completed
        }
        Apply(@event);
        Enqueue(@event);
    }

    private void Apply(EntityCreated @event)
    {
        Id = @event.EntityId;
    }

    private void Apply(RiskExtractInitiated @event)
    {
        Extracts.RemoveWhere(e => e is RiskExtract); // TODO: Clear previous risk extracts
        Extracts.Add(new RiskExtract(@event.ExtractId));
    }

    private void Apply(RiskExtractReceived @event)
    {
        var extract = Extracts.OfType<RiskExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);
        extract?.MarkAsReceived();
    }

    private void Apply(AsicExtractInitiated @event)
    {
        Extracts.RemoveWhere(e => e is AsicExtract ae && ae.Acn == @event.Acn); // TODO: Clear previous asic extracts
        Extracts.Add(new AsicExtract(@event.ExtractId, @event.Acn));
    }

    private void Apply(AsicExtractOrderCreated @event)
    {
        var extract = Extracts.OfType<AsicExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);
        extract?.MarkAsOrderCreated();
    }

    private void Apply(AsicExtractReceived @event)
    {
        var extract = Extracts.OfType<AsicExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);
        extract?.MarkAsReceived();
    }

    private void Apply(AsicExtractOrderCompleted @event)
    {
        var extract = Extracts.OfType<AsicExtract>().FirstOrDefault(x => x.ExtractId == @event.ExtractId);
        extract?.MarkAsCompleted();
    }

    // Compare ExtractId only
    private class ExtractEqualityComparer : IEqualityComparer<IExtract>
    {
        public bool Equals(IExtract? x, IExtract? y)
        {
            if (ReferenceEquals(x, y))
                return true;

            if (x is null || y is null)
                return false;

            return x.ExtractId == y.ExtractId;
        }

        public int GetHashCode(IExtract obj) => obj.ExtractId.GetHashCode();
    }
}

public interface IExtract
{
    Guid ExtractId { get; }

    string Status { get; }
}

public class RiskExtract(Guid id) : IExtract
{
    public Guid ExtractId { get; } = id;

    public string Status { get; private set; } = "Initiated";

    public void MarkAsReceived()
    {
        Status = "Received";
    }

    public void MarkAsCompleted()
    {
        Status = "Completed";
    }
}

public class AsicExtract(Guid id, string acn) : IExtract
{
    public Guid ExtractId { get; } = id;

    public string Acn { get; } = acn;

    public string Status { get; private set; } = "Initiated";

    public void MarkAsOrderCreated()
    {
        Status = "OrderCreate";
    }

    public void MarkAsReceived()
    {
        Status = "Received";
    }

    public void MarkAsCompleted()
    {
        Status = "Completed";
    }
}

public record EntityCreated(Guid EntityId);

public record RiskExtractInitiated(Guid EntityId, Guid ExtractId);
public record RiskExtractReceived(Guid EntityId, Guid ExtractId);

public record AsicExtractInitiated(Guid EntityId, Guid ExtractId, string Acn);
public record AsicExtractOrderCreated(Guid EntityId, Guid ExtractId, int OrderId);
public record AsicExtractReceived(Guid EntityId, Guid ExtractId);
public record AsicExtractOrderCompleted(Guid EntityId, Guid ExtractId);
