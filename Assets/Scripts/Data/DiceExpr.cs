using UnityEngine;

public static class DiceExpr
{
    public static float Roll(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return 0f;
        string s = expr.Replace(" ", "").ToLowerInvariant();
        float total = 0f;
        int i = 0;
        int sign = 1;
        while (i < s.Length)
        {
            if (s[i] == '+') { sign = 1; i++; continue; }
            if (s[i] == '-') { sign = -1; i++; continue; }
            float term = ParseTerm(s, ref i);
            total += sign * term;
            sign = 1;
        }
        return total;
    }

    public static float ResolveOnce(string expr)
    {
        return Roll(expr);
    }

    static float ParseTerm(string s, ref int i)
    {
        int start = i;
        while (i < s.Length && s[i] != '+' && s[i] != '-') i++;
        string term = s.Substring(start, i - start);
        int d = term.IndexOf('d');
        if (d < 0)
        {
            float n;
            if (!float.TryParse(term, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out n))
                return 0f;
            return n;
        }
        int count = 1;
        if (d > 0)
        {
            if (!int.TryParse(term.Substring(0, d), out count)) count = 1;
        }
        int sides;
        if (!int.TryParse(term.Substring(d + 1), out sides) || sides < 1) return 0f;
        if (count < 1) count = 1;
        float sum = 0f;
        for (int r = 0; r < count; r++)
            sum += Random.Range(1, sides + 1);
        return sum;
    }
}
