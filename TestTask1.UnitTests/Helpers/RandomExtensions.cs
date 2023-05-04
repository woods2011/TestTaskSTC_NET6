namespace TestTask1.UnitTests.Helpers;

public static class RandomExtensions
{
    public static T NextOneOf<T>(this Random random, params T[] values)
    {
        if (values.Length is 0) throw new ArgumentException("Аргумент не должен быть пустым.", nameof(values));

        int randomIndex = random.Next(values.Length);
        return values[randomIndex];
    }
}