namespace Tests.Entities;

public interface IEntity
{
    public int Id { get; set; }
}

public class Person : IEntity
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public int Age { get; set; }

    public ICollection<Dog> Dogs { get; set; } = [];
    public ICollection<Cat> Cats { get; set; } = [];
}

public abstract class Pet : IEntity
{
    public int Id { get; set; }
    public int Age { get; set; }

    public string? Name { get; set; }

    public int OwnerId { get; set; }
    public Person Owner { get; set; } = null!;
}

public class Dog : Pet
{
    public bool LovesChasingSticks { get; set; }
}

public class Cat : Pet
{
    public bool LovesSleeping { get; set; }
}