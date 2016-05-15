using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace VLM.DAS2.Core.Extensions
{
    public static class EnumerableExtensions
    {
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> coll)
        {
            var c = new ObservableCollection<T>();
            foreach (var e in coll)
                c.Add(e);
            return c;
        }

        public static ObservableCollection<T> ToObservableCollection<T>(this ICollection<T> coll)
        {
            var c = new ObservableCollection<T>();
            foreach (var e in coll)
                c.Add(e);
            return c;
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        /// <summary>
        /// Execute <paramref name="action"/> for each element of <paramref name="sequence"/>.
        /// </summary>
        /// <typeparam name="T">Type of items in <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">Sequence of items to act on.</param>
        /// <param name="action">Action to invoke for each item.</param>
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence)
            {
                action(item);
            }
        }

        /// <summary>
        /// Given a sequence, combine it with another sequence, passing the corresponding
        /// elements of each sequence to the <paramref name="zipper"/> action to create
        /// a new single value from the two sequence elements. "Zip" here refers to a zipper,
        /// not the compression algorithm. The resulting sequence will have the same number
        /// of elements as the shorter of sequence1 and sequence2.
        /// </summary>
        /// <typeparam name="T1">Type of the elements in the first sequence.</typeparam>
        /// <typeparam name="T2">Type of the elements in the second sequence.</typeparam>
        /// <typeparam name="TResult">Type of the resulting sequence elements.</typeparam>
        /// <param name="sequence1">The first sequence to combine.</param>
        /// <param name="sequence2">The second sequence to combine.</param>
        /// <param name="zipper">Func used to calculate the resulting values.</param>
        /// <returns>The result sequence.</returns>
        public static IEnumerable<TResult> Zip<T1, T2, TResult>(this IEnumerable<T1> sequence1,
                                                                IEnumerable<T2> sequence2, Func<T1, T2, TResult> zipper)
        {
            IEnumerator<T1> enumerator1 = sequence1.GetEnumerator();
            IEnumerator<T2> enumerator2 = sequence2.GetEnumerator();

            while (enumerator1.MoveNext())
            {
                if (!enumerator2.MoveNext())
                    yield break;

                yield return zipper(enumerator1.Current, enumerator2.Current);
            }
        }

        /// <summary>
        /// Take two sequences and return a new sequence of <see cref="KeyValuePair{TKey,TValue}"/> objects.
        /// </summary>
        /// <typeparam name="T1">Type of objects in sequence1.</typeparam>
        /// <typeparam name="T2">Type of objects in sequence2.</typeparam>
        /// <param name="sequence1">First sequence.</param>
        /// <param name="sequence2">Second sequence.</param>
        /// <returns>The sequence of <see cref="KeyValuePair{TKey,TValue}"/> objects.</returns>
        public static IEnumerable<KeyValuePair<T1, T2>> Zip<T1, T2>(this IEnumerable<T1> sequence1, IEnumerable<T2> sequence2)
        {
            return sequence1.Zip(sequence2, (i1, i2) => new KeyValuePair<T1, T2>(i1, i2));
        }

        //makes expression for specific prop
        public static Expression<Func<TSource, object>> GetExpression<TSource>(string propertyName)
        {
            var param = Expression.Parameter(typeof(TSource), "x");
            Expression conversion = Expression.Convert(Expression.Property
            (param, propertyName), typeof(object));   //important to use the Expression.Convert
            return Expression.Lambda<Func<TSource, object>>(conversion, param);
        }

        //makes delegate for specific prop
        public static Func<TSource, object> GetFunc<TSource>(string propertyName)
        {
            return GetExpression<TSource>(propertyName).Compile();  //only need compiled expression
        }

        //OrderBy overload
        public static IOrderedEnumerable<TSource> OrderBy<TSource>(this IEnumerable<TSource> source, string propertyName)
        {
            return source.OrderBy(GetFunc<TSource>(propertyName));
        }

        //OrderBy overload
        public static IOrderedQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, string propertyName)
        {
            return source.OrderBy(GetExpression<TSource>(propertyName));
        }
    }
}
