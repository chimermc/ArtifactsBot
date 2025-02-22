namespace ArtifactsBot.Services;

public class Constants
{
    public const string BaseUrl = "https://api.artifactsmmo.com";
    public const int MaxRetries = 5;
    public const int ServerUpdateCheckIntervalMilliseconds = 60000; // 1 minute

    public const int FightSimulatorIterations = 1000;
    public const int CharacterBaseHp = 115;
    public const int CharacterHpPerLevel = 5;

    /// <summary>
    /// Embed field values cannot be truly empty, but can contain something invisible instead.
    /// </summary>
    public const string EmbedEmptyItem = "** **";

    #region Commands

    public const string CommandSimulate = "simulate";
    public const string CommandSimulateCharacter = "simulate-character";
    public const string CommandCharacter = "character";
    public const string CommandCharacterEquipment = "character-equipment";
    public const string CommandMonster = "monster";
    public const string CommandItem = "item";

    public const string CommandNameParameter = "name";
    public const string CommandNameParameterDescription = "The name of the character (case sensitive).";
    public const string CommandMonsterParameter = "monster";
    public const string CommandMonsterParameterDescription = "The code or name of the monster.";
    public const string CommandItemsParameter = "items";
    public const string CommandItemParameter = "item";
    public const string CommandLevelParameter = "level";

    #endregion Commands

    #region EffectCodes

    // ReSharper disable IdentifierTypo
    // ReSharper disable StringLiteralTypo

    public const string Alchemy = "alchemy";
    public const string Antipoison = "antipoison";
    public const string AirAttack = "attack_air";
    public const string EarthAttack = "attack_earth";
    public const string FireAttack = "attack_fire";
    public const string WaterAttack = "attack_water";
    public const string AirDamageBoost = "boost_dmg_air";
    public const string EarthDamageBoost = "boost_dmg_earth";
    public const string FireDamageBoost = "boost_dmg_fire";
    public const string WaterDamageBoost = "boost_dmg_water";
    public const string HpBoost = "boost_hp";
    public const string AirResistBoost = "boost_res_air";
    public const string EarthResistBoost = "boost_res_earth";
    public const string FireResistBoost = "boost_res_fire";
    public const string WaterResistBoost = "boost_res_water";
    public const string Burn = "burn";
    public const string CriticalStrike = "critical_strike";
    public const string Damage = "dmg";
    public const string AirDamage = "dmg_air";
    public const string EarthDamage = "dmg_earth";
    public const string FireDamage = "dmg_fire";
    public const string WaterDamage = "dmg_water";
    public const string Fishing = "fishing";
    public const string Gold = "gold";
    public const string Haste = "haste";
    public const string Heal = "heal"; // Consumable heal
    public const string Healing = "healing"; // Healing rune
    public const string Hp = "hp";
    public const string InventorySpace = "inventory_space";
    public const string Lifesteal = "lifesteal";
    public const string Mining = "mining";
    public const string Poison = "poison";
    public const string Prospecting = "prospecting";
    public const string AirResist = "res_air";
    public const string EarthResist = "res_earth";
    public const string FireResist = "res_fire";
    public const string WaterResist = "res_water";
    public const string Restore = "restore";
    public const string TeleportX = "teleport_x";
    public const string TeleportY = "teleport_Y";
    public const string Wisdom = "wisdom";
    public const string Woodcutting = "woodcutting";

    public static string GetEffectCodeDisplayName(string effectCode) => effectCode switch
    {
        Alchemy => "% Alchemy CD",
        Antipoison => "Antipoison",
        AirAttack => "Air Attack",
        EarthAttack => "Earth Attack",
        FireAttack => "Fire Attack",
        WaterAttack => "Water Attack",
        AirDamageBoost => "% Air Damage Boost",
        EarthDamageBoost => "% Earth Damage Boost",
        FireDamageBoost => "% Fire Damage Boost",
        WaterDamageBoost => "% Water Damage Boost",
        HpBoost => "HP Boost",
        AirResistBoost => "% Air Res Boost",
        EarthResistBoost => "% Earth Res Boost",
        FireResistBoost => "% Fire Res Boost",
        WaterResistBoost => "% Water Res Boost",
        Burn => "% Burn",
        CriticalStrike => "% Critical Strike",
        Damage => "% Damage",
        AirDamage => "% Air Damage",
        EarthDamage => "% Earth Damage",
        FireDamage => "% Fire Damage",
        WaterDamage => "% Water Damage",
        Fishing => "% Fishing CD",
        Gold => "Gold",
        Haste => "Haste",
        Heal => "HP Heal",
        Healing => "% Healing",
        Hp => "Max HP",
        InventorySpace => "Inventory Space",
        Lifesteal => "% Lifesteal",
        Mining => "% Mining CD",
        Poison => "Poison",
        Prospecting => "Prospecting",
        AirResist => "% Res Air",
        EarthResist => "% Res Earth",
        FireResist => "% Res Fire",
        WaterResist => "% Res Water",
        Restore => "HP Restore",
        TeleportX => "(X) Teleport",
        TeleportY => "(Y) Teleport",
        Wisdom => "Wisdom",
        Woodcutting => "% Woodcutting CD",
        (_) => effectCode
    };

    // ReSharper restore StringLiteralTypo
    // ReSharper restore IdentifierTypo

    #endregion EffectCodes

    public static string GetCharacterImageUrl(CharacterSkin skin) => $"https://artifactsmmo.com/images/characters/{skin.ToString().ToLowerInvariant()}.png";

    public static string GetMonsterImageUrl(string code) => $"https://artifactsmmo.com/images/monsters/{code}.png";

    public static string GetItemImageUrl(string code) => $"https://artifactsmmo.com/images/items/{code}.png";
}
