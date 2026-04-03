// ============================================================================
// SeededRandom.cs — Детерминированный генератор случайных чисел
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-03-31 10:22:36 UTC
// ============================================================================
//
// Источник: docs/GENERATORS_SYSTEM.md
//
// Детерминированная генерация на основе seed.
// Одинаковый seed всегда даёт одинаковую последовательность.
// ============================================================================

using System;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Детерминированный генератор случайных чисел на основе seed.
    /// Одинаковый seed всегда даёт одинаковую последовательность.
    /// </summary>
    public class SeededRandom
    {
        private Random random;
        private int seed;
        private int steps;

        public int Seed => seed;
        public int Steps => steps;

        /// <summary>
        /// Создать генератор с указанным seed
        /// </summary>
        public SeededRandom(int seed)
        {
            this.seed = seed;
            this.random = new Random(seed);
            this.steps = 0;
        }

        /// <summary>
        /// Создать генератор со случайным seed
        /// </summary>
        public SeededRandom()
        {
            this.seed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            this.random = new Random(seed);
            this.steps = 0;
        }

        /// <summary>
        /// Сбросить генератор на начальное состояние
        /// </summary>
        public void Reset(int? newSeed = null)
        {
            seed = newSeed ?? seed;
            random = new Random(seed);
            steps = 0;
        }

        /// <summary>
        /// Следующее случайное число [0, int.MaxValue)
        /// </summary>
        public int Next()
        {
            steps++;
            return random.Next();
        }

        /// <summary>
        /// Случайное число [0, max)
        /// </summary>
        public int Next(int max)
        {
            steps++;
            return random.Next(max);
        }

        /// <summary>
        /// Случайное число [min, max)
        /// </summary>
        public int Next(int min, int max)
        {
            steps++;
            return random.Next(min, max);
        }

        /// <summary>
        /// Случайное число [0.0, 1.0)
        /// </summary>
        public float NextFloat()
        {
            steps++;
            return (float)random.NextDouble();
        }

        /// <summary>
        /// Случайное число [min, max)
        /// </summary>
        public float NextFloat(float min, float max)
        {
            steps++;
            return min + (float)random.NextDouble() * (max - min);
        }

        /// <summary>
        /// Случайный элемент массива
        /// </summary>
        public T NextElement<T>(T[] array)
        {
            if (array == null || array.Length == 0)
                return default;
            return array[Next(array.Length)];
        }

        /// <summary>
        /// Случайный элемент списка
        /// </summary>
        public T NextElement<T>(System.Collections.Generic.List<T> list)
        {
            if (list == null || list.Count == 0)
                return default;
            return list[Next(list.Count)];
        }

        /// <summary>
        /// Случайный bool с вероятностью
        /// </summary>
        public bool NextBool(float chance = 0.5f)
        {
            steps++;
            return (float)random.NextDouble() < chance;
        }

        /// <summary>
        /// Случайный bool с вероятностью (alias)
        /// </summary>
        public bool NextBool(float chance, float dummy)
        {
            return NextBool(chance);
        }

        /// <summary>
        /// Случайный индекс на основе весов
        /// </summary>
        public int NextWeighted(float[] weights)
        {
            if (weights == null || weights.Length == 0)
                return -1;

            float total = 0f;
            foreach (var w in weights)
                total += w;

            float value = NextFloat() * total;
            float cumulative = 0f;

            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (value < cumulative)
                    return i;
            }

            return weights.Length - 1;
        }

        /// <summary>
        /// Перемешать массив (Fisher-Yates)
        /// </summary>
        public void Shuffle<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }
        }

        /// <summary>
        /// Перемешать список (Fisher-Yates)
        /// </summary>
        public void Shuffle<T>(System.Collections.Generic.List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        /// <summary>
        /// Случайное число по нормальному распределению (Box-Muller)
        /// </summary>
        public float NextGaussian(float mean = 0f, float stdDev = 1f)
        {
            steps += 2;
            double u1 = random.NextDouble();
            double u2 = random.NextDouble();
            double z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            return mean + (float)z0 * stdDev;
        }

        /// <summary>
        /// Случайное число с ограничением по гауссу
        /// </summary>
        public int NextGaussianInt(int mean, int stdDev, int min, int max)
        {
            float value = NextGaussian(mean, stdDev);
            return UnityEngine.Mathf.Clamp(UnityEngine.Mathf.RoundToInt(value), min, max);
        }

        public override string ToString()
        {
            return $"SeededRandom[seed={seed}, steps={steps}]";
        }
    }
}
