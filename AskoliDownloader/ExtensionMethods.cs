using System.Linq;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;

namespace AskoliDownloader
{
    public static class ExtensionMethods
    {
        #region Default Extentions

        public static bool IsNullOrEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        public static bool IsNotNullOrEmpty(this string source)
        {
            return !string.IsNullOrEmpty(source);
        }

        public static bool IsNotEmptyObject(this object prop)
        {
            return !prop.IsEmptyObject();
        }

        public static bool IsEmptyObject(this object prop)
        {
            if (prop == null)
            {
                return true;
            }
            else if (prop is string)
            {
                return prop.ToString().Length == 0;
            }
            else
            {
                var ps = prop.GetType().GetProperties();

                foreach (var pi in ps)
                {
                    var value = pi.GetValue(prop, null);
                    var valueStr = value?.ToString() ?? string.Empty;    // if value not a class inside class

                    if (valueStr.IsNotNullOrEmpty())
                        return false;
                }

            }
            return true;
        }

        public static bool IsAny<T>(this IEnumerable<T> source)
        {
            return source != null && source.Any();
        }

        public static string ToFullFormat(this DateTime source)
        {
            return source.ToString("dd/MM/yyyy hh:MM");
        }

        public static string ToReadableFileSize(this long source)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            var order = 0;
            double result = source;
            while (result >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                result = result / 1024;
            }

            if (order == 0)
            {
                result = 1;
                order++;
            }
            // Adjust the format string to your preferences. For example "{0:0.#}{1}" would
            // show a single decimal place, and no space.
            return $"{result:0.##} {sizes[order]}";
        }

        public static long FolderSize(this DirectoryInfo source)
        {
            if (source.IsEmptyObject())
            {
                Console.WriteLine("Invalid Folder path");
            }

            try
            {
                var allFiles = source.GetFiles("*.*", SearchOption.AllDirectories);
                if (allFiles.IsAny())
                {
                    return allFiles.Sum(file => file.Length);
                }
            }
            catch (Exception ex)
            {
                Utils.WriteErrorLog(ex);
            }

            return 0;
        }

        public static T ToEnum<T>(this string value)
        {
            if (value.IsNullOrEmpty())
            {
                return default(T);
            }

            T result;
            try
            {
                result = (T)Enum.Parse(typeof(T), value, true);
                return result;
            }
            catch
            {
                return default(T);
            }
        }

        public static bool EqualsIgnoreCase(this string source, string value)
        {
            if (source.IsNullOrEmpty())
            {
                return false;
            }

            return source.Equals(value, StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool ContainsIgnoreCase(this string source, string value)
        {
            if (source.IsNullOrEmpty())
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public static bool HasNumber(this string source)
        {
            return !source.IsNullOrEmpty() && source.Any(char.IsDigit);
        }

        public static TimeSpan Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, TimeSpan> selector)
        {
            return source.Select(selector).Aggregate(TimeSpan.Zero, (t1, t2) => t1 + t2);
        }

        public static string EncodingUtf8(this string source)
        {
            if (source.IsNullOrEmpty())
            {
                return string.Empty;
            }

            var bytes = Encoding.Default.GetBytes(source);
            return Encoding.UTF8.GetString(bytes);
        }

        public static string PrintHebrew(this string source)
        {
            if (source.IsNullOrEmpty()) return string.Empty;

            return $"{string.Join("", source.Reverse())}";
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            var seenKeys = new HashSet<TKey>();
            foreach (var element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }
        #endregion Default Extentions
    }
}