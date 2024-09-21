using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using NCalc;
using Expression = NCalc.Expression;

public class FunctionInterpreter
{
    public static double Interpret(string expressionString, Dictionary<string, object> parameters)
    {
        if (!string.IsNullOrEmpty(expressionString))
        {
            Expression expression = new Expression(expressionString);
            expression.Parameters = parameters;
            object result = expression.Evaluate();

            return Convert.ToDouble(result);
        }

        return 0;
    }
}