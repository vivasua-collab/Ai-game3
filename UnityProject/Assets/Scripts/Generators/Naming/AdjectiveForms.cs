// ============================================================================
// AdjectiveForms.cs — Формы прилагательных по родам
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Источник: docs/examples/NameGenerator_Russian.md
//
// Согласование прилагательных:
// | Род | Основа | Пример (Пыла-) |
// |-----|--------|----------------|
// | Мужской | -ый, -ий | Пылающий |
// | Женский | -ая, -яя | Пылающая |
// | Средний | -ое, -ее | Пылающее |
// | Множественное | -ые, -ие | Пылающие |
// ============================================================================

using System;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Прилагательное с формами для всех грамматических родов.
    /// Позволяет автоматически согласовывать прилагательное с существительным.
    /// 
    /// Пример использования:
    /// <code>
    /// var burning = new AdjectiveForms("Пылающий", "Пылающая", "Пылающее", "Пылающие");
    /// 
    /// string swordName = burning.GetForm(GrammaticalGender.Masculine); // "Пылающий"
    /// string axeName = burning.GetForm(GrammaticalGender.Feminine);    // "Пылающая"
    /// string spearName = burning.GetForm(GrammaticalGender.Neuter);    // "Пылающее"
    /// string glovesName = burning.GetForm(GrammaticalGender.Plural);   // "Пылающие"
    /// </code>
    /// </summary>
    [Serializable]
    public struct AdjectiveForms
    {
        /// <summary>
        /// Мужской род (Пылающий)
        /// </summary>
        public string masculine;
        
        /// <summary>
        /// Женский род (Пылающая)
        /// </summary>
        public string feminine;
        
        /// <summary>
        /// Средний род (Пылающее)
        /// </summary>
        public string neuter;
        
        /// <summary>
        /// Множественное число (Пылающие)
        /// </summary>
        public string plural;

        /// <summary>
        /// Конструктор с явным указанием всех форм
        /// </summary>
        public AdjectiveForms(string masculine, string feminine, string neuter, string plural)
        {
            this.masculine = masculine;
            this.feminine = feminine;
            this.neuter = neuter;
            this.plural = plural;
        }

        /// <summary>
        /// Конструктор с автоматическим образованием форм (для простых случаев)
        /// Работает только для прилагательных с окончаниями -ый/-ий
        /// 
        /// FIX GEN-M01: WARNING — This auto-derivation is INCORRECT for many Russian adjectives (2026-04-11).
        /// Russian adjective declension is far more complex than simple suffix substitution:
        /// - Stem consonant mutations (г→ж, к→ч, х→ш): дорогий → дорогая (NOT дорогоя)
        /// - Mixed declension patterns (горячий → горячая, горячее, горячие)
        /// - Possessive adjectives (лисий → лисья, лисье, лисьи)
        /// - Short form adjectives don't follow this pattern at all
        /// - The -ой ending rules are oversimplified
        /// 
        /// For production use, prefer the full constructor with explicit forms.
        /// This auto-derivation should only be used for simple -ый/-ий adjectives
        /// where no stem mutation occurs.
        /// </summary>
        /// <param name="masculineForm">Мужская форма (например "Пылающий")</param>
        public AdjectiveForms(string masculineForm)
        {
            masculine = masculineForm;
            
            // Автоматическое образование форм
            // -ый → -ая, -ое, -ые
            // -ий → -яя, -ее, -ие
            if (masculineForm.EndsWith("ый"))
            {
                string stem = masculineForm.Substring(0, masculineForm.Length - 2);
                feminine = stem + "ая";
                neuter = stem + "ое";
                plural = stem + "ые";
            }
            else if (masculineForm.EndsWith("ий"))
            {
                string stem = masculineForm.Substring(0, masculineForm.Length - 2);
                feminine = stem + "яя";
                neuter = stem + "ее";
                plural = stem + "ие";
            }
            else if (masculineForm.EndsWith("ой"))
            {
                // Особый случай: -ой → -ая, -ое, -ые
                string stem = masculineForm.Substring(0, masculineForm.Length - 2);
                feminine = stem + "ая";
                neuter = stem + "ое";
                plural = stem + "ые";
            }
            else
            {
                // Если не удаётся определить паттерн, используем мужскую форму везде
                feminine = masculineForm;
                neuter = masculineForm;
                plural = masculineForm;
            }
        }

        /// <summary>
        /// Получить форму прилагательного для указанного рода
        /// </summary>
        /// <param name="gender">Грамматический род</param>
        /// <returns>Соответствующая форма прилагательного</returns>
        public string GetForm(GrammaticalGender gender)
        {
            return gender switch
            {
                GrammaticalGender.Masculine => masculine,
                GrammaticalGender.Feminine => feminine,
                GrammaticalGender.Neuter => neuter,
                GrammaticalGender.Plural => plural,
                _ => masculine
            };
        }

        /// <summary>
        /// Неявное преобразование из строки (мужская форма)
        /// </summary>
        public static implicit operator AdjectiveForms(string masculineForm)
        {
            return new AdjectiveForms(masculineForm);
        }

        public override string ToString() => $"{masculine}/{feminine}/{neuter}/{plural}";
    }
}
