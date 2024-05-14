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
        }

        if (number < 0)
        {
            buffer[position] = '-';
            position++;
            startPosition++;
            number = Math.Abs(number);
        }

        var digit = 0;
        while (number > 0)
        {
            digit = number % 10;

            if (position == startPosition && digit == 0)
            {
                digit = random.Next(1, 10);
            }

            buffer[position] = numbers[digit];
            position++;

            number /= 10;
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
