namespace Contoso.Domain.Entities;

public class Order : Entity<int>, IAggregateRoot
{
    private Order() { }

    public static Order Create(string name) => new();
}
