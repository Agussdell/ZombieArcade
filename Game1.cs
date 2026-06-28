using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        var game = new ZombieArcade();
        game.Run();
    }
}

struct Position
{
    public int X;
    public int Y;

    public Position(int x, int y)
    {
        X = x;
        Y = y;
    }
}

abstract class Entity
{
    public Position Position;
    public int Health;
    public char Symbol;
    public string Name;

    public bool IsAlive => Health > 0;

    protected Entity(string name, char symbol, int health, Position position)
    {
        Name = name;
        Symbol = symbol;
        Health = health;
        Position = position;
    }
}

class Weapon
{
    public string Name;
    public int Damage;
    public int Range;
    public int Ammo;
    public bool UnlimitedAmmo => Ammo < 0;

    public Weapon(string name, int damage, int range, int ammo)
    {
        Name = name;
        Damage = damage;
        Range = range;
        Ammo = ammo;
    }
}

class Player : Entity
{
    public Weapon CurrentWeapon;
    public int Armor;
    public int Credits;
    public int Kills;

    public Player() : base("Superviviente", 'P', 30, new Position(5, 5))
    {
        Armor = 0;
        Credits = 50;
        Kills = 0;
        CurrentWeapon = new Weapon("Pistola", 5, 3, -1);
    }

    public void EquipWeapon(Weapon weapon)
    {
        CurrentWeapon = weapon;
    }

    public int TakeDamage(int amount)
    {
        var reduction = Math.Min(Armor, amount / 2);
        var damage = amount - reduction;
        Health -= damage;
        return damage;
    }
}

class Zombie : Entity
{
    public Zombie(int health, Position position) : base("Zombie", 'Z', health, position)
    {
    }
}

class BaseCamp
{
    public int Materials;
    public int WeaponParts;
    public int ArmorLevel;

    public BaseCamp()
    {
        Materials = 20;
        WeaponParts = 10;
        ArmorLevel = 0;
    }

    public void ImproveArmor(Player player)
    {
        if (Materials >= 15)
        {
            Materials -= 15;
            ArmorLevel++;
            player.Armor += 2;
        }
    }

    public Weapon CraftWeapon()
    {
        if (WeaponParts >= 10)
        {
            WeaponParts -= 10;
            return new Weapon("Escopeta", 12, 2, 10);
        }
        return null;
    }
}

class ZombieArcade
{
    const int MapSize = 10;
    readonly Random random = new Random();
    readonly List<Zombie> zombies = new List<Zombie>();
    readonly List<Weapon> weaponRack = new List<Weapon>();
    Player player;
    BaseCamp baseCamp;
    int roundNumber = 1;

    public void Run()
    {
        Initialize();

        while (player.IsAlive)
        {
            if (zombies.Count == 0)
            {
                StartRound();
            }

            Render();
            PlayerAction();

            if (player.IsAlive)
            {
                ZombieTurn();
            }
        }

        Console.Clear();
        Console.WriteLine("Game Over. Los zombies te atraparon.");
        Console.WriteLine($"Rondas completadas: {roundNumber - 1}");
    }

    void Initialize()
    {
        player = new Player();
        baseCamp = new BaseCamp();
        weaponRack.Add(new Weapon("Ametralladora ligera", 8, 4, 20));
        weaponRack.Add(new Weapon("Lanzagranadas", 20, 3, 5));
    }

    void StartRound()
    {
        zombies.Clear();
        var targetCount = roundNumber + 2;
        for (var i = 0; i < targetCount; i++)
        {
            var pos = RandomEdgePosition();
            zombies.Add(new Zombie(10 + roundNumber * 2, pos));
        }

        Console.Clear();
        Console.WriteLine($"Ronda {roundNumber} iniciada. Sobrevive a la oleada de zombies.");
        Console.WriteLine("Presiona cualquier tecla para comenzar...");
        Console.ReadKey(true);
    }

