using ArtifactsBot.Services.Extensions;
using ArtifactsBot.Services.Models;
using Discord;
using System.Text;

namespace ArtifactsBot.Services;

public partial class DiscordService
{
    public Embed BuildCharacterEmbed(CharacterSchema character)
    {
        // ReSharper disable StringLiteralTypo
        string skills =
            $"""
                 Combat: {character.Level} ({Percentage(character.Xp, character.Max_xp)}%)
                 Mining: {character.Mining_level} ({Percentage(character.Mining_xp, character.Mining_max_xp)}%)
                 Woodcutting: {character.Woodcutting_level} ({Percentage(character.Woodcutting_xp, character.Woodcutting_max_xp)}%)
                 Fishing: {character.Fishing_level} ({Percentage(character.Fishing_xp, character.Fishing_max_xp)}%)
                 Weaponcrafting: {character.Weaponcrafting_level} ({Percentage(character.Weaponcrafting_xp, character.Weaponcrafting_max_xp)}%)
                 Gearcrafting: {character.Gearcrafting_level} ({Percentage(character.Gearcrafting_xp, character.Gearcrafting_max_xp)}%)
                 Jewelrycrafting: {character.Jewelrycrafting_level} ({Percentage(character.Jewelrycrafting_xp, character.Jewelrycrafting_max_xp)}%)
                 Cooking: {character.Cooking_level} ({Percentage(character.Cooking_xp, character.Cooking_max_xp)}%)
                 Alchemy: {character.Alchemy_level} ({Percentage(character.Alchemy_xp, character.Alchemy_max_xp)}%)
                 """;
        // ReSharper restore StringLiteralTypo

        var equipment = ArtifactsService.GetCharacterEquipmentItemCodes(character).Select(itemCode => $"`{itemCode}`").ToList();
        if (character.Utility1_slot_quantity > 0) { equipment.Add($"`{character.Utility1_slot}` x{character.Utility1_slot_quantity}"); }
        if (character.Utility2_slot_quantity > 0) { equipment.Add($"`{character.Utility2_slot}` x{character.Utility2_slot_quantity}"); }

        List<string> attacks = new(4);
        if (character.Attack_fire > 0) { attacks.Add($"Fire: {ArtifactsService.Round(character.Attack_fire * (1 + character.Dmg_fire / 100.0))}"); }
        if (character.Attack_earth > 0) { attacks.Add($"Earth: {ArtifactsService.Round(character.Attack_earth * (1 + character.Dmg_earth / 100.0))}"); }
        if (character.Attack_water > 0) { attacks.Add($"Water: {ArtifactsService.Round(character.Attack_water * (1 + character.Dmg_water / 100.0))}"); }
        if (character.Attack_air > 0) { attacks.Add($"Air: {ArtifactsService.Round(character.Attack_air * (1 + character.Dmg_air / 100.0))}"); }

        List<string> resistances = new(4);
        if (character.Res_fire != 0) { resistances.Add($"Fire: {character.Res_fire}%"); }
        if (character.Res_earth != 0) { resistances.Add($"Earth: {character.Res_earth}%"); }
        if (character.Res_water != 0) { resistances.Add($"Water: {character.Res_water}%"); }
        if (character.Res_air != 0) { resistances.Add($"Air: {character.Res_air}%"); }

        var builder = new EmbedBuilder()
            .WithTitle(character.Name)
            .WithThumbnailUrl(Constants.GetCharacterImageUrl(character.Skin))
            .AddField("Skills", skills, true)
            .AddField("Equipment", BuildColumn(equipment), true)
            .AddField("Inventory", BuildColumn(character.Inventory.Where(s => s.Quantity > 0).Select(s => $"`{s.Code}` x{s.Quantity}")), true)
            .AddField("Max HP", character.Max_hp, true)
            .AddField("Attack", BuildColumn(attacks), true)
            .AddField("Resist", BuildColumn(resistances), true);

        return builder.Build();
    }

