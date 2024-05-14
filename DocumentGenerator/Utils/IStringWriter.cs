using DocumentGenerator.Configuration;

namespace DocumentGenerator.Utils;

public interface IStringWriter
{
    void WriteNumber(int number, char[] buffer, ref int position);

    void WriteSymbols(char[] phrase, char[] buffer, ref int position);

    void WriteRandomSymbols(int length, char[] buffer, ref int position);
}
