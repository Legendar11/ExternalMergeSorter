using System.IO.MemoryMappedFiles;

namespace DocumentGenerator.Extensions;

internal static class MemoryMappedViewAccessorExtensions
{
    public static bool TryWriteCharArray(this MemoryMappedViewAccessor accessor, long position, char[] array, int offset, int count, out long newPosition)
    {
        var bytesToWrite = count * sizeof(char);
        newPosition = position + bytesToWrite;

        if (newPosition > accessor.Capacity)
        {
            newPosition = position;
            return false;
        }

        accessor.WriteArray(position, array, offset, count);
        return true;
    }
}