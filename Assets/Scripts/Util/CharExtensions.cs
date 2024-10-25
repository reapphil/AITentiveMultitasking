using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public static class CharExtensions
{
    public static string ToEscapedString(this char c)
    {
        // Use built-in switch pattern matching for special cases 
        return c switch
        {
            '\a' => "\\a",
            '\b' => "\\b",
            '\f' => "\\f",
            '\n' => "\\n",
            '\r' => "\\r",
            '\t' => "\\t",
            '\v' => "\\v",
            '\\' => "\\\\",
            '\'' => "\\\'",
            '\"' => "\\\"",
            _ when !char.IsControl(c) => c.ToString(), // Printable as-is
            _ => $"\\x{(int)c:X2}" // Format as \xHH for other control/non-printable
        };
    }
}
