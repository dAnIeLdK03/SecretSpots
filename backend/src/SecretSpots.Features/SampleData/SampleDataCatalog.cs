using SecretSpots.Domain;

namespace SecretSpots.Features.SampleData;

// The fixed content SampleDataSeeder inserts. Kept apart from the seeding logic so adding a spot
// or a comment is a one-line change here.
internal static class SampleDataCatalog
{
    // example.com is reserved (RFC 2606), so these can never collide with a real signup — and the
    // seeder uses "any user on this domain exists" to know it has already run.
    public const string EmailDomain = "example.com";

    public record SampleUser(string Email, string DisplayName, UserRole Role);

    public record SampleSpot(
        string Name,
        string Description,
        SpotCategory Category,
        double Latitude,
        double Longitude,
        SpotModerationStatus Status = SpotModerationStatus.Approved,
        string? RejectionReason = null);

    public static readonly SampleUser[] Users =
    [
        new($"admin@{EmailDomain}", "Админ Демо", UserRole.Admin),
        new($"maria.ivanova@{EmailDomain}", "Мария Иванова", UserRole.User),
        new($"georgi.petrov@{EmailDomain}", "Георги Петров", UserRole.User),
        new($"elena.dimitrova@{EmailDomain}", "Елена Димитрова", UserRole.User),
        new($"nikolay.stoyanov@{EmailDomain}", "Николай Стоянов", UserRole.User),
        new($"ivana.georgieva@{EmailDomain}", "Ивана Георгиева", UserRole.User),
        new($"petar.kolev@{EmailDomain}", "Петър Колев", UserRole.User),
        new($"desislava.todorova@{EmailDomain}", "Десислава Тодорова", UserRole.User),
    ];

    public static readonly SampleSpot[] Spots =
    [
        new("Седемте рилски езера", "Циркус от ледникови езера в Рила. Най-красиво е рано сутрин, преди да дойдат групите от лифта.", SpotCategory.BeachOrLake, 42.2037, 23.3194),
        new("Белоградчишките скали", "Червени пясъчникови скали с причудливи форми, гледани от крепостта Калето.", SpotCategory.RockFormation, 43.6230, 22.6830),
        new("Крушунски водопади", "Каскади от травертин сред гора — лятото е прохладно, а пътеките са кратки и лесни.", SpotCategory.Waterfall, 43.2430, 25.0280),
        new("Пещера Леденика", "Пещера със зала „Концертна“ и впечатляващи сталактити, на час от Враца.", SpotCategory.Cave, 43.2040, 23.4930),
        new("Бузлуджа", "Изоставеният паметник-дом на връх Бузлуджа. Отвън е достъпен, вътре — не влизайте.", SpotCategory.Abandoned, 42.7358, 25.3936),
        new("Рилски манастир", "Най-големият манастир в България, с пъстри стенописи и тишина привечер.", SpotCategory.MonasteryOrChurch, 42.1335, 23.3402),
        new("Мелнишки пирамиди", "Пясъчни „пирамиди“ над най-малкия град в България. Пътеката от Мелник до Роженския манастир минава през тях.", SpotCategory.RockFormation, 41.5250, 23.3940),
        new("Перперикон", "Скален град на тракийци и римляни над Кърджали. Носете вода — няма сянка.", SpotCategory.FortressRuins, 41.7180, 25.4650),
        new("Царевец", "Средновековната крепост във Велико Търново. Вечер понякога има светлинно шоу.", SpotCategory.FortressRuins, 43.0840, 25.6520),
        new("Ягодинска пещера", "Пещера с подземни езера и пещерни бисери в сърцето на Родопите.", SpotCategory.Cave, 41.6280, 24.3290),
        new("Чудните мостове", "Два огромни скални моста над река Еркюприя, на около час от Чепеларе.", SpotCategory.RockFormation, 41.8190, 24.5810),
        new("Побити камъни", "Каменна „гора“ от колони край Варна, особено фотогенична по залез.", SpotCategory.RockFormation, 43.2270, 27.7060),
        new("Черни връх", "Най-високата точка на Витоша. При ясно време се вижда Рила.", SpotCategory.Viewpoint, 42.5636, 23.2786),
        new("Боянски водопад", "Около 15-метров водопад на Витоша, до който се стига от Бояна за около два часа.", SpotCategory.Waterfall, 42.6280, 23.2580),
        new("Проходна пещера", "Пещерата с „Очите на Бога“ — два отвора в тавана, през които влиза светлина.", SpotCategory.Cave, 43.1730, 24.0700),
        new("Кайлъка", "Парк с варовикови скали и езера в южния край на Плевен — идеален за разходка.", SpotCategory.Nature, 43.3870, 24.6170),
        new("Плаж Иракли", "Един от малкото диви плажове по Черноморието, между Обзор и Емона.", SpotCategory.BeachOrLake, 42.7470, 27.8890),
        new("Скалите при Тюленово", "Отвесни скали над морето. Водата е кристална, а залезите са невероятни.", SpotCategory.Viewpoint, 43.4900, 28.5850),
        new("Сините камъни", "Природен парк над Сливен с лифт и гледки към цялата долина.", SpotCategory.Nature, 42.6960, 26.3330),
        new("Ждрелото на Ерма", "Тясно ждрело на река Ерма край Трън, с висящ мост и пътека по скалите.", SpotCategory.Nature, 42.8380, 22.6390),
        new("Тунелите на теснолинейката", "Влакът Септември–Добринище минава през тунели в Родопите — седнете откъм реката.", SpotCategory.RailwayTunnel, 42.0300, 24.0200),
        new("Огняновски минерални извори", "Топли минерални басейни на открито в подножието на Пирин.", SpotCategory.Spring, 41.6180, 23.7790, SpotModerationStatus.Pending),
        new("Къмпинг на язовир Батак", "Поляни за палатки на брега на язовира, с много сенки и гледка към водата.", SpotCategory.CampingSpot, 41.9670, 24.2010, SpotModerationStatus.Pending),
        new("Кафене в Стария Пловдив", "Малко кафене с двор между възрожденските къщи — опитайте домашната баклава.", SpotCategory.Cafe, 42.1480, 24.7520, SpotModerationStatus.Pending),
        new("тест", "аааа", SpotCategory.Other, 42.6977, 23.3219, SpotModerationStatus.Rejected,
            "Спам или несъществуващо място"),
        new("Седемте езера 2", "Същото като Седемте рилски езера.", SpotCategory.BeachOrLake, 42.2040, 23.3200, SpotModerationStatus.Rejected,
            "Дублира съществуващо място"),
    ];

    public static readonly string[] Comments =
    [
        "Бяхме миналата седмица — гледката си заслужава всяка крачка!",
        "Пътят до там е доста разбит, внимавайте с ниски коли.",
        "Препоръчвам да отидете рано сутрин, преди да стане претъпкано.",
        "Едно от най-красивите места, на които съм бил в България.",
        "Има паркинг наблизо, но в събота се пълни бързо.",
        "Вземете си удобни обувки, последната част е стръмна.",
        "Прекрасно място за пикник с децата.",
        "Снимките не могат да предадат колко е впечатляващо на живо.",
        "Има малко кафене наблизо, работи до 18 ч.",
        "Ходихме през есента — цветовете бяха невероятни.",
    ];
}
