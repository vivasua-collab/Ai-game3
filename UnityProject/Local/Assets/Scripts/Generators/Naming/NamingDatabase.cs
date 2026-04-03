// ============================================================================
// NamingDatabase.cs — База данных названий с грамматическим согласованием
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Источник: docs/GENERATORS_NAME_FIX.md
//
// Содержит все названия и прилагательные с правильным согласованием родов.
// Примеры:
// ❌ "Улучшенный секира" (мужской + женский)
// ✅ "Улучшенная секира" (женский + женский)
// ============================================================================

using System;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Статическая база данных названий с грамматическим согласованием.
    /// Содержит:
    /// - Названия оружия с родами
    /// - Названия брони с родами
    /// - Названия техник с родами
    /// - Прилагательные для грейдов
    /// - Прилагательные для весовых классов брони
    /// - Прилагательные для элементов
    /// </summary>
    public static class NamingDatabase
    {
        #region Weapon Names

        /// <summary>
        /// Названия оружия по подтипам с указанием грамматического рода
        /// </summary>
        public static readonly Dictionary<WeaponSubtype, NounWithGender[]> WeaponNames = new()
        {
            { WeaponSubtype.Unarmed, new[] {
                new NounWithGender("кастеты", GrammaticalGender.Plural),
                new NounWithGender("когти", GrammaticalGender.Plural),
                new NounWithGender("перчатки", GrammaticalGender.Plural)
            }},
            { WeaponSubtype.Dagger, new[] {
                new NounWithGender("кинжал", GrammaticalGender.Masculine),
                new NounWithGender("короткий меч", GrammaticalGender.Masculine),
                new NounWithGender("стилет", GrammaticalGender.Masculine)
            }},
            { WeaponSubtype.Sword, new[] {
                new NounWithGender("меч", GrammaticalGender.Masculine),
                new NounWithGender("клинок", GrammaticalGender.Masculine),
                new NounWithGender("катана", GrammaticalGender.Feminine)
            }},
            { WeaponSubtype.Greatsword, new[] {
                new NounWithGender("двуручный меч", GrammaticalGender.Masculine),
                new NounWithGender("палаш", GrammaticalGender.Masculine),
                new NounWithGender("клеймор", GrammaticalGender.Masculine)
            }},
            { WeaponSubtype.Axe, new[] {
                new NounWithGender("топор", GrammaticalGender.Masculine),
                new NounWithGender("секира", GrammaticalGender.Feminine),
                new NounWithGender("боевой топор", GrammaticalGender.Masculine)
            }},
            { WeaponSubtype.Spear, new[] {
                new NounWithGender("копьё", GrammaticalGender.Neuter),
                new NounWithGender("алебарда", GrammaticalGender.Feminine),
                new NounWithGender("глефа", GrammaticalGender.Feminine)
            }},
            { WeaponSubtype.Bow, new[] {
                new NounWithGender("лук", GrammaticalGender.Masculine),
                new NounWithGender("длинный лук", GrammaticalGender.Masculine),
                new NounWithGender("короткий лук", GrammaticalGender.Masculine)
            }},
            { WeaponSubtype.Staff, new[] {
                new NounWithGender("посох", GrammaticalGender.Masculine),
                new NounWithGender("жезл", GrammaticalGender.Masculine),
                new NounWithGender("скипетр", GrammaticalGender.Masculine)
            }},
            { WeaponSubtype.Hammer, new[] {
                new NounWithGender("молот", GrammaticalGender.Masculine),
                new NounWithGender("боевой молот", GrammaticalGender.Masculine),
                new NounWithGender("кувалда", GrammaticalGender.Feminine)
            }},
            { WeaponSubtype.Mace, new[] {
                new NounWithGender("булава", GrammaticalGender.Feminine),
                new NounWithGender("палица", GrammaticalGender.Feminine),
                new NounWithGender("моргенштерн", GrammaticalGender.Masculine)
            }},
            { WeaponSubtype.Crossbow, new[] {
                new NounWithGender("арбалет", GrammaticalGender.Masculine),
                new NounWithGender("тяжёлый арбалет", GrammaticalGender.Masculine)
            }},
            { WeaponSubtype.Wand, new[] {
                new NounWithGender("жезл", GrammaticalGender.Masculine),
                new NounWithGender("волшебная палочка", GrammaticalGender.Feminine),
                new NounWithGender("скипетр", GrammaticalGender.Masculine)
            }}
        };

        #endregion

        #region Armor Names

        /// <summary>
        /// Названия брони по подтипам с указанием грамматического рода
        /// </summary>
        public static readonly Dictionary<ArmorSubtype, NounWithGender[]> ArmorNames = new()
        {
            { ArmorSubtype.Head, new[] {
                new NounWithGender("шлем", GrammaticalGender.Masculine),
                new NounWithGender("корона", GrammaticalGender.Feminine),
                new NounWithGender("капюшон", GrammaticalGender.Masculine),
                new NounWithGender("диадема", GrammaticalGender.Feminine),
                new NounWithGender("маска", GrammaticalGender.Feminine)
            }},
            { ArmorSubtype.Torso, new[] {
                new NounWithGender("нагрудник", GrammaticalGender.Masculine),
                new NounWithGender("кираса", GrammaticalGender.Feminine),
                new NounWithGender("кольчуга", GrammaticalGender.Feminine),
                new NounWithGender("мантия", GrammaticalGender.Feminine),
                new NounWithGender("доспех", GrammaticalGender.Masculine)
            }},
            { ArmorSubtype.Arms, new[] {
                new NounWithGender("наручи", GrammaticalGender.Plural),
                new NounWithGender("наплечники", GrammaticalGender.Plural),
                new NounWithGender("рукава", GrammaticalGender.Plural),
                new NounWithGender("браслеты", GrammaticalGender.Plural)
            }},
            { ArmorSubtype.Hands, new[] {
                new NounWithGender("перчатки", GrammaticalGender.Plural),
                new NounWithGender("рукавицы", GrammaticalGender.Plural),
                new NounWithGender("латные перчатки", GrammaticalGender.Plural)
            }},
            { ArmorSubtype.Legs, new[] {
                new NounWithGender("поножи", GrammaticalGender.Plural),
                new NounWithGender("наголенники", GrammaticalGender.Plural),
                new NounWithGender("штаны", GrammaticalGender.Plural),
                new NounWithGender("брюки", GrammaticalGender.Plural)
            }},
            { ArmorSubtype.Feet, new[] {
                new NounWithGender("сабатоны", GrammaticalGender.Plural),
                new NounWithGender("сапоги", GrammaticalGender.Plural),
                new NounWithGender("ботинки", GrammaticalGender.Plural),
                new NounWithGender("туфли", GrammaticalGender.Plural)
            }},
            { ArmorSubtype.Full, new[] {
                new NounWithGender("полный доспех", GrammaticalGender.Masculine),
                new NounWithGender("латы", GrammaticalGender.Plural),
                new NounWithGender("броня", GrammaticalGender.Feminine),
                new NounWithGender("панцирь", GrammaticalGender.Masculine)
            }}
        };

        #endregion

        #region Technique Names

        /// <summary>
        /// Названия техник по типам с указанием грамматического рода
        /// </summary>
        public static readonly Dictionary<TechniqueType, NounWithGender[]> TechniqueNames = new()
        {
            { TechniqueType.Combat, new[] {
                new NounWithGender("удар", GrammaticalGender.Masculine),
                new NounWithGender("атака", GrammaticalGender.Feminine),
                new NounWithGender("стремительный удар", GrammaticalGender.Masculine)
            }},
            { TechniqueType.Defense, new[] {
                new NounWithGender("защита", GrammaticalGender.Feminine),
                new NounWithGender("блок", GrammaticalGender.Masculine),
                new NounWithGender("щит", GrammaticalGender.Masculine),
                new NounWithGender("стена", GrammaticalGender.Feminine),
                new NounWithGender("барьер", GrammaticalGender.Masculine)
            }},
            { TechniqueType.Healing, new[] {
                new NounWithGender("исцеление", GrammaticalGender.Neuter),
                new NounWithGender("восстановление", GrammaticalGender.Neuter),
                new NounWithGender("лечение", GrammaticalGender.Neuter),
                new NounWithGender("регенерация", GrammaticalGender.Feminine)
            }},
            { TechniqueType.Support, new[] {
                new NounWithGender("поддержка", GrammaticalGender.Feminine),
                new NounWithGender("усиление", GrammaticalGender.Neuter),
                new NounWithGender("помощь", GrammaticalGender.Feminine),
                new NounWithGender("благословение", GrammaticalGender.Neuter)
            }},
            { TechniqueType.Movement, new[] {
                new NounWithGender("рывок", GrammaticalGender.Masculine),
                new NounWithGender("прыжок", GrammaticalGender.Masculine),
                new NounWithGender("перемещение", GrammaticalGender.Neuter),
                new NounWithGender("телепорт", GrammaticalGender.Masculine)
            }},
            { TechniqueType.Curse, new[] {
                new NounWithGender("проклятие", GrammaticalGender.Neuter),
                new NounWithGender("порча", GrammaticalGender.Feminine),
                new NounWithGender("сглаз", GrammaticalGender.Masculine),
                new NounWithGender("скверна", GrammaticalGender.Feminine)
            }},
            { TechniqueType.Poison, new[] {
                new NounWithGender("яд", GrammaticalGender.Masculine),
                new NounWithGender("токсин", GrammaticalGender.Masculine),
                new NounWithGender("отрава", GrammaticalGender.Feminine)
            }},
            { TechniqueType.Sensory, new[] {
                new NounWithGender("чувство", GrammaticalGender.Neuter),
                new NounWithGender("восприятие", GrammaticalGender.Neuter),
                new NounWithGender("обнаружение", GrammaticalGender.Neuter),
                new NounWithGender("взгляд", GrammaticalGender.Masculine)
            }},
            { TechniqueType.Formation, new[] {
                new NounWithGender("формация", GrammaticalGender.Feminine),
                new NounWithGender("массив", GrammaticalGender.Masculine),
                new NounWithGender("печать", GrammaticalGender.Feminine),
                new NounWithGender("рунный круг", GrammaticalGender.Masculine)
            }},
            { TechniqueType.Cultivation, new[] {
                new NounWithGender("медитация", GrammaticalGender.Feminine),
                new NounWithGender("практика", GrammaticalGender.Feminine),
                new NounWithGender("сосредоточение", GrammaticalGender.Neuter),
                new NounWithGender("путь", GrammaticalGender.Masculine)
            }}
        };

        #endregion

        #region Grade Adjectives

        /// <summary>
        /// Прилагательные для грейдов экипировки
        /// Источник: EQUIPMENT_SYSTEM.md §2.1
        /// </summary>
        public static readonly Dictionary<EquipmentGrade, AdjectiveForms> GradeAdjectives = new()
        {
            { EquipmentGrade.Damaged, new AdjectiveForms(
                "Сломанный", "Сломанная", "Сломанное", "Сломанные"
            )},
            { EquipmentGrade.Common, new AdjectiveForms(
                "", "", "", ""  // Обычный предмет без префикса
            )},
            { EquipmentGrade.Refined, new AdjectiveForms(
                "Улучшенный", "Улучшенная", "Улучшенное", "Улучшенные"
            )},
            { EquipmentGrade.Perfect, new AdjectiveForms(
                "Совершенный", "Совершенная", "Совершенное", "Совершенные"
            )},
            { EquipmentGrade.Transcendent, new AdjectiveForms(
                "Трансцендентный", "Трансцендентная", "Трансцендентное", "Трансцендентные"
            )}
        };

        /// <summary>
        /// Прилагательные для грейдов техник
        /// Источник: TECHNIQUE_SYSTEM.md §"Система Grade"
        /// </summary>
        public static readonly Dictionary<TechniqueGrade, AdjectiveForms> TechniqueGradeAdjectives = new()
        {
            { TechniqueGrade.Common, new AdjectiveForms("", "", "", "") },
            { TechniqueGrade.Refined, new AdjectiveForms(
                "Очищенный", "Очищенная", "Очищенное", "Очищенные"
            )},
            { TechniqueGrade.Perfect, new AdjectiveForms(
                "Совершенный", "Совершенная", "Совершенное", "Совершенные"
            )},
            { TechniqueGrade.Transcendent, new AdjectiveForms(
                "Трансцендентный", "Трансцендентная", "Трансцендентное", "Трансцендентные"
            )}
        };

        #endregion

        #region Weight Class Adjectives

        /// <summary>
        /// Прилагательные для весовых классов брони
        /// </summary>
        public static readonly Dictionary<ArmorWeightClass, AdjectiveForms> WeightAdjectives = new()
        {
            { ArmorWeightClass.Light, new AdjectiveForms(
                "Лёгкий", "Лёгкая", "Лёгкое", "Лёгкие"
            )},
            { ArmorWeightClass.Medium, new AdjectiveForms(
                "", "", "", ""  // Средний без префикса
            )},
            { ArmorWeightClass.Heavy, new AdjectiveForms(
                "Тяжёлый", "Тяжёлая", "Тяжёлое", "Тяжёлые"
            )}
        };

        #endregion

        #region Element Adjectives

        /// <summary>
        /// Прилагательные для элементов (стихий)
        /// Источник: ELEMENTS_SYSTEM.md
        /// </summary>
        public static readonly Dictionary<Element, AdjectiveForms> ElementAdjectives = new()
        {
            { Element.Neutral, new AdjectiveForms("", "", "", "") },
            { Element.Fire, new AdjectiveForms(
                "Огненный", "Огненная", "Огненное", "Огненные"
            )},
            { Element.Water, new AdjectiveForms(
                "Водяной", "Водяная", "Водяное", "Водяные"
            )},
            { Element.Earth, new AdjectiveForms(
                "Земляной", "Земляная", "Земляное", "Земляные"
            )},
            { Element.Air, new AdjectiveForms(
                "Воздушный", "Воздушная", "Воздушное", "Воздушные"
            )},
            { Element.Lightning, new AdjectiveForms(
                "Громовой", "Громовая", "Громовое", "Громовые"
            )},
            { Element.Void, new AdjectiveForms(
                "Пустотный", "Пустотная", "Пустотное", "Пустотные"
            )},
            { Element.Poison, new AdjectiveForms(
                "Ядовитый", "Ядовитая", "Ядовитое", "Ядовитые"
            )}
        };

        #endregion

        #region Helper Methods

        /// <summary>
        /// Получить случайное название оружия с родом
        /// </summary>
        public static NounWithGender GetRandomWeaponName(WeaponSubtype subtype, SeededRandom rng)
        {
            if (WeaponNames.TryGetValue(subtype, out var names))
                return rng.NextElement(names);
            return new NounWithGender(subtype.ToString(), GrammaticalGender.Masculine);
        }

        /// <summary>
        /// Получить случайное название брони с родом
        /// </summary>
        public static NounWithGender GetRandomArmorName(ArmorSubtype subtype, SeededRandom rng)
        {
            if (ArmorNames.TryGetValue(subtype, out var names))
                return rng.NextElement(names);
            return new NounWithGender(subtype.ToString(), GrammaticalGender.Masculine);
        }

        /// <summary>
        /// Получить случайное название техники с родом
        /// </summary>
        public static NounWithGender GetRandomTechniqueName(TechniqueType type, SeededRandom rng)
        {
            if (TechniqueNames.TryGetValue(type, out var names))
                return rng.NextElement(names);
            return new NounWithGender("техника", GrammaticalGender.Feminine);
        }

        /// <summary>
        /// Проверить корректность данных (для отладки)
        /// </summary>
        public static bool ValidateDatabase()
        {
            bool valid = true;
            
            // Проверяем что все enum'ы покрыты
            foreach (WeaponSubtype subtype in Enum.GetValues(typeof(WeaponSubtype)))
            {
                if (!WeaponNames.ContainsKey(subtype))
                {
                    UnityEngine.Debug.LogWarning($"[NamingDatabase] Missing weapon subtype: {subtype}");
                    valid = false;
                }
            }
            
            foreach (ArmorSubtype subtype in Enum.GetValues(typeof(ArmorSubtype)))
            {
                if (!ArmorNames.ContainsKey(subtype))
                {
                    UnityEngine.Debug.LogWarning($"[NamingDatabase] Missing armor subtype: {subtype}");
                    valid = false;
                }
            }
            
            foreach (TechniqueType type in Enum.GetValues(typeof(TechniqueType)))
            {
                if (!TechniqueNames.ContainsKey(type))
                {
                    UnityEngine.Debug.LogWarning($"[NamingDatabase] Missing technique type: {type}");
                    valid = false;
                }
            }
            
            return valid;
        }

        #endregion
    }
}
