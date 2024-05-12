namespace DocumentGenerator;

public static class Constants
{
    public static int DefaultDegreeOfParallelism => (Environment.ProcessorCount / 2) + 1;

    public const long LineBufferSize = 1_000_000L;

    public readonly static char[][] Phrases =
    [
        "No pity! No remorse! No fear!".ToCharArray(),
        "In the Emperor's name, let none survive!".ToCharArray(),
        "No army is big enough to conquer the galaxy. But faith alone can overturn the universe".ToCharArray(),
        "The most dangerous weapon is the one no one expects".ToCharArray(),
        "Courage and Honour".ToCharArray(),
        "Knowledge is power, guard it well".ToCharArray(),
        "The man who has nothing can still have faith".ToCharArray(),
        "A mind without purpose will wander in dark places".ToCharArray(),
        "Victory is achieved through mettle. Glory is achieved through metal".ToCharArray(),
        "A hero is no braver than an ordinary man, but he is brave five minutes longer".ToCharArray(),
        "They shall be my finest warriors, these men who give of themselves to me".ToCharArray(),
        "Faith without deeds is worthless".ToCharArray(),
        "We do not fight for victory, but for the survival of our species".ToCharArray(),
        "By His will alone is Terra kept safe".ToCharArray(),
        "Ruthlessness is the kindness of the wise".ToCharArray(),
        "In the face of death I shall have no remorse".ToCharArray(),
        "Let none find us wanting".ToCharArray(),
        "Through the destruction of our enemies do we earn our salvation".ToCharArray(),
        "Innocence proves nothing".ToCharArray(),
        "Wisdom is the beginning of fear".ToCharArray(),
        "The Emperor Protects".ToCharArray(),
        "Speak the truth, even if your voice shakes".ToCharArray(),
        "The difference between heresy and treachery is ignorance".ToCharArray(),
        "True Happiness stems only from Duty".ToCharArray(),
        "Heresy grows from idleness".ToCharArray(),
        "There is only the Emperor, and he is our shield and protector".ToCharArray(),
        "Truth is Subjective".ToCharArray(),
        "Damnation is Eternal".ToCharArray(),
        "He who keeps silent consents".ToCharArray(),
        "Work earns Salvation".ToCharArray(),
        "Victory needs no explanation, defeat allows none".ToCharArray(),
        "Blessed is the mind too small for doubt".ToCharArray(),
        "Success is commemorated; Failure merely remembered".ToCharArray(),
        "Hope is the first step on the road to disappointment".ToCharArray(),
        "All souls call out for salvation".ToCharArray(),
        "To Question is to doubt".ToCharArray(),
        "Only the Emperor is all".ToCharArray(),
    ];
}
