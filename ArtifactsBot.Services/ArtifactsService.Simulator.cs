using ArtifactsBot.Services.Models;

namespace ArtifactsBot.Services;

public partial class ArtifactsService
{
    public static (List<FightSimulatorResult>, bool IsDeterministic) SimulateFight(CharacterStats character, MonsterSchema monster, int iterations)
    {
        static (int RealDamage, int Resist) GetRealDamagePerHit(int attack, int defenderResist)
        {
            return (Round(attack - attack * defenderResist * 0.01), defenderResist);
        }

        List<(int RealDamage, int Resist)> characterAttacks = new(4);
        if (character.FireAttack > 0) { characterAttacks.Add(GetRealDamagePerHit(character.FireAttack, monster.Res_fire)); }
        if (character.EarthAttack > 0) { characterAttacks.Add(GetRealDamagePerHit(character.EarthAttack, monster.Res_earth)); }
        if (character.WaterAttack > 0) { characterAttacks.Add(GetRealDamagePerHit(character.WaterAttack, monster.Res_water)); }
        if (character.AirAttack > 0) { characterAttacks.Add(GetRealDamagePerHit(character.AirAttack, monster.Res_air)); }

        List<(int RealDamage, int Resist)> monsterAttacks = new(4);
        if (monster.Attack_fire > 0) { monsterAttacks.Add(GetRealDamagePerHit(monster.Attack_fire, character.FireResist)); }
        if (monster.Attack_earth > 0) { monsterAttacks.Add(GetRealDamagePerHit(monster.Attack_earth, character.EarthResist)); }
        if (monster.Attack_water > 0) { monsterAttacks.Add(GetRealDamagePerHit(monster.Attack_water, character.WaterResist)); }
        if (monster.Attack_air > 0) { monsterAttacks.Add(GetRealDamagePerHit(monster.Attack_air, character.AirResist)); }

        bool isDeterministic = characterAttacks.All(a => a.Resist == 0) && monsterAttacks.All(a => a.Resist == 0);
        if (isDeterministic) { iterations = 1; }

        List<FightSimulatorResult> results = new(iterations);
        for (int i = 0; i < iterations; i++)
        {
            int turn = 1;
            int characterHp = character.MaxHp;
            int monsterHp = monster.Hp;
            int lostOnTurn = 0;
            bool? isWin = null;
            while (turn < 100)
            {
                if (turn % 2 == 0)
                {
                    // monster turn
                    foreach ((int realDamage, int resist) in monsterAttacks)
                    {
                        if (IsAttackUnblocked(resist))
                        {
                            characterHp -= realDamage;
                        }
                    }

                    if (isWin == null && characterHp <= 0)
                    {
                        isWin = false;
                        lostOnTurn = turn;
                    }
                }
                else
                {
                    // character turn
                    foreach ((int realDamage, int resist) in characterAttacks)
                    {
                        if (IsAttackUnblocked(resist))
                        {
                            monsterHp -= realDamage;
                        }
                    }

                    if (monsterHp <= 0)
                    {
                        isWin ??= true;
                        break;
                    }
                }
                ++turn;
            }

            results.Add(new FightSimulatorResult(isWin ?? false, isWin == true ? turn : lostOnTurn, turn, characterHp));
        }

        return (results, isDeterministic);
    }

    private static bool IsAttackUnblocked(int resist) => resist <= 0 || resist <= Random.Shared.Next(0, 1000);

    private static int GetMultipliedAttack(int attack, int damageMultiplier) => Round(attack + attack * damageMultiplier * 0.01);

    /// <summary>
    /// Round to the nearest int. Values ending in .5 always round up. This is the rounding logic used by the game.
    /// </summary>
    public static int Round(double value) => (int)Math.Floor(value + 0.5);

    public static CharacterStats GetCharacterStats(IEnumerable<ItemSchema> characterEquipment, int characterLevel)
    {
        int maxHp = Constants.CharacterBaseHp + characterLevel * Constants.CharacterHpPerLevel,
            fireAttack = 0, earthAttack = 0, waterAttack = 0, airAttack = 0,
            fireDamage = 0, earthDamage = 0, waterDamage = 0, airDamage = 0,
            fireResist = 0, earthResist = 0, waterResist = 0, airResist = 0;

        foreach (var item in characterEquipment)
        {
            foreach (var effect in item.Effects)
            {
                switch (effect.Code)
                {
                    case Constants.Hp:
                    case Constants.HpBoost:
                        maxHp += effect.Value;
                        break;
                    case Constants.FireAttack:
                        fireAttack += effect.Value;
                        break;
                    case Constants.EarthAttack:
                        earthAttack += effect.Value;
                        break;
                    case Constants.WaterAttack:
                        waterAttack += effect.Value;
                        break;
                    case Constants.AirAttack:
                        airAttack += effect.Value;
                        break;
                    case Constants.FireDamage:
                    case Constants.FireDamageBoost:
                        fireDamage += effect.Value;
                        break;
                    case Constants.EarthDamage:
                    case Constants.EarthDamageBoost:
                        earthDamage += effect.Value;
                        break;
                    case Constants.WaterDamage:
                    case Constants.WaterDamageBoost:
                        waterDamage += effect.Value;
                        break;
                    case Constants.AirDamage:
                    case Constants.AirDamageBoost:
                        airDamage += effect.Value;
                        break;
                    case Constants.FireResist:
                    case Constants.FireResistBoost:
                        fireResist += effect.Value;
                        break;
                    case Constants.EarthResist:
                    case Constants.EarthResistBoost:
                        earthResist += effect.Value;
                        break;
                    case Constants.WaterResist:
                    case Constants.WaterResistBoost:
                        waterResist += effect.Value;
                        break;
                    case Constants.AirResist:
                    case Constants.AirResistBoost:
                        airResist += effect.Value;
                        break;
                }
            }
        }

        return new CharacterStats(maxHp,
            GetMultipliedAttack(fireAttack, fireDamage),
            GetMultipliedAttack(earthAttack, earthDamage),
            GetMultipliedAttack(waterAttack, waterDamage),
            GetMultipliedAttack(airAttack, airDamage),
            fireResist, earthResist, waterResist, airResist);
    }

    public static List<string> GetCharacterEquipmentItemCodes(CharacterSchema character)
    {
        List<string> equipment = new(14);
        void AddIfExists(string slotItem)
        {
            if (!string.IsNullOrEmpty(slotItem)) { equipment.Add(slotItem); }
        }

        AddIfExists(character.Weapon_slot);
        AddIfExists(character.Shield_slot);
        AddIfExists(character.Helmet_slot);
        AddIfExists(character.Body_armor_slot);
        AddIfExists(character.Leg_armor_slot);
        AddIfExists(character.Boots_slot);
        AddIfExists(character.Ring1_slot);
        AddIfExists(character.Ring2_slot);
        AddIfExists(character.Amulet_slot);
        AddIfExists(character.Artifact1_slot);
        AddIfExists(character.Artifact2_slot);
        AddIfExists(character.Artifact3_slot);
        AddIfExists(character.Utility1_slot);
        AddIfExists(character.Utility2_slot);

        return equipment;
    }
}
