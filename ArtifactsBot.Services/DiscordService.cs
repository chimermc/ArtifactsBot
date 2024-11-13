using ArtifactsBot.Services.Extensions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using static ArtifactsBot.Services.Enums;

namespace ArtifactsBot.Services;

public partial class DiscordService
{
    private readonly AppInsightsLogService _logService;
    private readonly ArtifactsService _artifactsService;
    private readonly string _token;

    public DiscordService(AppInsightsLogService logService, ArtifactsService artifactsService, string token)
    {
        _logService = logService;
        _artifactsService = artifactsService;
        _token = token;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        DiscordSocketClient client = new(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.None
        });
        client.Log += Log;
        client.SlashCommandExecuted += SlashCommandHandler;

#if DEBUG
        // This shouldn't be called on every app startup. Only run manually, one time, when command signatures have changed.
        //client.Ready += () => AddCommands(client);
#endif

        await client.LoginAsync(TokenType.Bot, _token);
        await client.StartAsync();

        await Task.Delay(Timeout.Infinite, cancellationToken);
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        string logMessage = $"{command.User.Username} requested {command.Data.Name}{(command.Data.Options.Count == 0 ? "" : $"({string.Join(", ", command.Data.Options.Select(o => $"{o.Name}: '{o.Value}'"))})")}";
        try
        {
            switch (command.Data.Name)
            {
                case Constants.CommandSimulate:
                    await SimulateCommand(command);
                    break;
                case Constants.CommandSimulateCharacter:
                    await SimulateCharacterCommand(command);
                    break;
                case Constants.CommandCharacter:
                    await CharacterCommand(command);
                    break;
                case Constants.CommandCharacterEquipment:
                    await CharacterEquipmentCommand(command);
                    break;
                case Constants.CommandMonster:
                    await MonsterCommand(command);
                    break;
                case Constants.CommandItem:
                    await ItemCommand(command);
                    break;
                default:
                    throw new NotImplementedException();
            }

            _logService.LogInfo(logMessage);
        }
        catch (ControlException ex) when (ex.Reason == ControlReason.CommandResponse)
        {
            _logService.LogWarning($"{logMessage}, but {ex.Message}");
            await command.RespondAsync(ex.Message);
        }
        catch (Exception ex)
        {
            string referenceCode = Guid.NewGuid().ToString("N");
            _logService.LogCritical($"Unhandled exception when {logMessage}: {ex}", properties: new Dictionary<string, string> { { "ReferenceCode", referenceCode } });
            await command.RespondAsync($"An unhandled exception occurred (reference code: `{referenceCode}`).");
        }
    }

    #region Commands

    private async Task SimulateCommand(SocketSlashCommand command)
    {
        string? monsterName = command.Data.Options.FirstOrDefault(o => o.Name == "monster")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(monsterName))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `monster`.");
        }

        string? inputItems = command.Data.Options.FirstOrDefault(o => o.Name == "items")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(inputItems))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `items`.");
        }

        string? inputLevel = command.Data.Options.FirstOrDefault(o => o.Name == "level")?.Value.ToString()?.Trim();
        int level = 40;
        if (!string.IsNullOrEmpty(inputLevel) && !int.TryParse(inputLevel, out level))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `level`.");
        }
        if (level is < 1 or > 40)
        {
            throw new ControlException(ControlReason.CommandResponse, "`level` must be between 1 and 40.");
        }

        var monster = GetMonster(monsterName);

        string[] inputItemSplit = inputItems.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<ItemSchema> items = new(inputItemSplit.Length);
        foreach (string itemName in inputItemSplit)
        {
            var item = GetItem(itemName);
            var itemSlot = ArtifactsService.GetSlotForItem(item);
            if (itemSlot is ItemSlot.Weapon or ItemSlot.Shield or ItemSlot.Helmet or ItemSlot.Body_armor or ItemSlot.Leg_armor or ItemSlot.Boots or ItemSlot.Amulet)
            {
                if (items.Any(i => i.Type == item.Type))
                {
                    throw new ControlException(ControlReason.CommandResponse, $"Cannot have more than one {itemSlot} item.");
                }

                items.Add(item);
            }
            else if (itemSlot == ItemSlot.Ring1)
            {
                if (items.Count(i => i.Type == item.Type) > 1)
                {
                    throw new ControlException(ControlReason.CommandResponse, "Cannot have more than two Ring items.");
                }

                items.Add(item);
            }
            else if (itemSlot == ItemSlot.Artifact1)
            {
                if (items.Count(i => i.Type == item.Type) > 2)
                {
                    throw new ControlException(ControlReason.CommandResponse, "Cannot have more than three Artifact items.");
                }
                if (items.Any(i => i.Code == item.Code))
                {
                    throw new ControlException(ControlReason.CommandResponse, $"Cannot have more than one {item.Code}; equipping duplicate artifacts is not allowed.");
                }

                items.Add(item);
            }
        }

        var stats = ArtifactsService.GetCharacterStats(items, level);
        (var fightSimulatorResults, bool isDeterministic) = ArtifactsService.SimulateFight(stats, monster, Constants.FightSimulatorIterations);
        await command.RespondAsync(embed: BuildSimulatorEmbed(stats, monster, fightSimulatorResults, items.Select(i => i.Code), isDeterministic, level));
    }

    private async Task SimulateCharacterCommand(SocketSlashCommand command)
    {
        string? characterName = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `name`.");
        }

        string? monsterName = command.Data.Options.FirstOrDefault(o => o.Name == "monster")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(monsterName))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `monster`.");
        }

        var monster = GetMonster(monsterName);
        var character = await GetCharacter(characterName);
        var itemCodes = ArtifactsService.GetCharacterEquipmentItemCodes(character);
        var stats = ArtifactsService.GetCharacterStats(itemCodes.Select(GetItem), character.Level);
        (var fightSimulatorResults, bool isDeterministic) = ArtifactsService.SimulateFight(stats, monster, Constants.FightSimulatorIterations);
        await command.RespondAsync(embed: BuildSimulatorEmbed(stats, monster, fightSimulatorResults, itemCodes, isDeterministic, character.Level, character.Name));
    }

    private async Task CharacterCommand(SocketSlashCommand command)
    {
        string? characterName = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `name`.");
        }

        var character = await GetCharacter(characterName);
        await command.RespondAsync(embed: BuildCharacterEmbed(character));
    }

    private async Task CharacterEquipmentCommand(SocketSlashCommand command)
    {
        string? characterName = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(characterName))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `name`.");
        }

        var character = await GetCharacter(characterName);
        await command.RespondAsync($"`{string.Join(',', ArtifactsService.GetCharacterEquipmentItemCodes(character))}`");
    }

    private async Task MonsterCommand(SocketSlashCommand command)
    {
        string? monsterName = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(monsterName))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `name`.");
        }

        var monster = GetMonster(monsterName);
        await command.RespondAsync(embed: BuildMonsterEmbed(monster));
    }

    private async Task ItemCommand(SocketSlashCommand command)
    {
        string? itemName = command.Data.Options.FirstOrDefault(o => o.Name == "name")?.Value.ToString()?.Trim();
        if (string.IsNullOrEmpty(itemName))
        {
            throw new ControlException(ControlReason.CommandResponse, "Invalid `name`.");
        }

        var item = GetItem(itemName);
        await command.RespondAsync(embed: BuildItemEmbed(item));
    }

    #endregion Commands

    private async Task<CharacterSchema> GetCharacter(string characterName)
    {
        try
        {
            return await _artifactsService.GetCharacter(characterName);
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            throw new ControlException(ControlReason.CommandResponse, "No character with that name exists. (This lookup is case-sensitive.)");
        }
    }

    private MonsterSchema GetMonster(string monster)
    {
        try
        {
            return _artifactsService.GetMonsterByCode(monster.ToCodeFormat());
        }
        catch (ControlException ex) when (ex.Reason == ControlReason.InvalidResource)
        {
            throw new ControlException(ControlReason.CommandResponse, $"Monster `{monster}` does not exist.");
        }
    }

    private ItemSchema GetItem(string item)
    {
        try
        {
            return _artifactsService.GetItemByCode(item.ToCodeFormat());
        }
        catch (ControlException ex) when (ex.Reason == ControlReason.InvalidResource)
        {
            throw new ControlException(ControlReason.CommandResponse, $"Item `{item}` does not exist.");
        }
    }

    /// <summary>
    /// Method to receive events from DiscordSocketClient's Log event source.
    /// </summary>
    private Task Log(LogMessage logMessage)
    {
        string message = logMessage.Exception is CommandException ex
            ? ex.ToString()
            : logMessage.Message;

        switch (logMessage.Severity)
        {
            case LogSeverity.Critical:
                _logService.LogCritical(message);
                break;
            case LogSeverity.Error:
                _logService.LogError(message);
                break;
            case LogSeverity.Warning:
                _logService.LogWarning(message);
                break;
            default:
                _logService.LogInfo(message);
                break;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Register commands that the bot supports.
    /// This does not need to be called every time the application starts; after calling it once, Discord will remember it.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    private async Task AddCommands(DiscordSocketClient client)
    {
        SlashCommandBuilder[] commandBuilders =
        [
            new SlashCommandBuilder()
                .WithName(Constants.CommandSimulate)
                .WithDescription("Simulates a battle between a monster and a character with the specified items equipped.")
                .AddOption("monster", ApplicationCommandOptionType.String, "The code or name of the monster.", isRequired: true)
                .AddOption("items", ApplicationCommandOptionType.String, "The codes or names of the items equipped, separated by commas.", isRequired: true)
                .AddOption("level", ApplicationCommandOptionType.Integer, "Player level (defaults to 40).", isRequired: false),
            new SlashCommandBuilder()
                .WithName(Constants.CommandSimulateCharacter)
                .WithDescription("Simulates a battle between a monster and a specific character.")
                .AddOption("monster", ApplicationCommandOptionType.String, "The code of the monster.", isRequired: true)
                .AddOption("name", ApplicationCommandOptionType.String, "The name of the character (case sensitive).", isRequired: true),
            new SlashCommandBuilder()
                .WithName(Constants.CommandCharacter)
                .WithDescription("Displays a specific character's information.")
                .AddOption("name", ApplicationCommandOptionType.String, "The name of the character (case sensitive).", isRequired: true),
            new SlashCommandBuilder()
                .WithName(Constants.CommandCharacterEquipment)
                .WithDescription("Get the item codes of a character's equipped items, in the format expected by /simulate")
                .AddOption("name", ApplicationCommandOptionType.String, "The name of the character (case sensitive).", isRequired: true),
            new SlashCommandBuilder()
                .WithName(Constants.CommandMonster)
                .WithDescription("Displays a monster's information.")
                .AddOption("name", ApplicationCommandOptionType.String, "The code or name of the monster.", isRequired: true),
            new SlashCommandBuilder()
                .WithName(Constants.CommandItem)
                .WithDescription("Displays an item's information.")
                .AddOption("name", ApplicationCommandOptionType.String, "The code or name of the item.", isRequired: true)
        ];

        try
        {
            // ReSharper disable once CoVariantArrayConversion
            await client.BulkOverwriteGlobalApplicationCommandsAsync(commandBuilders.Select(c => c.Build()).ToArray());
        }
        catch (Exception ex)
        {
            _logService.LogCritical(ex.ToString());
        }
    }
}
