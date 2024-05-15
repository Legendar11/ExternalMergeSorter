namespace DocumentGenerator.Utils;

public class StringWriter : IStringWriter
{
    private static readonly Random random = new();

    public void WriteNumber(int number, char[] buffer, ref int position)
    {
        var startPosition = position;

        if (number == 0)
        {
            buffer[position] = '0';
            position++;
            return;
        }

        if (number < 0)
        {
            buffer[position] = '-';
            position++;
            startPosition++;
            number = -number;
        }

        var digit = 0;
        while (number > 0)
        {
            digit = number % 10;

            buffer[position] = numbers[digit];
            position++;

            number /= 10;
        }

        for (var i = 0; i < ((position - startPosition) / 2); i++)
        {
            (buffer[startPosition + i], buffer[position - i - 1])
                = (buffer[position - i - 1], buffer[startPosition + i]);
        }
    }

    public void WriteSymbols(char[] phrase, char[] buffer, ref int position)
    {
        for (var i = 0; i < phrase.Length; i++)
        {
            buffer[position] = phrase[i];
            position++;
        }
    }

    public void WriteRandomSymbols(int length, char[] buffer, ref int position)
    {
        for (var i = 0; i < length; i++)
        {
            buffer[position] = chars[random.Next(chars.Length)];
            position++;
        }
    }

    private static readonly char[] chars =
        "abcdefghijklmnopqrstuvwxyz".ToCharArray();

    private static readonly char[] numbers = "0123456789".ToCharArray();
}
