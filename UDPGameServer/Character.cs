using MessagePack;

namespace UDPGameServer;

public enum CharacterType
{
    Warrior,
}

[MessagePackObject]
public class Point
{
    [Key(0)]
    public int X { get; set; }
    [Key(1)]
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

[MessagePackObject]
public class Character
{
    [Key(0)]
    public int OwnerID { get; set; }
    [Key(1)]
    public Point Point { get; set; }
    [Key(2)]
    public string Name { get; set; }
    [Key(3)]
    public CharacterType CharacterType { get; set; }
    [Key(4)]
    public int Damage { get; set; }
    [Key(5)]
    public int MaxHealth { get; set; }
    [Key(6)]
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
    public string[] GetGridData()
    {
        return
        [
            $"|Owner: {OwnerID}".PadRight(15,' '),
            $"|{Name}".PadRight(15,' '),
            $"|HP: {CurrentHealth}/{MaxHealth}".PadRight(15,' '),
            $"|Attack: {Damage}".PadRight(15,' '),
            "|              ",
            "|______________"
        ];
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="damage"></param>
    /// <returns>False = dead, True = alive</returns>
    public bool TakeDamage(int damage)
    {
        if (CurrentHealth - damage > 0)
        {
            CurrentHealth -= damage;
            return true;
        }
        else
        {
            CurrentHealth = 0;
            return false;
        }
    }

    public bool DealDamage(Character otherCharacter)
    {
        return otherCharacter.TakeDamage(Damage);
    }
}
