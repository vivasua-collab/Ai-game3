# План: Внедрение грамматического согласования в генераторы

**Дата:** 2025-01-12
**Статус:** Планирование
**Связанные документы:**
- docs/GENERATORS_NAME_FIX.md
- docs/examples/NameGenerator_Russian.md

---

## 📋 ЭТАПЫ реализации

### ЭТАП 1: Создание инфраструктуры (1-2 часа)

#### 1.1 Новые файлы

| Файл | Описание | Приоритет |
|------|----------|-----------|
| `Scripts/Generators/Naming/GrammaticalGender.cs` | Enum родов | Высокий |
| `Scripts/Generators/Naming/AdjectiveForms.cs` | Формы прилагательных | Высокий |
| `Scripts/Generators/Naming/NounWithGender.cs` | Структура существительного | Высокий |
| `Scripts/Generators/Naming/NamingDatabase.cs` | База данных | Высокий |
| `Scripts/Generators/Naming/NameBuilder.cs` | Утилита генерации | Средний |

#### 1.2 Структура папок

```
Scripts/Generators/
├── Naming/                          # НОВАЯ ПАПКА
│   ├── GrammaticalGender.cs         # Enum
│   ├── AdjectiveForms.cs            # Struct
│   ├── NounWithGender.cs            # Struct
│   ├── NamingDatabase.cs            # Static class с данными
│   └── NameBuilder.cs               # Utility class
├── WeaponGenerator.cs               # ИЗМЕНИТЬ
├── ArmorGenerator.cs                # ИЗМЕНИТЬ
├── TechniqueGenerator.cs            # ИЗМЕНИТЬ
└── ... другие генераторы
```

---

### ЭТАП 2: Модификация WeaponGenerator (30-45 мин)

#### 2.1 Изменения в данных

**Заменить:**
```csharp
// СТАРОЕ (строки 257-271)
private static readonly Dictionary<WeaponSubtype, string[]> WeaponNamesBySubtype = ...

// НОВОЕ
private static readonly Dictionary<WeaponSubtype, NounWithGender[]> WeaponNamesBySubtype = new()
{
    { WeaponSubtype.Sword, new[] {
        new NounWithGender("меч", GrammaticalGender.Masculine),
        new NounWithGender("клинок", GrammaticalGender.Masculine),
        new NounWithGender("катана", GrammaticalGender.Feminine)
    }},
    { WeaponSubtype.Axe, new[] {
        new NounWithGender("топор", GrammaticalGender.Masculine),
        new NounWithGender("секира", GrammaticalGender.Feminine)
    }},
    { WeaponSubtype.Spear, new[] {
        new NounWithGender("копьё", GrammaticalGender.Neuter),
        new NounWithGender("алебарда", GrammaticalGender.Feminine)
    }},
    // ... все подтипы
};
```

#### 2.2 Изменения в GenerateName()

**Заменить функцию (строки 543-564):**
```csharp
// СТАРОЕ
private static string GenerateName(GeneratedWeapon weapon, SeededRandom rng)
{
    string gradePrefix = weapon.grade switch
    {
        EquipmentGrade.Damaged => "Сломанный ",
        ...
    };
    return $"{gradePrefix}{materialName} {baseName}";
}

// НОВОЕ
private static string GenerateName(GeneratedWeapon weapon, SeededRandom rng)
{
    // Получаем базовое название с родом
    var nounData = rng.NextElement(WeaponNamesBySubtype[weapon.subtype]);
    string baseName = nounData.noun;
    GrammaticalGender gender = nounData.gender;
    
    // Строим название
    var sb = new System.Text.StringBuilder();
    
    // Префикс грейда (согласованный)
    if (weapon.grade != EquipmentGrade.Common)
    {
        var gradeAdj = NamingDatabase.GradeAdjectives[weapon.grade];
        sb.Append(gradeAdj.GetForm(gender));
        sb.Append(" ");
    }
    
    // Материал (обычно как существительное, не согласуется)
    sb.Append(weapon.materialId);
    sb.Append(" ");
    
    // Базовое название
    sb.Append(baseName);
    
    return sb.ToString();
}
```

---

### ЭТАП 3: Модификация ArmorGenerator (30-45 мин)

#### 3.1 Изменения в данных

**Заменить ArmorNamesBySubtype (строки 213-222):**
```csharp
private static readonly Dictionary<ArmorSubtype, NounWithGender[]> ArmorNamesBySubtype = new()
{
    { ArmorSubtype.Head, new[] {
        new NounWithGender("шлем", GrammaticalGender.Masculine),
        new NounWithGender("корона", GrammaticalGender.Feminine),
        new NounWithGender("капюшон", GrammaticalGender.Masculine),
        new NounWithGender("диадема", GrammaticalGender.Feminine)
    }},
    { ArmorSubtype.Torso, new[] {
        new NounWithGender("нагрудник", GrammaticalGender.Masculine),
        new NounWithGender("кираса", GrammaticalGender.Feminine),
        new NounWithGender("кольчуга", GrammaticalGender.Feminine),
        new NounWithGender("мантия", GrammaticalGender.Feminine)
    }},
    { ArmorSubtype.Hands, new[] {
        new NounWithGender("перчатки", GrammaticalGender.Plural),
        new NounWithGender("рукавицы", GrammaticalGender.Plural)
    }},
    { ArmorSubtype.Feet, new[] {
        new NounWithGender("сапоги", GrammaticalGender.Plural),
        new NounWithGender("ботинки", GrammaticalGender.Plural)
    }},
    // ... остальные
};
```

