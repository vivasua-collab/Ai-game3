// ============================================================================
// NameBuilder.cs — Утилита для построения названий
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Источник: docs/examples/NameGenerator_Russian.md
//
// Утилита для построения согласованных названий на русском языке.
// Поддерживает цепочку модификаторов, которые автоматически согласуются.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using CultivationGame.Core;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Утилита для построения названий с грамматическим согласованием.
    /// 
    /// Пример использования:
    /// <code>
    /// var builder = new NameBuilder();
    /// 
    /// // Оружие: "Улучшенная секира"
    /// builder.Reset()
    ///     .WithGrade(EquipmentGrade.Refined)
    ///     .WithNoun(new NounWithGender("секира", GrammaticalGender.Feminine));
    /// string name = builder.Build(); // "Улучшенная секира"
    /// 
    /// // Броня: "Тяжёлая Улучшенная мантия"
    /// builder.Reset()
    ///     .WithGrade(EquipmentGrade.Refined)
    ///     .WithWeightClass(ArmorWeightClass.Heavy)
    ///     .WithNoun(new NounWithGender("мантия", GrammaticalGender.Feminine));
    /// string name = builder.Build(); // "Тяжёлая Улучшенная мантия"
    /// 
    /// // Техника: "Огненное копьё" (element + noun)
    /// builder.Reset()
    ///     .WithElement(Element.Fire)
    ///     .WithNoun(new NounWithGender("копьё", GrammaticalGender.Neuter));
    /// string name = builder.Build(); // "Огненное копьё"
    /// </code>
    /// </summary>
    public class NameBuilder
    {
        private readonly List<AdjectiveForms> _adjectives = new List<AdjectiveForms>();
        private string _noun = "";
        private GrammaticalGender _gender = GrammaticalGender.Masculine;
        private string _material = "";
        private string _suffix = "";

        /// <summary>
        /// Сбросить builder для нового названия
        /// </summary>
        public NameBuilder Reset()
        {
            _adjectives.Clear();
            _noun = "";
            _gender = GrammaticalGender.Masculine;
            _material = "";
            _suffix = "";
            return this;
        }

        /// <summary>
        /// Установить базовое существительное
        /// </summary>
        public NameBuilder WithNoun(NounWithGender noun)
        {
            _noun = noun.noun;
            _gender = noun.gender;
            return this;
        }

        /// <summary>
        /// Установить базовое существительное (строка + род)
        /// </summary>
        public NameBuilder WithNoun(string noun, GrammaticalGender gender)
        {
            _noun = noun;
            _gender = gender;
            return this;
        }

        /// <summary>
        /// Добавить грейд экипировки
        /// </summary>
        public NameBuilder WithGrade(EquipmentGrade grade)
        {
            if (grade != EquipmentGrade.Common && NamingDatabase.GradeAdjectives.TryGetValue(grade, out var adj))
            {
                _adjectives.Add(adj);
            }
            return this;
        }

        /// <summary>
        /// Добавить грейд техники
        /// </summary>
        public NameBuilder WithTechniqueGrade(TechniqueGrade grade)
        {
            if (grade != TechniqueGrade.Common && NamingDatabase.TechniqueGradeAdjectives.TryGetValue(grade, out var adj))
            {
                _adjectives.Add(adj);
            }
            return this;
        }

        /// <summary>
        /// Добавить весовой класс брони
        /// </summary>
        public NameBuilder WithWeightClass(ArmorWeightClass weightClass)
        {
            if (weightClass != ArmorWeightClass.Medium && NamingDatabase.WeightAdjectives.TryGetValue(weightClass, out var adj))
            {
                _adjectives.Add(adj);
            }
            return this;
        }

        /// <summary>
        /// Добавить элемент (стихию)
        /// </summary>
        public NameBuilder WithElement(Element element)
        {
            if (element != Element.Neutral && NamingDatabase.ElementAdjectives.TryGetValue(element, out var adj))
            {
                _adjectives.Add(adj);
            }
            return this;
        }

        /// <summary>
        /// Добавить произвольное прилагательное
        /// </summary>
        public NameBuilder WithAdjective(AdjectiveForms adjective)
        {
            _adjectives.Add(adjective);
            return this;
        }

        /// <summary>
        /// Добавить произвольное прилагательное (строка)
        /// </summary>
        public NameBuilder WithAdjective(string masculineForm)
        {
            _adjectives.Add(new AdjectiveForms(masculineForm));
            return this;
        }

        /// <summary>
        /// Установить материал
        /// </summary>
        public NameBuilder WithMaterial(string material)
        {
            _material = material;
            return this;
        }

        /// <summary>
        /// Установить суффикс (добавляется в конце)
        /// </summary>
        public NameBuilder WithSuffix(string suffix)
        {
            _suffix = suffix;
            return this;
        }

        /// <summary>
        /// Построить итоговое название
        /// </summary>
        public string Build()
        {
            var sb = new StringBuilder();

            // Прилагательные (согласованные)
            foreach (var adj in _adjectives)
            {
                string form = adj.GetForm(_gender);
                if (!string.IsNullOrEmpty(form))
                {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append(form);
                }
            }

            // Материал (обычно не согласуется, идёт как существительное)
            if (!string.IsNullOrEmpty(_material))
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(_material);
            }

            // Базовое существительное
            if (!string.IsNullOrEmpty(_noun))
            {
                if (sb.Length > 0) sb.Append(" ");
                sb.Append(_noun);
            }

            // Суффикс
            if (!string.IsNullOrEmpty(_suffix))
            {
                sb.Append(" ");
                sb.Append(_suffix);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Построить название и сбросить builder
        /// </summary>
        public string BuildAndReset()
        {
            string result = Build();
            Reset();
            return result;
        }

        /// <summary>
        /// Получить текущий грамматический род
        /// </summary>
        public GrammaticalGender GetGender() => _gender;
    }

    /// <summary>
    /// Статические методы для быстрой генерации названий
    /// </summary>
    public static class NameGenerator
    {
        private static readonly NameBuilder _builder = new NameBuilder();

        /// <summary>
        /// Сгенерировать название оружия
        /// </summary>
        public static string GenerateWeaponName(
            WeaponSubtype subtype,
            EquipmentGrade grade,
            string material,
            SeededRandom rng)
        {
            var noun = NamingDatabase.GetRandomWeaponName(subtype, rng);
            
            return _builder.Reset()
                .WithGrade(grade)
                .WithMaterial(material)
                .WithNoun(noun)
                .Build();
        }

        /// <summary>
        /// Сгенерировать название брони
        /// </summary>
        public static string GenerateArmorName(
            ArmorSubtype subtype,
            ArmorWeightClass weightClass,
            EquipmentGrade grade,
            string material,
            SeededRandom rng)
        {
            var noun = NamingDatabase.GetRandomArmorName(subtype, rng);
            
            return _builder.Reset()
                .WithGrade(grade)
                .WithWeightClass(weightClass)
                .WithMaterial(material)
                .WithNoun(noun)
                .Build();
        }

        /// <summary>
        /// Сгенерировать название техники
        /// </summary>
        public static string GenerateTechniqueName(
            TechniqueType type,
            Element element,
            SeededRandom rng)
        {
            var noun = NamingDatabase.GetRandomTechniqueName(type, rng);
            
            return _builder.Reset()
                .WithElement(element)
                .WithNoun(noun)
                .Build();
        }

        /// <summary>
        /// Сгенерировать название техники с грейдом
        /// </summary>
        public static string GenerateTechniqueName(
            TechniqueType type,
            Element element,
            TechniqueGrade grade,
            SeededRandom rng)
        {
            var noun = NamingDatabase.GetRandomTechniqueName(type, rng);
            
            return _builder.Reset()
                .WithElement(element)
                .WithTechniqueGrade(grade)
                .WithNoun(noun)
                .Build();
        }

        /// <summary>
        /// Вывести примеры генерации для отладки
        /// </summary>
        public static string GenerateExamples()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== ПРИМЕРЫ ГЕНЕРАЦИИ НАЗВАНИЙ ===\n");

            var rng = new SeededRandom(12345);

            sb.AppendLine("--- Оружие ---");
            foreach (WeaponSubtype subtype in Enum.GetValues(typeof(WeaponSubtype)))
            {
                var noun = NamingDatabase.GetRandomWeaponName(subtype, rng);
                
                // Common
                sb.AppendLine($"Common: {_builder.Reset().WithNoun(noun).WithMaterial("Железо").Build()}");
                
                // Refined
                sb.AppendLine($"Refined: {_builder.Reset().WithGrade(EquipmentGrade.Refined).WithNoun(noun).WithMaterial("Сталь").Build()}");
                
                // Perfect
                sb.AppendLine($"Perfect: {_builder.Reset().WithGrade(EquipmentGrade.Perfect).WithNoun(noun).WithMaterial("Духовное железо").Build()}");
                
                sb.AppendLine();
            }

            sb.AppendLine("--- Броня ---");
            foreach (ArmorSubtype subtype in Enum.GetValues(typeof(ArmorSubtype)))
            {
                var noun = NamingDatabase.GetRandomArmorName(subtype, rng);
                
                // Light Refined
                sb.AppendLine($"Light Refined: {_builder.Reset().WithGrade(EquipmentGrade.Refined).WithWeightClass(ArmorWeightClass.Light).WithNoun(noun).Build()}");
                
                // Heavy Perfect
                sb.AppendLine($"Heavy Perfect: {_builder.Reset().WithGrade(EquipmentGrade.Perfect).WithWeightClass(ArmorWeightClass.Heavy).WithNoun(noun).Build()}");
                
                sb.AppendLine();
            }

            sb.AppendLine("--- Техники ---");
            foreach (TechniqueType type in Enum.GetValues(typeof(TechniqueType)))
            {
                var noun = NamingDatabase.GetRandomTechniqueName(type, rng);
                
                // Fire
                sb.AppendLine($"Fire: {_builder.Reset().WithElement(Element.Fire).WithNoun(noun).Build()}");
                
                // Lightning
                sb.AppendLine($"Lightning: {_builder.Reset().WithElement(Element.Lightning).WithNoun(noun).Build()}");
                
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
