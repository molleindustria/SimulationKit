using System;
using System.Data;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

public static class VariableListExtensions
{
    // Extension method for List<Variable> so you can access the Variable by name.
    public static float ValueByName(this List<Variable> variables, string name)
    {
        foreach (Variable v in variables)
        {
            if (v.name == name)
            {
                return v.Value;
            }
        }
        Debug.LogWarning("Variable '" + name + "' not found.");
        return 0f;
    }
}

public static class Evaluator
{
    // Main evaluation method
    public static float Evaluate(string expression, List<Variable> vars)
    {
        expression = expression.Trim();

        if (expression.Contains("+="))
        {
            return EvaluateCompound(expression, "+=", vars, (a, b) => a + b);
        }
        else if (expression.Contains("-="))
        {
            return EvaluateCompound(expression, "-=", vars, (a, b) => a - b);
        }
        else if (expression.Contains("*="))
        {
            return EvaluateCompound(expression, "*=", vars, (a, b) => a * b);
        }
        else if (expression.Contains("/="))
        {
            return EvaluateCompound(expression, "/=", vars, (a, b) => a / b);
        }
        else if (expression.EndsWith("++"))
        {
            string varName = expression.Substring(0, expression.Length - 2).Trim();
            float current = GetVariableValue(varName, vars);
            float result = current + 1;
            SetVariable(varName, result, vars);
            return result;
        }
        else if (expression.EndsWith("--"))
        {
            string varName = expression.Substring(0, expression.Length - 2).Trim();
            float current = GetVariableValue(varName, vars);
            float result = current - 1;
            SetVariable(varName, result, vars);
            return result;
        }
        else if (expression.Contains("="))
        {
            // e.g., "x = x + 5"
            string[] parts = expression.Split('=');
            if (parts.Length == 2)
            {
                string varName = parts[0].Trim();
                string valueExpr = parts[1].Trim();
                valueExpr = SubstituteVariables(valueExpr, vars);
                float result = EvaluateExpression(valueExpr);
                SetVariable(varName, result, vars);
                return result;
            }
            else
            {
                Debug.LogError("Invalid assignment expression: " + expression);
                return 0f;
            }
        }
        else
        {
            // Substitute variable names and evaluate the expression.
            expression = SubstituteVariables(expression, vars);
            return EvaluateExpression(expression);
        }
    }

    // Evaluates boolean expressions; now supports compound boolean expressions
    // using &&, ||, !, ==, != and parenthesis.
    public static bool EvaluateBool(string expression, List<Variable> vars)
    {
        // Substitute variable names and random calls.
        expression = SubstituteVariables(expression, vars);
        expression = SubstituteRandoms(expression);

        // Convert C#-style boolean operators into DataTable-friendly ones.
        expression = ConvertBooleanOperators(expression);

        DataTable table = new DataTable();
        object result = table.Compute(expression, string.Empty);
        return Convert.ToBoolean(result);
    }

    // Helper: evaluates an arithmetic expression using DataTable.Compute.
    // This version also substitutes any Random() calls.
    private static float EvaluateExpression(string expression)
    {
        // Replace any Random(min, max) calls with a random value.
        expression = SubstituteRandoms(expression);
        DataTable table = new DataTable();
        object result = table.Compute(expression, string.Empty);
        return (float)Convert.ToDouble(result);
    }

    // A helper that evaluates an expression without substituting Random calls.
    // This is used inside SubstituteRandoms to avoid recursion.
    private static float EvaluateExpressionNoRandom(string expression)
    {
        DataTable table = new DataTable();
        object result = table.Compute(expression, string.Empty);
        return (float)Convert.ToDouble(result);
    }

    // Substitutes calls of the form Random(min, max) with a random number.
    private static string SubstituteRandoms(string expression)
    {
        // Regex pattern to capture "Random(min, max)"
        return Regex.Replace(expression, @"Random\s*\(\s*(?<min>[^,]+?)\s*,\s*(?<max>[^)]+?)\s*\)", match =>
        {
            string minStr = match.Groups["min"].Value;
            string maxStr = match.Groups["max"].Value;
            // Evaluate the min and max parts without doing another random substitution.
            float minVal = EvaluateExpressionNoRandom(minStr);
            float maxVal = EvaluateExpressionNoRandom(maxStr);
            float rnd = Mathf.Floor(UnityEngine.Random.Range(minVal, maxVal));
            return rnd.ToString();
        });
    }

    // Converts C#-style boolean operators to DataTable-friendly operators.
    private static string ConvertBooleanOperators(string expression)
    {
        // Replace equality operators: "==" to "=".
        expression = expression.Replace("==", "=");
        // Replace inequality operators: "!=" to "<>".
        expression = expression.Replace("!=", "<>");
        // Replace logical AND: "&&" to " AND ".
        expression = expression.Replace("&&", " AND ");
        // Replace logical OR: "||" to " OR ".
        expression = expression.Replace("||", " OR ");
        // Replace logical NOT: use a regex to replace "!" not followed by "=" with "NOT ".
        expression = Regex.Replace(expression, @"!(?!=)", "NOT ");
        return expression;
    }

    // Helper: substitutes variable names with their current values.
    // This implementation uses regex to detect all identifier tokens.
    private static string SubstituteVariables(string expression, List<Variable> vars)
    {
        // Build a dictionary for quick lookup.
        Dictionary<string, float> variableDict = new Dictionary<string, float>();
        foreach (Variable v in vars)
        {
            if (!variableDict.ContainsKey(v.name))
                variableDict.Add(v.name, v.Value);
        }

        // Regex pattern for identifiers: assumes variable names start with a letter or underscore.
        return Regex.Replace(expression, @"\b[a-zA-Z_]\w*\b", match =>
        {
            string token = match.Value;
            // Skip our Random keyword so we don't accidentally substitute it.
            if (token == "Random")
                return token;

            if (variableDict.ContainsKey(token))
            {
                return variableDict[token].ToString();
            }
            else
            {
                Debug.LogWarning("Variable '" + token + "' not found. Replacing with 0.");
                return "0";
            }
        });
    }

    // Handles compound assignments like +=, -=, etc.
    private static float EvaluateCompound(string expression, string op, List<Variable> vars, Func<float, float, float> operation)
    {
        string[] parts = expression.Split(new string[] { op }, StringSplitOptions.None);
        if (parts.Length == 2)
        {
            string varName = parts[0].Trim();
            string valueExpr = parts[1].Trim();
            float current = GetVariableValue(varName, vars);
            valueExpr = SubstituteVariables(valueExpr, vars);
            float operand = EvaluateExpression(valueExpr);
            float result = operation(current, operand);
            SetVariable(varName, result, vars);
            return result;
        }
        else
        {
            Debug.LogError("Invalid compound assignment: " + expression);
            return 0f;
        }
    }

    // Returns the current value of a variable from the list; if not found, returns 0.
    private static float GetVariableValue(string name, List<Variable> vars)
    {
        foreach (Variable v in vars)
        {
            if (v.name == name)
            {
                return v.Value;
            }
        }
        Debug.LogWarning("Variable '" + name + "' not found. Returning 0.");
        return 0f;
    }

    // Updates an existing variable or adds a new one.
    private static void SetVariable(string name, float value, List<Variable> vars)
    {
        foreach (Variable v in vars)
        {
            if (v.name == name)
            {
                v.Value = value;
                return;
            }
        }
        vars.Add(new Variable { name = name, Value = value });
    }
}
