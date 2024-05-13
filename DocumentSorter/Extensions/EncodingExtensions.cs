using System.Text;

namespace DocumentSorter.Extensions;

internal static class EncodingExtensions
{
    public static int GetSymbolSize(this Encoding encoding) => encoding.GetByteCount("0");
}