#### 3.2 Изменения в GenerateName()

**Заменить функцию (строки 500-529):**
```csharp
private static string GenerateName(GeneratedArmor armor, SeededRandom rng)
{
    var nounData = rng.NextElement(ArmorNamesBySubtype[armor.subtype]);
    string baseName = nounData.noun;
    GrammaticalGender gender = nounData.gender;
    
    var sb = new System.Text.StringBuilder();
    
    // Префикс грейда
    if (armor.grade != EquipmentGrade.Common)
    {
        var gradeAdj = NamingDatabase.GradeAdjectives[armor.grade];
        sb.Append(gradeAdj.GetForm(gender));
        sb.Append(" ");
    }
    
    // Префикс весового класса
    if (armor.weightClass != ArmorWeightClass.Medium)
    {
        var weightAdj = NamingDatabase.WeightAdjectives[armor.weightClass];
        sb.Append(weightAdj.GetForm(gender));
        sb.Append(" ");
    }
    
    sb.Append(armor.materialId);
    sb.Append(" ");
    sb.Append(baseName);
    
    return sb.ToString();
}
```

---

### ЭТАП 4: Модификация TechniqueGenerator (30-45 мин)

#### 4.1 Изменения в данных

**Заменить TechniqueNames (строки 182-194):**
```csharp
private static readonly Dictionary<TechniqueType, NounWithGender[]> TechniqueNames = new()
{
    { TechniqueType.Combat, new[] {
        new NounWithGender("удар", GrammaticalGender.Masculine),
        new NounWithGender("атака", GrammaticalGender.Feminine)
    }},
    { TechniqueType.Defense, new[] {
        new NounWithGender("защита", GrammaticalGender.Feminine),
        new NounWithGender("блок", GrammaticalGender.Masculine),
        new NounWithGender("щит", GrammaticalGender.Masculine),
        new NounWithGender("стена", GrammaticalGender.Feminine)
    }},
    { TechniqueType.Healing, new[] {
        new NounWithGender("исцеление", GrammaticalGender.Neuter),
        new NounWithGender("восстановление", GrammaticalGender.Neuter),
        new NounWithGender("лечение", GrammaticalGender.Neuter)
    }},
    // ... остальные
};
```

**Заменить ElementPrefixes (строки 197-206):**
```csharp
// Использовать NamingDatabase.ElementAdjectives
```

#### 4.2 Изменения в Generate()

**Изменить генерацию имени (строки 297-308):**
```csharp
// Получаем название техники с родом
var nounData = GetTechniqueNameData(technique.type, rng);
string baseName = nounData.noun;
GrammaticalGender gender = nounData.gender;

// Элементный префикс (согласованный)
if (technique.element != Element.Neutral)
{
    var elementAdj = NamingDatabase.ElementAdjectives[technique.element];
    technique.nameRu = $"{elementAdj.GetForm(gender)} {baseName}";
}
else
{
    technique.nameRu = baseName;
}
```

---

### ЭТАП 5: Тестирование (30 мин)

#### 5.1 Создать тесты

**Файл:** `Scripts/Tests/GeneratorNameTests.cs`

```csharp
// Тесты для проверки грамматического согласования
[Test]
public void WeaponGenerator_FeminineNoun_GetsFeminineAdjective()
{
    // Arrange
    var parameters = new WeaponGenerationParams {
        subtype = WeaponSubtype.Axe,
        grade = EquipmentGrade.Refined
    };
    
    // Act
    var weapon = WeaponGenerator.Generate(parameters, new SeededRandom(12345));
    
    // Assert - если название "секира", должно быть "Улучшенная секира"
    Assert.IsFalse(weapon.nameRu.Contains("Улучшенный секира"));
}
```

#### 5.2 Запустить примеры

```csharp
// В редакторе Unity
Debug.Log(WeaponGenerator.GenerateExamples());
Debug.Log(ArmorGenerator.GenerateExamples());
Debug.Log(TechniqueGenerator.GenerateExamples());
```

---

## ⏱️ Оценка времени

| Этап | Время |
|------|-------|
| ЭТАП 1: Инфраструктура | 1-2 часа |
| ЭТАП 2: WeaponGenerator | 30-45 мин |
| ЭТАП 3: ArmorGenerator | 30-45 мин |
| ЭТАП 4: TechniqueGenerator | 30-45 мин |
| ЭТАП 5: Тестирование | 30 мин |
| **ИТОГО** | **3-4.5 часа** |

---

## 📊 Прогресс

- [ ] ЭТАП 1: Создать Naming/ папку и базовые классы
- [ ] ЭТАП 2: Модифицировать WeaponGenerator
- [ ] ЭТАП 3: Модифицировать ArmorGenerator
- [ ] ЭТАП 4: Модифицировать TechniqueGenerator
- [ ] ЭТАП 5: Написать тесты и проверить

---

## 📝 Заметки

1. **Обратная совместимость:** Старые названия сохранятся в сохранениях
2. **Локализация:** Система подготовлена для будущей локализации
3. **Расширяемость:** Легко добавлять новые модификаторы

---

*План создан: 2025-01-12*
