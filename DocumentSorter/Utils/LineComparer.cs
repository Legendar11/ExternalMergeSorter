using System.Collections.Concurrent;

namespace DocumentSorter.Utils;

internal class LineComparer(char[] Delimeter, ConcurrentDictionary<int, int> DictHash) : IComparer<string>
{
    public int Compare(string? a, string? b)
    {
        var indexA = a!.IndexOf(Delimeter[0]);
        var indexB = b!.IndexOf(Delimeter[0]);

        var hash = CustomHashCodeForChars(
            a,
            indexA + Delimeter.Length,
            a.Length,
            b,
            indexB + Delimeter.Length,
            b.Length);
        var isCached = DictHash.TryGetValue(hash, out var cached);

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
                    DictHash.TryAdd(hash, compared);
                    return compared;
                }

                if (i == a.Length)
                {
                    if (j != b.Length)
                    {
                        DictHash.TryAdd(hash, -1);
                        return -1;
                    }
                    else
                    {
                        DictHash.TryAdd(hash, 0);
                        break;
                    }
                }
                else if (j == b.Length)
                {
                    if (i != a.Length)
                    {
                        DictHash.TryAdd(hash, 1);
                        return 1;
                    }
                    else
                    {
                        DictHash.TryAdd(hash, 0);
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
