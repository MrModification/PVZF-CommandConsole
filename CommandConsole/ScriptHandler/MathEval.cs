
public static class MathEval
{
    public static float Eval(string expr)
    {
        if (string.IsNullOrEmpty(expr))
            return 0f;

        int index = 0;
        return ParseAddSub(expr.Replace(" ", ""), ref index);
    }

    private static float ParseAddSub(string expr, ref int index)
    {
        float value = ParseMulDiv(expr, ref index);

        while (index < expr.Length)
        {
            char op = expr[index];
            if (op != '+' && op != '-') break;
            index++;

            float rhs = ParseMulDiv(expr, ref index);
            if (op == '+') value += rhs;
            else value -= rhs;
        }

        return value;
    }

    private static float ParseMulDiv(string expr, ref int index)
    {
        float value = ParseAtom(expr, ref index);

        while (index < expr.Length)
        {
            char op = expr[index];
            if (op != '*' && op != '/' && op != '%') break;
            index++;

            float rhs = ParseAtom(expr, ref index);
            if (op == '*') value *= rhs;
            else if (op == '/') value /= rhs;
            else value %= rhs;
        }

        return value;
    }

    private static float ParseAtom(string expr, ref int index)
    {
        if (index >= expr.Length)
            return 0f;

        if (expr[index] == '(')
        {
            index++;
            float value = ParseAddSub(expr, ref index);
            if (index < expr.Length && expr[index] == ')')
                index++;
            return value;
        }

        int start = index;
        while (index < expr.Length &&
               (char.IsDigit(expr[index]) || expr[index] == '.'))
        {
            index++;
        }

        string number = expr.Substring(start, index - start);
        float result;
        if (!float.TryParse(number, out result))
            result = 0f;
        return result;
    }
}