using System;
using System.Collections.Generic;
using System.Linq;

namespace AzureCAT.Samples.AppInsight
{
    public static class MathExtensions
    {
        public static double StdDev(this IEnumerable<double> values)
        {
            double ret = 0;
            int count = values.Count();

            if (count  > 1)
            {
                double avg = values.Average();
                double sum = values.Sum(d => (d - avg) * (d - avg));
                ret = Math.Sqrt(sum / count);
            }
            return ret;
        }

        public static double StdDev<TSource>(this IEnumerable<TSource> source,
            Func<TSource, double> selector)
        {
            return MathExtensions.StdDev(Enumerable.Select(source, selector));
        }
    }

    public static class LinqExtensions
    {
        public static IEnumerable<TSource> EnrichDictionary<TSource>(
            this IEnumerable<TSource> source, 
            Func<TSource, IDictionary<string, string>> selector,
            IDictionary<string, string> newRecords)
        {
            foreach (var s in source)
            {
                var dict = selector(s);
                foreach (var nr in newRecords)
                    dict.Add(nr.Key, nr.Value);
                yield return s;
            }
        } 

        public static IEnumerable<TResult> SelectWithEnrich<TSource, TResult>(
            this IEnumerable<TSource> source, 
            Func<TSource, TResult> selector,
            Func<TResult, IDictionary<string, string>> dictSelector,
            IDictionary<string, string> newItems)
        {
            foreach (var s in source)
            {
                var res = selector(s);
                var dict = dictSelector(res);
                if (dict != null)
                {
                    foreach (var n in newItems)
                        dict.Add(n.Key, n.Value);
                }

                yield return res ;
            }
        }
    }
}