using System.Text;

public static class SentisUtilities
{
    public static int[] SetupWhiteSpaceShifts(int[] whiteSpaceCharacters)
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
        }

        return whiteSpaceCharacters;
    }

    public static bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
    }

    // Translates encoded special characters to Unicode
    public static string GetUnicodeText(string text, int[] whiteSpaceCharacters)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text, whiteSpaceCharacters));
        return Encoding.UTF8.GetString(bytes);
    }

    public static string ShiftCharacterDown(string text, int[] whiteSpaceCharacters)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter :
                (char)whiteSpaceCharacters[(int)(letter - 256)];
        }
        return outText;
    }
}
