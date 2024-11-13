using ArtifactsBot.Services.Models;

namespace ArtifactsBot.Services;

public partial class ArtifactsService
{
    public static (List<FightSimulatorResult>, bool IsDeterministic) SimulateFight(CharacterStats character, MonsterSchema monster, int iterations)
    {
        static (int RealDamage, int Resist) GetRealDamagePerHit(int attack, int defenderResist)
        {
            return ((int)Math.Round(attack - attack * defenderResist * 0.01f), defenderResist);
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
                    foreach (var attack in monsterAttacks)
                    {
                        if (IsAttackUnblocked(attack.Resist))
                        {
                            characterHp -= attack.RealDamage;
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
                    foreach (var attack in characterAttacks)
                    {
                        if (IsAttackUnblocked(attack.Resist))
                        {
                            monsterHp -= attack.RealDamage;
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

    private static bool IsAttackUnblocked(int resist) => resist == 0 || resist <= Random.Shared.Next(0, 1000);

    private static int GetMultipliedAttack(int attack, int damageMultiplier) => (int)Math.Round(attack + attack * damageMultiplier * 0.01f);

    public static CharacterStats GetCharacterStats(IEnumerable<ItemSchema> characterEquipment, int characterLevel)
    {
        int maxHp = Constants.CharacterBaseHp + (characterLevel - 1) * Constants.CharacterHpPerLevel,
            fireAttack = 0, earthAttack = 0, waterAttack = 0, airAttack = 0,
            fireDamage = 0, earthDamage = 0, waterDamage = 0, airDamage = 0,
            fireResist = 0, earthResist = 0, waterResist = 0, airResist = 0;

        foreach (var item in characterEquipment)
        {
            foreach (var effect in item.Effects)
            {
                switch (effect.Name)
                {
                    case Constants.Hp:
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
                        fireDamage += effect.Value;
                        break;
                    case Constants.EarthDamage:
                        earthDamage += effect.Value;
                        break;
                    case Constants.WaterDamage:
                        waterDamage += effect.Value;
                        break;
                    case Constants.AirDamage:
                        airDamage += effect.Value;
                        break;
                    case Constants.FireResist:
                        fireResist += effect.Value;
                        break;
                    case Constants.EarthResist:
                        earthResist += effect.Value;
                        break;
                    case Constants.WaterResist:
                        waterResist += effect.Value;
                        break;
                    case Constants.AirResist:
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
        List<string> equipment = new(12);
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

        return equipment;
    }
}
