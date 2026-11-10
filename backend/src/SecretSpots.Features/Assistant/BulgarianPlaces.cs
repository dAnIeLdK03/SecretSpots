namespace SecretSpots.Features.Assistant;

// Coordinates of Bulgarian towns, so the assistant can turn "within 30 km of Plovdiv" into a
// nearby search without an external geocoding service. Covers the 28 province centres, the
// towns people most often travel to, and the resorts, villages and landmarks they search around. Approximate town centres — good enough for a radius search.
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

        // Mountain and sea resorts, villages and monasteries that people search for spots around.
        ("Боровец", 42.2667, 23.6000, ["Borovets"]),
        ("Пампорово", 41.6500, 24.6833, ["Pamporovo"]),
        ("Рилски манастир", 42.1333, 23.3400, ["Rila Monastery", "Rilski Manastir", "Рила"]),
        ("Витоша", 42.5800, 23.2500, ["Vitosha"]),
        ("Черни връх", 42.5639, 23.2750, ["Cherni Vrah"]),
        ("Мусала", 42.1792, 23.5853, ["Musala"]),
        ("Вихрен", 41.7667, 23.4000, ["Vihren", "Vikhren"]),
        ("Бузлуджа", 42.7358, 25.3944, ["Buzludzha", "Buzludja"]),
        ("Шипка", 42.7333, 25.3167, ["Shipka"]),
        ("Бачково", 41.9500, 24.8500, ["Bachkovo"]),
        ("Рожен", 41.5000, 23.4000, ["Rozhen"]),
        ("Перперикон", 41.6900, 25.4600, ["Perperikon"]),
        ("Широка лъка", 41.6833, 24.5667, ["Shiroka Laka", "Shiroka Luka"]),
        ("Триград", 41.6167, 24.3833, ["Trigrad"]),
        ("Арбанаси", 43.0833, 25.6667, ["Arbanasi"]),
        ("Жеравна", 42.8667, 26.4167, ["Zheravna"]),
        ("Котел", 42.9000, 26.4500, ["Kotel"]),
        ("Златни пясъци", 43.2833, 28.0400, ["Golden Sands", "Zlatni Pyasatsi"]),
        ("Албена", 43.3667, 28.0833, ["Albena"]),
        ("Слънчев бряг", 42.6930, 27.7090, ["Sunny Beach", "Slanchev Bryag"]),
        ("Обзор", 42.8200, 27.8830, ["Obzor"]),
        ("Поморие", 42.5667, 27.6500, ["Pomorie"]),
        ("Приморско", 42.2667, 27.7500, ["Primorsko"]),
        ("Царево", 42.1667, 27.8500, ["Tsarevo"]),
        ("Ахтопол", 42.1000, 27.9400, ["Ahtopol"]),
        ("Синеморец", 42.0600, 27.9800, ["Sinemorets"]),
        ("Резово", 41.9833, 28.0167, ["Rezovo"]),
        ("Малко Търново", 42.0167, 27.5333, ["Malko Tarnovo"]),
        ("Каварна", 43.4333, 28.3333, ["Kavarna"]),
        ("Шабла", 43.5333, 28.5333, ["Shabla"]),
        ("Горна Оряховица", 43.1264, 25.6989, ["Gorna Oryahovitsa", "Gorna Oryahovica"]),
        ("Дряново", 42.9833, 25.4833, ["Dryanovo"]),
        ("Елена", 42.9167, 25.8750, ["Elena"]),
        ("Севлиево", 43.0167, 25.1167, ["Sevlievo"]),
        ("Етрополе", 42.8167, 24.0000, ["Etropole"]),
        ("Тетевен", 42.9167, 24.2667, ["Teteven"]),
        ("Калофер", 42.6000, 24.9833, ["Kalofer"]),
        ("Сопот", 42.6500, 24.7500, ["Sopot"]),
        ("Хисаря", 42.5833, 24.7167, ["Hisarya", "Hissarya"]),
        ("Батак", 41.9500, 24.2167, ["Batak"]),
        ("Брацигово", 42.0167, 24.3417, ["Bratsigovo"]),
        ("Златоград", 41.3833, 25.0833, ["Zlatograd"]),
        ("Мадан", 41.5000, 24.9500, ["Madan"]),
        ("Рудозем", 41.4833, 24.8500, ["Rudozem"]),
        ("Ардино", 41.5833, 25.1500, ["Ardino"]),
        ("Крумовград", 41.4667, 25.6500, ["Krumovgrad"]),
        ("Ивайловград", 41.5333, 26.1167, ["Ivaylovgrad"]),
        ("Свиленград", 41.7667, 26.2000, ["Svilengrad"]),
        ("Разлог", 41.8833, 23.4667, ["Razlog"]),
        ("Дупница", 42.2667, 23.1167, ["Dupnitsa"]),
        ("Долна баня", 42.2967, 23.5256, ["Dolna Banya"]),
        ("Костенец", 42.2667, 23.8167, ["Kostenets"]),
        ("Ихтиман", 42.4333, 23.8167, ["Ihtiman", "Ikhtiman"]),
        ("Банкя", 42.7083, 23.1167, ["Bankya"]),
        ("Трън", 42.8333, 22.6167, ["Tran"]),
        ("Берковица", 43.2333, 23.1167, ["Berkovitsa"]),
        ("Чипровци", 43.3833, 23.4000, ["Chiprovtsi"]),
        ("Козлодуй", 43.7667, 23.7500, ["Kozloduy"]),
        ("Провадия", 43.1833, 27.4333, ["Provadia", "Provadiya"]),
        ("Мадара", 43.2667, 27.1333, ["Madara"]),
        ("Велики Преслав", 43.1667, 26.8167, ["Veliki Preslav", "Преслав", "Preslav"]),
        ("Айтос", 42.7000, 27.2500, ["Aytos"]),
        ("Карнобат", 42.6500, 26.9833, ["Karnobat"]),
        ("Елхово", 42.1667, 26.5667, ["Elhovo", "Elkhovo"]),
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
