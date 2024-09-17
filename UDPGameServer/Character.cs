namespace UDPGameServer;

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
    public Point Point { get; set; }
    public string Name { get; set; }
    public CharacterType CharacterType { get; set; }
    public int Damage { get; set; }
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }

    public Character(int ownerID, Point point, string name, CharacterType type, int startDmg, int maxHealth)
    {
        OwnerID = ownerID; 
        Point = point; 
        Name = name; 
        CharacterType = type; 
        Damage = startDmg; 
        MaxHealth = maxHealth; 
        CurrentHealth = maxHealth;
    }
    public string[] getGridStrings()
    {
        return new string[]
        {
                ($"|Owner: {OwnerID}".PadRight(11,' '),
                $"|:{CharacterType}: {Name}").PadRight(11,' '),
                $"|HP: {CurrentHealth}/{MaxHealth}".PadRight(11,' '),
                $"|Attack: {Damage}".PadRight(11,' '),
                "|__________"
        };
    }
}
