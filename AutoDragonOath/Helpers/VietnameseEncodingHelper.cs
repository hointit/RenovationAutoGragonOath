using System.Collections.Generic;
using System.Text;

namespace AutoDragonOath.Helpers;

public static class VietnameseEncodingHelper
{
    private static readonly Dictionary<char, char> CharacterMap = new()
    {
        // Full 2381824 Extended Character Mapping (0x80 - 0xFF)
        // Note: The keys are the ISO-8859-1 (Latin-1) characters corresponding to the VISCII byte.
        // For C0 control codes (0x80-0x9F), Latin-1 often maps them to C1 control codes, 
        // which may appear as unknown characters or '?' in text editors, but their char value is consistent.
        // We use the direct char conversion from the byte value (e.g., (char)0x80).

        // --- Upper half (0x80 - 0x9F) - Mapped to C1 Controls in Latin-1 ---
        { (char)0x80, 'Ạ' }, { (char)0x81, 'Ắ' }, { (char)0x82, 'Ằ' }, { (char)0x83, 'Ặ' },
        { (char)0x84, 'Ấ' }, { (char)0x85, 'Ầ' }, { (char)0x86, 'Ẩ' }, { (char)0x87, 'Ậ' },
        { (char)0x88, 'Ẽ' }, { (char)0x89, 'Ẹ' }, { (char)0x8A, 'Ế' }, { (char)0x8B, 'Ề' },
        { (char)0x8C, 'Ể' }, { (char)0x8D, 'Ễ' }, { (char)0x8E, 'Ệ' }, { (char)0x8F, 'Ố' },
        { (char)0x90, 'Ồ' }, { (char)0x91, 'Ổ' }, { (char)0x92, 'Ỗ' }, { (char)0x93, 'Ộ' },
        { (char)0x94, 'Ợ' }, { (char)0x95, 'Ớ' }, { (char)0x96, 'Ờ' }, { (char)0x97, 'Ở' },
        { (char)0x98, 'Ị' }, { (char)0x99, 'Ỏ' }, { (char)0x9A, 'Ọ' }, { (char)0x9B, 'Ỉ' },
        { (char)0x9C, 'Ủ' }, { (char)0x9D, 'Ũ' }, { (char)0x9E, 'Ụ' }, { (char)0x9F, 'Ỳ' },

        // --- Upper half (0xA0 - 0xFF) - Mapped to Punctuation/Symbols in Latin-1 ---
        // Punctuation/Symbols (0xA0-0xBF)
        { (char)0xA0, 'Õ' }, { (char)0xA1, 'ắ' }, { (char)0xA2, 'ằ' }, { (char)0xA3, 'ặ' },
        { (char)0xA4, 'ấ' }, { (char)0xA5, 'ầ' }, { (char)0xA6, 'ẩ' }, { (char)0xA7, 'ậ' },
        { (char)0xA8, 'ẽ' }, { (char)0xA9, 'ẹ' }, { (char)0xAA, 'ế' }, { (char)0xAB, 'ề' },
        { (char)0xAC, 'ể' }, { (char)0xAD, 'ễ' }, { (char)0xAE, 'ệ' }, { (char)0xAF, 'ố' },
        { (char)0xB0, 'ồ' }, { (char)0xB1, 'ổ' }, { (char)0xB2, 'ỗ' }, { (char)0xB3, 'Ỡ' },
        { (char)0xB4, 'Ơ' }, { (char)0xB5, 'ộ' }, { (char)0xB6, 'ờ' }, { (char)0xB7, 'ở' },
        { (char)0xB8, 'ị' }, { (char)0xB9, 'Ự' }, { (char)0xBA, 'Ứ' }, { (char)0xBB, 'Ừ' },
        { (char)0xBC, 'Ử' }, { (char)0xBD, 'ơ' }, { (char)0xBE, 'ớ' }, { (char)0xBF, 'Ư' },

        // Latin-1 Letters (0xC0-0xFF)
        { (char)0xC0, 'À' }, { (char)0xC1, 'Á' }, { (char)0xC2, 'Â' }, { (char)0xC3, 'Ã' },
        { (char)0xC4, 'Ả' }, { (char)0xC5, 'Ă' }, { (char)0xC6, 'ẳ' }, { (char)0xC7, 'ẵ' },
        { (char)0xC8, 'È' }, { (char)0xC9, 'É' }, { (char)0xCA, 'Ê' }, { (char)0xCB, 'Ẻ' },
        { (char)0xCC, 'Ì' }, { (char)0xCD, 'Í' }, { (char)0xCE, 'Ĩ' }, { (char)0xCF, 'ỳ' },
        { (char)0xD0, 'Đ' }, { (char)0xD1, 'ứ' }, { (char)0xD2, 'Ò' }, { (char)0xD3, 'Ó' },
        { (char)0xD4, 'Ô' }, { (char)0xD5, 'ạ' }, { (char)0xD6, 'ỷ' }, { (char)0xD7, 'ừ' },
        { (char)0xD8, 'ử' }, { (char)0xD9, 'Ù' }, { (char)0xDA, 'Ú' }, { (char)0xDB, 'ỹ' },
        { (char)0xDC, 'ỵ' }, { (char)0xDD, 'Ý' }, { (char)0xDE, 'ỡ' }, { (char)0xDF, 'ư' },
        { (char)0xE0, 'à' }, { (char)0xE1, 'á' }, { (char)0xE2, 'â' }, { (char)0xE3, 'ã' },
        { (char)0xE4, 'ả' }, { (char)0xE5, 'ă' }, { (char)0xE6, 'ữ' }, { (char)0xE7, 'ẫ' },
        { (char)0xE8, 'è' }, { (char)0xE9, 'é' }, { (char)0xEA, 'ê' }, { (char)0xEB, 'ẻ' },
        { (char)0xEC, 'ì' }, { (char)0xED, 'í' }, { (char)0xEE, 'ĩ' }, { (char)0xEF, 'ỉ' },
        { (char)0xF0, 'đ' }, { (char)0xF1, 'ự' }, { (char)0xF2, 'ò' }, { (char)0xF3, 'ó' },
        { (char)0xF4, 'ô' }, { (char)0xF5, 'õ' }, { (char)0xF6, 'ỏ' }, { (char)0xF7, 'ọ' },
        { (char)0xF8, 'ụ' }, { (char)0xF9, 'ù' }, { (char)0xFA, 'ú' }, { (char)0xFB, 'ũ' },
        { (char)0xFC, 'ủ' }, { (char)0xFD, 'ý' }, { (char)0xFE, 'ợ' }, { (char)0xFF, 'Ữ' }
    };

    /// <summary>
    ///     Converts a byte array encoded with VISCII to a standard Unicode (UTF-8) string.
    /// </summary>
    /// <param name="bytes">The raw bytes of the VISCII-encoded text.</param>
    /// <returns>The correctly decoded Vietnamese string in Unicode (UTF-8).</returns>
    public static string ParseVietnameseBytes(byte[] bytes)
    {
        // 1. Decode bytes using ISO-8859-1 (Latin-1).
        // Latin-1 is a single-byte encoding that maps byte 0-255 to char 0-255.
        // This preserves the VISCII byte value as a unique character,
        // which we then treat as our 'key' for the replacement map.
        var intermediateString = Encoding.GetEncoding("iso-8859-1").GetString(bytes);

        var unicodeResult = new StringBuilder(intermediateString.Length);

        foreach (var c in intermediateString)
            unicodeResult.Append(CharacterMap.TryGetValue(c, out var correctChar) ? correctChar : c);

        return unicodeResult.ToString().TrimStart().TrimEnd();
    }
}