using DocumentGenerator.Configuration;

namespace DocumentGenerator;

public class StringWriter(StringWriterOptions options) : IStringWriter
{
    private static readonly Random random = new();

    public StringWriterOptions Options => options;

    public int WriteLine(char[] buffer, ref int position)
    {
        var number = random.Next(options.GenerateNumberFrom, options.GenerateNumberTo);
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
        } // reverse array

        for (var i = 0; i < options.Delimeter.Length; i++)
        {
            buffer[position] = options.Delimeter[i];
            position++;
        }

        var phrase = Constants.Phrases[random.Next(Constants.Phrases.Length)];
        for (var i = 0; i < phrase.Length; i++)
        {
            buffer[position] = phrase[i];
            position++;
        }

        for (var i = 0; i < options.NewLine.Length; i++)
        {
            buffer[position] = options.NewLine[i];
            position++;
        }

        return position;
    }

    public int WriteRandomSymbols(int length, char[] buffer, ref int position)
    {
        for (var i = 0; i < length; i++)
        {
            buffer[position] = chars[random.Next(chars.Length)];
            position++;
        }

        return length;
    }

    public int WriteNewLine(char[] buffer, ref int position)
    {
        for (var i = 0; i < options.NewLine.Length; i++)
        {
            buffer[position] = options.NewLine[i];
            position++;
        }

        return options.NewLine.Length;
    }


    private static readonly char[] chars =
        "abcdefghijklmnopqrstuvwxyz".ToCharArray();

    private static readonly char[] numbers = "0123456789".ToCharArray();
}
