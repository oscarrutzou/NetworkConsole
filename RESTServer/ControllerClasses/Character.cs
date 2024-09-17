namespace RESTServer.ControllerClasses;

public enum CharacterType
{
    Warrior,
}

public struct Point
{
    public int X { get; set; } 
    public int Y { get; set; }
}

public class Character
{
    public int OwnerID { get; set; }
    public string Name { get; set; }
    public CharacterType CharacterType { get; set; }
    public int Damage { get; set; }
    public int Health { get; set; }
    public Point Point { get; set; }
}
