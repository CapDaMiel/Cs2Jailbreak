using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace PlayerModelChanger;

public abstract class EntryKey
{
    public string Content { get; set; }

    public EntryKey(string content)
    {
        this.Content = content;
    }

    public bool Equals(EntryKey? key)
    {
        if (!(key is EntryKey))
        {
            return false;
        }
        if (key == null)
        {
            return false;
        }
        if (this is AllKey)
        {
            return true;
        }
        return Content.Equals(key.Content);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EntryKey)obj);
    }
    public override int GetHashCode()
    {
        return Content.GetHashCode();
    }

    public abstract bool Fits(CCSPlayerController player);
}

class SteamIDKey : EntryKey
{
    public SteamIDKey(string content) : base(content) { }
    public override bool Fits(CCSPlayerController player)
    {
        return Content == player.AuthorizedSteamID?.SteamId64.ToString();
    }
}
class PermissionFlagKey : EntryKey
{
    public PermissionFlagKey(string content) : base(content) { }
    public override bool Fits(CCSPlayerController player)
    {
        return AdminManager.PlayerHasPermissions(player, [Content]);
    }
}
class PermissionGroupKey : EntryKey
{
    public PermissionGroupKey(string content) : base(content) { }
    public override bool Fits(CCSPlayerController player)
    {
        return AdminManager.PlayerInGroup(player, [Content]);
    }
}
class AllKey : EntryKey
{
    public AllKey() : base("") { }
    public override bool Fits(CCSPlayerController player)
    {
        return true;
    }
}

public class DefaultModelEntry
{
    public EntryKey key;
    public DefaultModel item; // model index
    public Side side;

    public DefaultModelEntry(EntryKey key, DefaultModel item, Side side)
    {
        this.key = key;
        this.item = item;
        this.side = side;
    }
}

public class DefaultModel
{
    public required string index;
    public bool force;
}

class ConfigDefaultModelsTemplate
{
    [JsonProperty("all")] public Dictionary<string, DefaultModel>? allModels = null;
    [JsonProperty("t")] public Dictionary<string, DefaultModel>? tModels = null;
    [JsonProperty("ct")] public Dictionary<string, DefaultModel>? ctModels = null;

}
class ConfigTemplate
{
    [JsonProperty("DefaultModels")] public required ConfigDefaultModelsTemplate models { get; set; }
}

public class DefaultModelManager
{

    private List<DefaultModelEntry> DefaultModels = new List<DefaultModelEntry>();


    public void ReloadConfig(string ModuleDirectory, ModelService service)
    {
        var filePath = Path.Join(ModuleDirectory, "../../configs/plugins/PlayerModelChanger/DefaultModels.json");
        if (File.Exists(filePath))
        {
            StreamReader reader = File.OpenText(filePath);
            string content = reader.ReadToEnd();
            ConfigTemplate config = JsonConvert.DeserializeObject<ConfigTemplate>(content)!;

            DefaultModels = ParseModelConfig(config.models, service);
        }
        else
        {
            PlayerModelChanger.getInstance().Logger.LogInformation("'DefaultModels.json' not found. Disabling default models feature.");
        }
    }

    private static EntryKey ParseKey(string key)
    {
        EntryKey entryKey;
        if (key == "*")
        {
            entryKey = new AllKey();
        }
        else if (key.StartsWith("@"))
        {
            entryKey = new PermissionFlagKey(key);
        }
        else if (key.StartsWith("#"))
        {
            entryKey = new PermissionGroupKey(key);
        }
        else
        {
            entryKey = new SteamIDKey(key);
        }
        return entryKey;
    }
    private static List<DefaultModelEntry> ParseModelConfig(ConfigDefaultModelsTemplate config, ModelService service)
    {
        List<DefaultModelEntry> defaultModels = new List<DefaultModelEntry>();

        if (config.allModels != null)
        {
            foreach (var model in config.allModels)
            {
                var key = ParseKey(model.Key);
                defaultModels.Add(new DefaultModelEntry(key, model.Value, Side.CT));
                defaultModels.Add(new DefaultModelEntry(key, model.Value, Side.T));
            }
        }
        if (config.tModels != null)
        {
            foreach (var model in config.tModels)
            {
                var key = ParseKey(model.Key);
                defaultModels.RemoveAll(entry => entry.key.Equals(key) && entry.side == Side.T);
                defaultModels.Add(new DefaultModelEntry(key, model.Value, Side.T));
            }
        }
        if (config.ctModels != null)
        {
            foreach (var model in config.ctModels)
            {
                var key = ParseKey(model.Key);
                defaultModels.RemoveAll(entry => entry.key.Equals(key) && entry.side == Side.CT);
                defaultModels.Add(new DefaultModelEntry(key, model.Value, Side.CT));
            }
        }
        for (var i = 0; i < defaultModels.Count; i++)
        {

            if (service.GetModel(defaultModels[i].item.index) == null)
            {
                PlayerModelChanger.getInstance().Logger.LogInformation($"model '${defaultModels[i].item.index}' defined in DefaultModels.json does not exist. Skipped.");
                defaultModels.RemoveAt(i);
            }
        }
        return defaultModels;
    }

    // stage 0 : search steam id
    // stage 1 : search permission flag
    // stage 2 : search permission group
    // stage 3 : search all

    private DefaultModelEntry? GetPlayerDefaultModelWithPriority(List<DefaultModelEntry> entries)
    {
        var result = entries.Find(entry => entry.key is SteamIDKey);
        if (result != null) return result;
        result = entries.Find(entry => entry.key is PermissionFlagKey);
        if (result != null) return result;
        result = entries.Find(entry => entry.key is PermissionGroupKey);
        if (result != null) return result;
        result = entries.Find(entry => entry.key is AllKey);
        return result;
    }
    public DefaultModel? GetPlayerDefaultModel(CCSPlayerController player, Side side)
    {
        var filter1 = DefaultModels.Where(entry => entry.side == side && entry.key.Fits(player)).ToList();
        if (filter1 == null)
        {
            return null;
        }
        var result = GetPlayerDefaultModelWithPriority(filter1);
        if (result == null)
        {
            return null;
        }
        if (result.item == null || result.item.index == "")
        {
            return null;
        }
        return result.item;
    }
}
