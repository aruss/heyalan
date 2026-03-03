namespace HeyAlan;

public static partial class Constants
{
    /// <summary>
    /// Used to prefix table names in EF mappers.
    /// </summary>
    public const string TablePrefix = "srbd";
    public const string DefaultDatabaseName = "heyalan";

    public const int TakeDefault = 50;
    public const int TakeMin = 1;
    public const int TakeMax = 1000;

    public const int SkipDefault = 0;
    public const int SkipMin = 0;
    public const int SkipMax = 999;

    /// <summary>
    /// According to RFC 5321 and RFC 5322, maximum email length is 320.
    /// </summary>
    public const int EmailMaxLength = 320;
}
