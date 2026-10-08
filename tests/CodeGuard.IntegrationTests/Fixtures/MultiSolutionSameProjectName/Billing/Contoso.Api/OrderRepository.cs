namespace Contoso.Api;

public class AuditBase
{
    public virtual void Add(string id) { }
}

// Same project name and type FullNames as Orders/Contoso.Api, but a different base type and a
// mutable class (not a record) - neither analyzer may attribute these to the Orders project's types.
public class OrderRepository : AuditBase
{
    public override void Add(string id) => base.Add(id);
}

public class Money
{
    public decimal Amount { get; set; }

    public void Recalculate() => Amount = 0;
}
