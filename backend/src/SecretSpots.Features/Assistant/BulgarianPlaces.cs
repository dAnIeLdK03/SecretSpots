namespace SecretSpots.Features.Assistant;

// Coordinates of Bulgarian towns, so the assistant can turn "within 30 km of Plovdiv" into a
// nearby search without an external geocoding service. Covers the 28 province centres and the
// towns people most often travel to. Approximate town centres — good enough for a radius search.
public static class BulgarianPlaces
{
    public record Place(string Name, double Latitude, double Longitude);

    // Bulgarian name first (returned as the canonical name), then common Latin spellings.
    private static readonly (string Name, double Latitude, double Longitude, string[] Aliases)[] Entries =
    [
        ("София", 42.6977, 23.3219, ["Sofia", "Sofiya"]),
        ("Пловдив", 42.1354, 24.7453, ["Plovdiv"]),
        ("Варна", 43.2141, 27.9147, ["Varna"]),
        ("Бургас", 42.5048, 27.4626, ["Burgas", "Bourgas"]),
        ("Русе", 43.8356, 25.9657, ["Ruse", "Rousse"]),
        ("Стара Загора", 42.4258, 25.6345, ["Stara Zagora"]),
        ("Плевен", 43.4170, 24.6067, ["Pleven"]),
        ("Сливен", 42.6817, 26.3229, ["Sliven"]),
        ("Добрич", 43.5726, 27.8273, ["Dobrich"]),
        ("Шумен", 43.2712, 26.9361, ["Shumen"]),
        ("Перник", 42.6052, 23.0378, ["Pernik"]),
        ("Хасково", 41.9344, 25.5554, ["Haskovo", "Khaskovo"]),
        ("Ямбол", 42.4842, 26.5035, ["Yambol"]),
        ("Пазарджик", 42.1928, 24.3336, ["Pazardzhik", "Pazardjik"]),
        ("Благоевград", 42.0209, 23.0943, ["Blagoevgrad"]),
        ("Велико Търново", 43.0757, 25.6172, ["Veliko Tarnovo", "Veliko Turnovo", "Tarnovo", "Търново"]),
        ("Враца", 43.2102, 23.5529, ["Vratsa", "Vraca"]),
        ("Габрово", 42.8742, 25.3187, ["Gabrovo"]),
        ("Видин", 43.9962, 22.8679, ["Vidin"]),
        ("Кърджали", 41.6338, 25.3777, ["Kardzhali", "Kardjali", "Kurdzhali"]),
        ("Кюстендил", 42.2839, 22.6911, ["Kyustendil", "Kjustendil"]),
        ("Монтана", 43.4085, 23.2257, ["Montana"]),
        ("Ловеч", 43.1370, 24.7142, ["Lovech"]),
        ("Разград", 43.5333, 26.5167, ["Razgrad"]),
        ("Силистра", 44.1171, 27.2606, ["Silistra"]),
        ("Смолян", 41.5774, 24.7011, ["Smolyan", "Smoljan"]),
        ("Търговище", 43.2512, 26.5722, ["Targovishte", "Turgovishte"]),
        ("Асеновград", 42.0167, 24.8667, ["Asenovgrad"]),
        ("Казанлък", 42.6194, 25.3929, ["Kazanlak", "Kazanluk"]),
        ("Карлово", 42.6439, 24.8072, ["Karlovo"]),
        ("Велинград", 42.0275, 23.9917, ["Velingrad"]),
        ("Банско", 41.8383, 23.4885, ["Bansko"]),
        ("Сандански", 41.5667, 23.2833, ["Sandanski"]),
        ("Петрич", 41.3983, 23.2072, ["Petrich"]),
        ("Мелник", 41.5234, 23.3925, ["Melnik"]),
        ("Гоце Делчев", 41.5667, 23.7333, ["Gotse Delchev", "Goce Delchev"]),
        ("Самоков", 42.3370, 23.5528, ["Samokov"]),
        ("Троян", 42.8833, 24.7167, ["Troyan", "Trojan"]),
        ("Трявна", 42.8667, 25.4833, ["Tryavna"]),
        ("Копривщица", 42.6366, 24.3593, ["Koprivshtitsa", "Koprivshtica"]),
        ("Белоградчик", 43.6253, 22.6833, ["Belogradchik"]),
        ("Чепеларе", 41.7244, 24.6850, ["Chepelare"]),
        ("Девин", 41.7436, 24.3997, ["Devin"]),
        ("Несебър", 42.6597, 27.7356, ["Nesebar", "Nessebar", "Nesebur"]),
        ("Созопол", 42.4176, 27.6961, ["Sozopol"]),
        ("Балчик", 43.4083, 28.1631, ["Balchik"]),
        ("Свищов", 43.6167, 25.3500, ["Svishtov"]),
        ("Димитровград", 42.0500, 25.6000, ["Dimitrovgrad"]),
    ];

    // Declared before ByNormalizedName: static fields initialise in this order, and building the index
    // normalises every alias.
    private static readonly string[] TownPrefixes = ["гр.", "град ", "гр ", "gr.", "grad ", "town of ", "city of "];

    private static readonly Dictionary<string, Place> ByNormalizedName = BuildIndex();

    // "гр. Пловдив", "град Пловдив", "PLOVDIV", "Veliko-Tarnovo" and "Велико  Търново" all match.
    // Unknown names return null — the caller asks the user for a nearby town instead of guessing.
    public static Place? Find(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ByNormalizedName.GetValueOrDefault(Normalize(name));
    }

    private static Dictionary<string, Place> BuildIndex()
    {
        var index = new Dictionary<string, Place>();
        foreach (var (name, latitude, longitude, aliases) in Entries)
        {
            var place = new Place(name, latitude, longitude);
            foreach (var alias in aliases.Prepend(name))
            {
                index[Normalize(alias)] = place;
            }
        }

        return index;
    }

    private static string Normalize(string name)
    {
        var normalized = name.Trim().ToLowerInvariant().Replace('-', ' ');
        foreach (var prefix in TownPrefixes)
        {
            if (normalized.StartsWith(prefix, StringComparison.Ordinal))
            {
                normalized = normalized[prefix.Length..];
                break;
            }
        }

        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.', ',');
    }
}