    Position RandomEdgePosition()
    {
        int x, y;
        if (random.Next(2) == 0)
        {
            x = random.Next(MapSize);
            y = random.Next(2) == 0 ? 0 : MapSize - 1;
        }
        else
        {
            x = random.Next(2) == 0 ? 0 : MapSize - 1;
            y = random.Next(MapSize);
        }
        return new Position(x, y);
    }

    void Render()
    {
        Console.Clear();
        var grid = CreateMapGrid();
        for (var y = 0; y < MapSize; y++)
        {
            for (var x = 0; x < MapSize; x++)
            {
                Console.Write(grid[x, y]);
            }
            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine($"Ronda: {roundNumber}    Vida: {player.Health}    Armadura:{player.Armor}    Creditos:{player.Credits}");
        Console.WriteLine($"Arma: {player.CurrentWeapon.Name}  Daño: {player.CurrentWeapon.Damage}  Alcance: {player.CurrentWeapon.Range}  Munición: {(player.CurrentWeapon.UnlimitedAmmo ? "∞" : player.CurrentWeapon.Ammo.ToString())}");
        Console.WriteLine($"Zombies restantes: {zombies.Count}    Materiales: {baseCamp.Materials}    Partes: {baseCamp.WeaponParts}");
        Console.WriteLine("Comandos: W/A/S/D = mover, F = disparar, B = base, Q = rendirse");
    }

    char[,] CreateMapGrid()
    {
        var map = new char[MapSize, MapSize];
        for (var y = 0; y < MapSize; y++)
        {
            for (var x = 0; x < MapSize; x++)
            {
                map[x, y] = '.';
            }
        }

        foreach (var zombie in zombies)
        {
            if (zombie.IsAlive)
            {
                map[zombie.Position.X, zombie.Position.Y] = zombie.Symbol;
            }
        }

        map[player.Position.X, player.Position.Y] = player.Symbol;
        return map;
    }

    void PlayerAction()
    {
        var key = Console.ReadKey(true).Key;
        switch (key)
        {
            case ConsoleKey.W: MovePlayer(0, -1); break;
            case ConsoleKey.S: MovePlayer(0, 1); break;
            case ConsoleKey.A: MovePlayer(-1, 0); break;
            case ConsoleKey.D: MovePlayer(1, 0); break;
            case ConsoleKey.F: Shoot(); break;
            case ConsoleKey.B: EnterBase(); break;
            case ConsoleKey.Q:
                player.Health = 0;
                break;
        }
    }

    void MovePlayer(int dx, int dy)
    {
        var next = new Position(
            Math.Clamp(player.Position.X + dx, 0, MapSize - 1),
            Math.Clamp(player.Position.Y + dy, 0, MapSize - 1));

        player.Position = next;
        CheckZombieCollision();
    }

    void CheckZombieCollision()
    {
        var zombie = zombies.Find(z => z.IsAlive && z.Position.X == player.Position.X && z.Position.Y == player.Position.Y);
        if (zombie != null)
        {
            var damage = zombie.IsAlive ? zombie.Health / 2 : 0;
            player.TakeDamage(damage);
            Console.WriteLine($"Te han alcanzado un zombie y recibes {damage} de daño.");
            Console.ReadKey(true);
        }
    }

    void Shoot()
    {
        if (!player.CurrentWeapon.UnlimitedAmmo && player.CurrentWeapon.Ammo == 0)
        {
            Console.WriteLine("No tienes munición. Visita la base para recargar o cambiar de arma.");
            Console.ReadKey(true);
            return;
        }

        var target = FindNearestZombie();
        if (target == null)
        {
            Console.WriteLine("No hay zombies en rango.");
            Console.ReadKey(true);
            return;
        }

        if (!player.CurrentWeapon.UnlimitedAmmo)
        {
            player.CurrentWeapon.Ammo--;
        }

        target.Health -= player.CurrentWeapon.Damage;
        Console.WriteLine($"Disparas con {player.CurrentWeapon.Name} y haces {player.CurrentWeapon.Damage} de daño al zombie.");

        if (!target.IsAlive)
        {
            player.Kills++;
            player.Credits += 10;
            baseCamp.Materials += 5;
            baseCamp.WeaponParts += 2;
            Console.WriteLine("Zombie eliminado! Recibes recursos.");
        }

        Console.ReadKey(true);
    }

    Zombie FindNearestZombie()
    {
        Zombie best = null;
        var bestDistance = int.MaxValue;

        foreach (var zombie in zombies)
        {
            if (!zombie.IsAlive) continue;
            var distance = Math.Abs(zombie.Position.X - player.Position.X) + Math.Abs(zombie.Position.Y - player.Position.Y);
            if (distance <= player.CurrentWeapon.Range && distance < bestDistance)
            {
                bestDistance = distance;
                best = zombie;
            }
        }

        return best;
    }

    void EnterBase()
    {
        Console.Clear();
        Console.WriteLine("Base de operaciones");
        Console.WriteLine($"Materiales disponibles: {baseCamp.Materials}");
        Console.WriteLine($"Partes de arma: {baseCamp.WeaponParts}");
        Console.WriteLine("1. Mejorar armadura (15 materiales)");
        Console.WriteLine("2. Fabricar nueva arma (10 partes)");
        Console.WriteLine("3. Recargar munición del arma actual (5 créditos)");
        Console.WriteLine("4. Volver al combate");

        var key = Console.ReadKey(true).Key;
        switch (key)
        {
            case ConsoleKey.D1:
                baseCamp.ImproveArmor(player);
                break;
            case ConsoleKey.D2:
                var weapon = baseCamp.CraftWeapon();
                if (weapon != null)
                {
                    weaponRack.Add(weapon);
                    Console.WriteLine($"Has fabricado {weapon.Name}.");
                }
                else
                {
                    Console.WriteLine("No tienes partes suficientes para fabricar un arma.");
                }
                Console.ReadKey(true);
                break;
            case ConsoleKey.D3:
                if (player.Credits >= 5)
                {
                    player.Credits -= 5;
                    if (!player.CurrentWeapon.UnlimitedAmmo)
                    {
                        player.CurrentWeapon.Ammo += 10;
                    }
                    Console.WriteLine("Munición recargada.");
                }
                else
                {
                    Console.WriteLine("Créditos insuficientes.");
                }
                Console.ReadKey(true);
                break;
            case ConsoleKey.D4:
                break;
        }

        if (weaponRack.Count > 0)
        {
            Console.WriteLine("Armas disponibles:");
            for (var i = 0; i < weaponRack.Count; i++)
            {
                var w = weaponRack[i];
                Console.WriteLine($"{i + 1}. {w.Name} (Daño {w.Damage} Alcance {w.Range} Munición {(w.UnlimitedAmmo ? "∞" : w.Ammo.ToString())})");
            }
            Console.WriteLine("Selecciona el número para equipar o cualquier otra tecla para volver.");
            var weaponKey = Console.ReadKey(true).Key;
            var index = weaponKey - ConsoleKey.D1;
            if (index >= 0 && index < weaponRack.Count)
            {
                player.EquipWeapon(weaponRack[index]);
            }
        }
    }

    void ZombieTurn()
    {
        foreach (var zombie in zombies)
        {
            if (!zombie.IsAlive) continue;

            var dx = Math.Sign(player.Position.X - zombie.Position.X);
            var dy = Math.Sign(player.Position.Y - zombie.Position.Y);
            zombie.Position = new Position(
                Math.Clamp(zombie.Position.X + dx, 0, MapSize - 1),
                Math.Clamp(zombie.Position.Y + dy, 0, MapSize - 1));

            if (zombie.Position.X == player.Position.X && zombie.Position.Y == player.Position.Y)
            {
                var damage = 4 + roundNumber / 2;
                player.TakeDamage(damage);
            }
        }

        if (zombies.TrueForAll(z => !z.IsAlive))
        {
            roundNumber++;
            player.Credits += 20;
            baseCamp.Materials += 10;
            Console.WriteLine("Has completado la ronda! Recompensas de base recibidas.");
            Console.ReadKey(true);
        }
    }
}
