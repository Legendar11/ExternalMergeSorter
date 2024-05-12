using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentSorter.Configuration;

public class DocumentSorterOptions
{
    public long InitialChunkFileSize { get; init; } = 1024 * 1024;

    public char[] Delimeter { get; init; } = ". ".ToCharArray();
}
