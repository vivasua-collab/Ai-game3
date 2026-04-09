// ============================================================================
// TileEnums.cs — Перечисления для тайловой системы
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// ============================================================================

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Тип поверхности тайла.
    /// Влияет на проходимость и стоимость движения.
    /// </summary>
    public enum TerrainType
    {
        None = 0,           // Пустота (нет тайла)
        Grass = 1,          // Трава (базовая проходимость)
        Dirt = 2,           // Земля (базовая проходимость)
        Stone = 3,          // Камень (базовая проходимость)
        Water_Shallow = 4,  // Мелкая вода (замедление)
        Water_Deep = 5,     // Глубокая вода (требует навык)
        Sand = 6,           // Песок (небольшое замедление)
        Snow = 7,           // Снег (замедление)
        Ice = 8,            // Лёд (скольжение)
        Lava = 9,           // Лава (урон)
        Void = 10           // Пустота (непроходимо)
    }

    /// <summary>
    /// Категория объекта на тайле.
    /// </summary>
    public enum TileObjectCategory
    {
        None = 0,           // Нет объекта
        Vegetation = 1,     // Растительность (деревья, кусты)
        Rock = 2,           // Камни
        Water = 3,          // Водные объекты
        Building = 4,       // Здания и сооружения
        Furniture = 5,      // Мебель (интерьер)
        Interactive = 6,    // Интерактивные объекты
        Decoration = 7      // Декорации
    }

    /// <summary>
    /// Тип объекта на тайле.
    /// </summary>
    public enum TileObjectType
    {
        None = 0,
        
        // Vegetation
        Tree_Oak = 100,
        Tree_Pine = 101,
        Tree_Birch = 102,
        Bush = 110,
        Bush_Berry = 111,
        Grass_Tall = 120,
        Flower = 121,
        
        // Rocks
        Rock_Small = 200,
        Rock_Medium = 201,
        Rock_Large = 202,
        Boulder = 210,
        
        // Water
        Pond = 300,
        Well = 310,
        
        // Buildings (базовые)
        Wall_Wood = 400,
        Wall_Stone = 401,
        Door = 410,
        Window = 411,
        
        // Interactive
        Chest = 500,
        Shrine = 510,
        Altar = 511,
        OreVein = 520,
        Herb = 530
    }

    /// <summary>
    /// Флаги свойств тайла.
    /// Переименовано в GameTileFlags для избежания конфликта с UnityEngine.Tilemaps.TileFlags.
    /// Редактировано: 2026-04-09 07:10:00 UTC
    /// </summary>
    [System.Flags]
    public enum GameTileFlags
    {
        None = 0,
        Passable = 1 << 0,          // Можно пройти
        Swimable = 1 << 1,          // Можно плыть
        Flyable = 1 << 2,           // Можно летать над
        BlocksVision = 1 << 3,      // Блокирует видимость
        ProvidesCover = 1 << 4,     // Даёт укрытие
        Interactable = 1 << 5,      // Можно взаимодействовать
        Harvestable = 1 << 6,       // Можно собирать ресурсы
        Dangerous = 1 << 7          // Опасно (урон)
    }
}
