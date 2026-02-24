namespace SquareBuddy;

public static partial class Constants
{
    /// <summary>
    /// Used to prefix table names in EF mappers 
    /// </summary>
    public const string TablePrefix = "sb";
    public const string DefaultDatabaseName = "squarebuddy";

    public const int TakeDefault = 50;
    public const int TakeMin = 1;
    public const int TakeMax = 1000;

    public const int SkipDefault = 0;
    public const int SkipMin = 0;
    public const int SkipMax = 999;

    /// <summary>
    /// According to email standards (RFC 5321 and RFC 5322), the maximum length of an email address is 320 characters
    /// </summary>
    public const int EmailMaxLength = 320;

    public const int BoardDefaultRowCount = 6;
    public const int BoardDefaultColCount = 6;
    public const string DefaultLanguage = "de";
}
