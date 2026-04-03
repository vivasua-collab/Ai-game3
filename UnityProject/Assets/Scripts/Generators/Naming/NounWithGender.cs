// ============================================================================
// NounWithGender.cs — Существительное с грамматическим родом
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Источник: docs/examples/NameGenerator_Russian.md
// ============================================================================

using System;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Существительное с указанием грамматического рода.
    /// Используется для корректного согласования с прилагательными.
    /// 
    /// Примеры:
    /// <code>
    /// var sword = new NounWithGender("меч", GrammaticalGender.Masculine);
    /// var axe = new NounWithGender("секира", GrammaticalGender.Feminine);
    /// var spear = new NounWithGender("копьё", GrammaticalGender.Neuter);
    /// var gloves = new NounWithGender("перчатки", GrammaticalGender.Plural);
    /// </code>
    /// </summary>
    [Serializable]
    public struct NounWithGender
    {
        /// <summary>
        /// Существительное в именительном падеже
        /// </summary>
        public string noun;
        
        /// <summary>
        /// Грамматический род
        /// </summary>
        public GrammaticalGender gender;

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="noun">Существительное в именительном падеже</param>
        /// <param name="gender">Грамматический род</param>
        public NounWithGender(string noun, GrammaticalGender gender)
        {
            this.noun = noun;
            this.gender = gender;
        }

        /// <summary>
        /// Получить согласованную форму прилагательного для этого существительного
        /// </summary>
        /// <param name="adjective">Формы прилагательного</param>
        /// <returns>Форма прилагательного, согласованная с родом существительного</returns>
        public string GetAgreedAdjective(AdjectiveForms adjective)
        {
            return adjective.GetForm(gender);
        }

        /// <summary>
        /// Построить полное название: [прилагательное] [существительное]
        /// </summary>
        /// <param name="adjective">Формы прилагательного</param>
        /// <returns>Полное название</returns>
        public string BuildName(AdjectiveForms adjective)
        {
            return $"{adjective.GetForm(gender)} {noun}";
        }

        /// <summary>
        /// Построить полное название с несколькими прилагательными
        /// </summary>
        /// <param name="adjectives">Массив прилагательных</param>
        /// <returns>Полное название</returns>
        public string BuildName(params AdjectiveForms[] adjectives)
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (var adj in adjectives)
            {
                parts.Add(adj.GetForm(gender));
            }
            parts.Add(noun);
            return string.Join(" ", parts);
        }

        /// <summary>
        /// Неявное преобразование в строку (возвращает существительное)
        /// </summary>
        public static implicit operator string(NounWithGender n) => n.noun;

        /// <summary>
        /// Неявное преобразование из кортежа
        /// </summary>
        public static implicit operator NounWithGender((string noun, GrammaticalGender gender) t)
        {
            return new NounWithGender(t.noun, t.gender);
        }

        public override string ToString() => $"{noun} ({gender})";
    }
}
