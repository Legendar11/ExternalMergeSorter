using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentSorter;

internal class LineComparer(char[] Delimeter) : IComparer<string>
{
    private readonly ConcurrentDictionary<int, int> dict = new();

    public int Compare(string? a, string? b)
    {
        if (a == null || b == null)
        {
            throw new ArgumentNullException();
        }

        var indexA = a.IndexOf(Delimeter[0]);
        var indexB = b.IndexOf(Delimeter[0]);

        var hash = CustomHashCodeForChars(a, indexA, a.Length, b, indexB, b.Length);

        if (dict.TryGetValue(hash, out var cached) && cached != 0)
        {
            return cached;
        }

        int i = indexA + Delimeter.Length, j = indexB + Delimeter.Length;
        int compared = 0;

        do
        {
            compared = a[i].CompareTo(b[j]);

            i++;
            j++;

            if (compared != 0)
            {
                dict.TryAdd(hash, compared);
                return compared;
            }

            if (i == a.Length)
            {
                if (j != b.Length)
                {
                    dict.TryAdd(hash, -1);
                    return -1;
                }
                else
                {
                    break;
                }
            }
            else if (j == b.Length)
            {
                if (i != a.Length)
                {
                    dict.TryAdd(hash, 1);
                    return 1;
                }
                else
                {
                    break;
                }
            }
        } while (true);

        if (indexA > indexB)
        {
            return 1;
        }
        else if (indexA < indexB)
        {
            return -1;
        }

        for (i = 0; i < indexA; i++)
        {
            compared = a[i].CompareTo(b[i]);

            if (compared != 0)
            {
                return compared;
            }
        }

        return 0;
    }


    private int CustomHashCodeForChars(string a, int startA, int endA, string b, int startB, int endB)
    {
        unchecked
        {
            int result = endA + endB;

            for (var i = startA; i < endA; i++)
            {
                result = result * 314159 + a[i];
            }
            for (var i = startB; i < endB; i++)
            {
                result = result * 314159 + b[i];
            }

            return result;
        }
    }
}