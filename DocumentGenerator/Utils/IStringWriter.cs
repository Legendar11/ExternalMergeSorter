using DocumentGenerator.Configuration;

namespace DocumentGenerator.Utils;

public interface IStringWriter
{
    StringWriterConfiguration Options { get; }

    int WriteLine(char[] buffer, ref int bufferPosition);

    int WriteRandomSymbols(int length, char[] buffer, ref int position);

    int WriteNewLine(char[] buffer, ref int position);
}