    public Embed BuildItemEmbed(ItemSchema item)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"{item.Name} (`{item.Code}`)")
            .WithThumbnailUrl(Constants.GetItemImageUrl(item.Code))
            .AddField("Type", item.Type.Replace('_', ' ').ToTitleCase().ToInvisibleEmbedIfEmpty(), true)
            .AddField("Subtype", item.Subtype.Replace('_', ' ').ToTitleCase().ToInvisibleEmbedIfEmpty(), true)
            .AddField("Level", item.Level.ToString(), true);

        if (item.Effects.Count > 0)
        {
            builder.AddField("Effects", BuildColumn(item.Effects.Select(e => $"{e.Value} {Constants.GetEffectCodeDisplayName(e.Code)}")), true);
        }

        if (item.Craft != null)
        {
            builder.AddField("Craft Skill", $"{item.Craft.Skill} {item.Craft.Level}", true)
                .AddField("Craft Recipe", BuildColumn(item.Craft.Items.Select(i => $"`{i.Code}` x{i.Quantity}")), true);
        }

        var usedToCraft = _artifactsService.GetItemsWithThisCraftIngredient(item.Code);
        if (usedToCraft.Count > 0)
        {
            builder.AddField("Used to Craft", BuildColumn(usedToCraft.Select(i => $"`{i.Code}`")));
        }

        var droppedByMonsters = _artifactsService.GetMonstersThatDropThisItem(item.Code);
        if (droppedByMonsters.Count > 0)
        {
            builder.AddField("Dropped By", BuildColumn(droppedByMonsters.Select(m => $"`{m.Code}` {GetDropRateNotation(m.Drops.First(d => d.Code == item.Code))}")));
        }

        return builder.Build();
    }

    public Embed BuildMonsterEmbed(MonsterSchema monster)
    {
        List<string> attacks = new(4);
        if (monster.Attack_fire > 0) { attacks.Add($"Fire: {monster.Attack_fire}"); }
        if (monster.Attack_earth > 0) { attacks.Add($"Earth: {monster.Attack_earth}"); }
        if (monster.Attack_water > 0) { attacks.Add($"Water: {monster.Attack_water}"); }
        if (monster.Attack_air > 0) { attacks.Add($"Air: {monster.Attack_air}"); }

        List<string> resistances = new(4);
        if (monster.Res_fire != 0) { resistances.Add($"Fire: {monster.Res_fire}%"); }
        if (monster.Res_earth != 0) { resistances.Add($"Earth: {monster.Res_earth}%"); }
        if (monster.Res_water != 0) { resistances.Add($"Water: {monster.Res_water}%"); }
        if (monster.Res_air != 0) { resistances.Add($"Air: {monster.Res_air}%"); }

        var builder = new EmbedBuilder()
            .WithTitle($"{monster.Name} (`{monster.Code}`)")
            .WithThumbnailUrl(Constants.GetMonsterImageUrl(monster.Code))
            .AddField("Level", monster.Level, true)
            .AddField("Max HP", monster.Hp, true)
            .AddField("Gold", $"{monster.Min_gold} - {monster.Max_gold}", true)
            .AddField("Attack", BuildColumn(attacks), true)
            .AddField("Resist", BuildColumn(resistances), true)
            .AddField("Drops", BuildColumn(monster.Drops.Select(d => $"`{d.Code}` {GetDropRateNotation(d)}")), true);

        return builder.Build();
    }

    public Embed BuildSimulatorEmbed(CharacterStats characterStats, MonsterSchema monster, List<FightSimulatorResult> results, IEnumerable<string> items, bool isDeterministic, int characterLevel, string characterName = "Character")
    {
        List<string> offense = new(5);
        if (characterStats.FireAttack != 0) { offense.Add($"{characterStats.FireAttack} Fire Atk"); }
        if (characterStats.EarthAttack != 0) { offense.Add($"{characterStats.EarthAttack} Earth Atk"); }
        if (characterStats.WaterAttack != 0) { offense.Add($"{characterStats.WaterAttack} Water Atk"); }
        if (characterStats.AirAttack != 0) { offense.Add($"{characterStats.AirAttack} Air Atk"); }
        if (characterStats.CriticalStrike != 0) { offense.Add($"{characterStats.CriticalStrike}% Critical"); }

        List<string> defense = new(5)
        {
            $"{characterStats.MaxHp} Max HP"
        };
        if (characterStats.FireResist != 0) { defense.Add($"{characterStats.FireResist}% Fire Res"); }
        if (characterStats.EarthResist != 0) { defense.Add($"{characterStats.EarthResist}% Earth Res"); }
        if (characterStats.WaterResist != 0) { defense.Add($"{characterStats.WaterResist}% Water Res"); }
        if (characterStats.AirResist != 0) { defense.Add($"{characterStats.AirResist}% Air Res"); }

        var builder = new EmbedBuilder()
            .WithTitle($"Simulate: {characterName} vs {monster.Name}")
            .AddField("Equipment", $"`{string.Join(',', items)}`")
            .AddField("Character Level", characterLevel, true)
            .AddField("Offense", BuildColumn(offense), true)
            .AddField("Defense", BuildColumn(defense), true);

        int total = results.Count;
        int wins = results.Count(r => r.IsWin);
        int losses = total - wins;

        StringBuilder message = new(isDeterministic
            ? $"This fight is deterministic since neither fighter can block. The player {(wins > 0 ? "won" : "lost")} the fight."
            : $"Simulated {total} fight{Pluralize(total)}. The player won {wins} and lost {losses} ({Percentage(wins, total)}% win rate).");
        message.AppendLine();

        const string rounding = "0.0";

        if (wins > 0)
        {
            if (wins > 1)
            {
                var bestWin = results.Where(r => r.IsWin).MaxBy(r => r.RemainingHp);
                var worstWin = results.Where(r => r.IsWin).MinBy(r => r.RemainingHp);
                message.AppendLine($"Best Win: {bestWin.Turns} turn{Pluralize(bestWin.Turns)}, lost {characterStats.MaxHp - bestWin.RemainingHp} health.");
                message.AppendLine($"Worst Win: {worstWin.Turns} turn{Pluralize(worstWin.Turns)}, lost {characterStats.MaxHp - worstWin.RemainingHp} health.");
            }

            message.AppendLine($"Average Win: {results.Where(r => r.IsWin).Average(r => r.Turns).ToString(rounding)} turns, lost {(characterStats.MaxHp - results.Where(r => r.IsWin).Average(r => r.RemainingHp)).ToString(rounding)} health.");
        }

        if (losses > 0)
        {
            var completedLosses = results.Where(r => !r.IsWin && r.Turns < 100).ToList();
            if (completedLosses.Count > 0)
            {
                if (completedLosses.Count > 1)
                {
                    var bestLoss = completedLosses.MaxBy(r => r.RemainingHp);
                    var worstLoss = completedLosses.MinBy(r => r.RemainingHp);
                    message.AppendLine($"Best Loss: {bestLoss.Turns} turn{Pluralize(bestLoss.Turns)}{(bestLoss.WithHealingTurns < 100 ? $", would need {bestLoss.RemainingHp * -1 + 1} healing to win in {bestLoss.WithHealingTurns} turns" : string.Empty)}.");
                    message.AppendLine($"Worst Loss: {worstLoss.Turns} turn{Pluralize(worstLoss.Turns)}{(worstLoss.WithHealingTurns < 100 ? $", would need {worstLoss.RemainingHp * -1 + 1} healing to win in {worstLoss.WithHealingTurns} turns" : string.Empty)}.");
                }

                double averageHealingTurns = completedLosses.Average(r => r.WithHealingTurns);
                message.AppendLine($"Average Loss: {completedLosses.Average(r => r.Turns).ToString(rounding)} turns{(averageHealingTurns < 100 ? $" and would need {(completedLosses.Average(r => r.RemainingHp) * -1 + 1).ToString(rounding)} healing to win in {averageHealingTurns.ToString(rounding)} turns" : string.Empty)}.");
            }

            int incompleteLosses = losses - completedLosses.Count;
            if (incompleteLosses > 0)
            {
                message.AppendLine($"{incompleteLosses} fights were lost due to reaching 100 turns without a result.");
            }
        }

        builder.AddField("Results", message);
        return builder.Build();
    }

    private static string GetDropRateNotation(DropRateSchema drop) =>
        $"{1.0f / drop.Rate * 100:0.##}%{(drop.Min_quantity == drop.Max_quantity ? string.Empty : $" ({drop.Min_quantity}-{drop.Max_quantity})")}";

    private static string Percentage(int current, int max) => ((float)current / max * 100).ToString("0.#");

    private static string Pluralize(int count) => count == 1 ? string.Empty : "s";

    private static string BuildColumn(IEnumerable<string> items) => string.Join(Environment.NewLine, items).ToInvisibleEmbedIfEmpty();
}
