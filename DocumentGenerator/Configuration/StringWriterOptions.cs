using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentGenerator.Configuration;

public class StringWriterOptions
{
    public int GenerateNumberFrom { get; init; } = 0;

    public int GenerateNumberTo { get; init; } = 1_000_000;

    public char[] Delimeter { get; init; } = ". ".ToCharArray();

    public char[] NewLine { get; init; } = "\r\n".ToCharArray();
}
