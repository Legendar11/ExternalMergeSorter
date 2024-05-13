using System.Collections.Concurrent;

namespace DocumentSorter;

internal class LineComparer(char[] Delimeter) : IComparer<string>
{
    private readonly ConcurrentDictionary<int, int> dict = new(8, 1_000_000);

    public int Compare(string? a, string? b)
    {
        var indexA = a!.IndexOf(Delimeter[0]);
        var indexB = b!.IndexOf(Delimeter[0]);

        var hash = CustomHashCodeForChars(a, indexA, a.Length, b, indexB, b.Length);
        var isCached = dict.TryGetValue(hash, out var cached);

        if (isCached && cached != 0)
        {
            return cached;
        }

        int i, j;
        int compared;

        if (!isCached)
        {
            i = indexA + Delimeter.Length;
            j = indexB + Delimeter.Length;
            compared = 0;

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
                        dict.TryAdd(hash, 0);
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
                        dict.TryAdd(hash, 0);
                        break;
                    }
                }
            } while (true);
        }

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
                result = result * 37 + b[i];
            }

            return result;
        }
    }
}

//internal class RowComparer(IComparer<string> comparer) : IComparer<(string? Value, int Index)>
//{
//    public int Compare((string? Value, int Index) x, (string? Value, int Index) y)
//    {
//        return comparer.Compare(x.Value, y.Value);
//    }
//}
