using System.IO.MemoryMappedFiles;

namespace DocumentGenerator.Extensions;

internal static class MemoryMappedViewAccessorExtensions
{
    public static bool TryWriteByteArray(this MemoryMappedViewAccessor accessor, long position, byte[] array, int offset, int count, out long newPosition)
    {
        newPosition = position + count;

        if (newPosition > accessor.Capacity)
        {
            newPosition = position;
            return false;
        }

        accessor.WriteArray(position, array, offset, count);
        return true;
    }
}