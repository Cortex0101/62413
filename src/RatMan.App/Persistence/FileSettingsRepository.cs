using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;
using RatMan.Game;

namespace RatMan.App.Persistence;

public interface ISettingsRepository
{
    Task<IReadOnlyList<MovementBinding>> LoadAsync();
    Task SaveAsync(IReadOnlyList<MovementBinding> bindings);
}

public sealed class FileSettingsRepository : ISettingsRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
    };
    private readonly string path;

    public FileSettingsRepository(string? path = null) => this.path = Path.GetFullPath(path ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RatMan", "settings.json"));

    public async Task<IReadOnlyList<MovementBinding>> LoadAsync()
    {
        string json;
        try { json = await File.ReadAllTextAsync(path).ConfigureAwait(false); }
        catch (FileNotFoundException) { return MovementBinding.Defaults; }
        catch (DirectoryNotFoundException) { return MovementBinding.Defaults; }

        SettingsFile? file;
        try { file = JsonSerializer.Deserialize<SettingsFile>(json, JsonOptions); }
        catch (JsonException exception) { throw new InvalidDataException("Invalid settings JSON.", exception); }
        if (file is not { Version: 1, Movement: { } bindings } || bindings.Length != MovementBinding.Defaults.Count)
            throw new InvalidDataException("Invalid movement settings file.");

        BindingEntry?[] orderedBindings = bindings.OrderBy(binding => binding?.Direction).ToArray();
        MovementBinding[] result = new MovementBinding[orderedBindings.Length];
        HashSet<Key> keys = [];
        for (int i = 0; i < orderedBindings.Length; i++)
        {
            if (orderedBindings[i] is not { } binding || binding.Direction != MovementBinding.Defaults[i].Direction ||
                !MovementBinding.CanBind(binding.FirstKey) || !MovementBinding.CanBind(binding.SecondKey) ||
                !keys.Add(binding.FirstKey) || !keys.Add(binding.SecondKey))
                throw new InvalidDataException("Invalid movement key bindings.");
            result[i] = new MovementBinding(binding.Direction, binding.FirstKey, binding.SecondKey);
        }

        return result;
    }

    public async Task SaveAsync(IReadOnlyList<MovementBinding> bindings)
    {
        BindingEntry[] entries = bindings.Select(binding =>
            new BindingEntry(binding.Direction, binding.FirstKey, binding.SecondKey)).ToArray();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(new SettingsFile(1, entries), JsonOptions))
                .ConfigureAwait(false);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private sealed record SettingsFile(int Version, BindingEntry?[] Movement);
    private sealed record BindingEntry(Direction Direction, Key FirstKey, Key SecondKey);
}
