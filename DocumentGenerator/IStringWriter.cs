using DocumentGenerator.Configuration;

namespace DocumentGenerator;

public interface IStringWriter
{
    StringWriterOptions Options { get; }

    int WriteLine(char[] buffer, ref int bufferPosition);

    int WriteRandomSymbols(int length, char[] buffer, ref int position);

    int WriteNewLine(char[] buffer, ref int position);
}
