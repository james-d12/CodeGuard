namespace Contoso.Api;

public abstract class RepositoryBase
{
    public virtual void Add(string id) { }
}

public class OrderRepository : RepositoryBase
{
    public override void Add(string id) => base.Add(id);
}

public record Money(decimal Amount);
