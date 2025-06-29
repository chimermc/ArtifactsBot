using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using static ArtifactsBot.Services.Enums;

namespace ArtifactsBot.Services;

public partial class ArtifactsService
{
    private readonly AppInsightsLogService _logService;
    private readonly ArtifactsClient _client;
    private FrozenDictionary<string, ItemSchema> _items;
    private FrozenDictionary<string, MonsterSchema> _monsters;
    private string _serverVersion;

    public ArtifactsService(AppInsightsLogService logService)
    {
        _logService = logService;
        HttpClientHandler handler = new() { UseCookies = false };
        HttpClient serviceHttpClient = new(handler);
        _client = new ArtifactsClient(Constants.BaseUrl, serviceHttpClient);
        var statusTask = GetStatus();
        var itemsTask = GetAllItems();
        var monstersTask = GetAllMonsters();
        Task.Run(() => Task.WhenAll(statusTask, itemsTask, monstersTask));
        _serverVersion = statusTask.Result.Version;
        _items = itemsTask.Result;
        _monsters = monstersTask.Result;
    }

    #region Data

    public async Task<StatusSchema> GetStatus(CancellationToken cancellationToken = default) => (await DoWithRetry(_client.Get_server_details__getAsync, cancellationToken)).Data;

    private async Task<FrozenDictionary<string, ItemSchema>> GetAllItems(CancellationToken cancellationToken = default)
    {
        return await DoWithRetry(GetAllItemsSub, cancellationToken);

        async Task<FrozenDictionary<string, ItemSchema>> GetAllItemsSub()
        {
            const int resultsPerPage = 100;
            var response = await _client.Get_all_items_items_getAsync(null, null, null, null, null, null, 1, resultsPerPage, cancellationToken);
            int pages = response.Pages ?? 100;
            List<ItemSchema> items = new(response.Total ?? 256);
            items.AddRange(response.Data);
            for (int i = 2; i <= pages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                response = await _client.Get_all_items_items_getAsync(null, null, null, null, null, null, i, resultsPerPage, cancellationToken);
                items.AddRange(response.Data);
            }

            return items.Select(item => new KeyValuePair<string, ItemSchema>(item.Code, item)).ToFrozenDictionary();
        }
    }

    private async Task<FrozenDictionary<string, MonsterSchema>> GetAllMonsters(CancellationToken cancellationToken = default)
    {
        return await DoWithRetry(GetAllMonstersSub, cancellationToken);

        async Task<FrozenDictionary<string, MonsterSchema>> GetAllMonstersSub()
        {
            const int resultsPerPage = 100;
            var response = await _client.Get_all_monsters_monsters_getAsync(null, null, null, null, 1, resultsPerPage, cancellationToken);
            int pages = response.Pages ?? 100;
            List<MonsterSchema> monsters = new(response.Total ?? 256);
            monsters.AddRange(response.Data);
            for (int i = 2; i <= pages; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                response = await _client.Get_all_monsters_monsters_getAsync(null, null, null, null, i, resultsPerPage, cancellationToken);
                monsters.AddRange(response.Data);
            }

            return monsters.Select(m => new KeyValuePair<string, MonsterSchema>(m.Code, m)).ToFrozenDictionary();
        }
    }

    public async Task<CharacterSchema> GetCharacter(string characterName, CancellationToken cancellationToken = default)
    {
        var response = await DoWithRetry(() => _client.Get_character_characters__name__getAsync(characterName, cancellationToken), cancellationToken);
        return response.Data;
    }

    public ItemSchema GetItemByCode(string item) => _items.TryGetValue(item, out var itemSchema)
        ? itemSchema
        : throw new ControlException(ControlReason.InvalidResource, "This item does not exist.");

    public MonsterSchema GetMonsterByCode(string monster) => _monsters.TryGetValue(monster, out var monsterSchema)
        ? monsterSchema
        : throw new ControlException(ControlReason.InvalidResource, "This monster does not exist.");

    public List<ItemSchema> GetItemsWithThisCraftIngredient(string itemCode) => _items.Values.Where(i => i.Craft != null && i.Craft.Items.Any(c => c.Code == itemCode)).ToList();

    public List<MonsterSchema> GetMonstersThatDropThisItem(string itemCode) => _monsters.Values.Where(m => m.Drops.Any(d => d.Code == itemCode)).ToList();

    public static ItemSlot? GetSlotForItem(ItemSchema item)
    {
        if (item.Type == null) { return null; }

        return item.Type switch
        {
            "ring" => ItemSlot.Ring1,
            "utility" => ItemSlot.Utility1,
            "artifact" => ItemSlot.Artifact1,
            _ => Enum.TryParse<ItemSlot>(item.Type, true, out var slot) ? slot : null
        };
    }

    #endregion Data

    public async Task ReloadGameData(CancellationToken cancellationToken = default)
    {
        var itemsTask = GetAllItems(cancellationToken);
        var monstersTask = GetAllMonsters(cancellationToken);
        await Task.WhenAll(itemsTask, monstersTask);
        _items = itemsTask.Result;
        _monsters = monstersTask.Result;
    }

    public async Task<bool> CheckForServerUpdate(CancellationToken cancellationToken = default)
    {
        var status = await GetStatus(cancellationToken);
        if (status.Version == _serverVersion) { return false; }

        _logService.LogInfo($"Detected server version update from '{_serverVersion}' to '{status.Version}'.");
        _serverVersion = status.Version;
        await ReloadGameData(cancellationToken);
        return true;
    }

    // 478: Insufficient items in inventory
    // 429: CloudFlare rate limiting
    // 461: A transaction is already in progress with this item/your golds in your bank
    // 486: This action is already in progress (Happens when the last request hasn't finished processing. Should only happen when server is lagging.)
    // 490: Character is already at this position
    // 493: Character level too low
    // 497: Inventory full
    // 499: Character is in cooldown
    // 598: Required resource is not at this position

    private async Task<TResult> DoWithRetry<TResult>(Func<Task<TResult>> action, CancellationToken cancellationToken = default, [CallerMemberName] string caller = "")
    {
        for (int i = 0; i < Constants.MaxRetries; i++)
        {
            try
            {
                return await action();
            }
            catch (ApiException ex) when (ex.StatusCode is >= 500 or 486 or 461 or 429 or 409)
            {
                _logService.LogWarning(ex.ToString(), $"{nameof(DoWithRetry)}:{caller}");
                await Task.Delay(5000, cancellationToken);
            }
            catch (ApiException)
            {
                // Any other type of ApiException won't be handled here
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logService.LogWarning("Task canceled.", $"{nameof(DoWithRetry)}:{caller}");
                throw;
            }
            catch (Exception ex)
            {
                _logService.LogError(ex.ToString(), $"{nameof(DoWithRetry)}:{caller}");
                await Task.Delay(5000, cancellationToken);
            }
        }

        throw new ControlException(ControlReason.OutOfRetries, "Max retries exceeded.");
    }
}
