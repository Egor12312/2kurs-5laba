using System;
using System.Globalization;

namespace _2kurs_5laba {
  public sealed class FormulaEvaluator {
    private const char OpeningParenthesis = '(';
    private const char ClosingParenthesis = ')';
    private const char PlusSign = '+';
    private const char MinusSign = '-';
    private const char MultiplicationSign = '*';
    private const char DivisionSign = '/';
    private const char PowerSign = '^';
    private const char DecimalSeparator = '.';

    private const string VariableIdentifier = "x";
    private const string PiIdentifier = "pi";
    private const string EulerIdentifier = "e";

    private readonly string sourceText;
    private int currentPosition;

    public FormulaEvaluator(string sourceText)
    {
      this.sourceText = sourceText ?? string.Empty;
    }

    public double Evaluate(double variableValue)
    {
      currentPosition = 0;
      double result = ParseExpression(variableValue);
      SkipWhitespace();
      if (currentPosition < sourceText.Length)
      {
        throw new FormatException($"Непредвиденный символ на позиции {currentPosition}: '{sourceText[currentPosition]}'.");
      }
      if (double.IsNaN(result) || double.IsInfinity(result))
      {
        throw new ArithmeticException("Значение функции не является конечным числом.");
      }
      return result;
    }

    private double ParseExpression(double variableValue)
    {
      double result = ParseTerm(variableValue);
      while (true)
      {
        SkipWhitespace();
        if (TryConsume(PlusSign)) result += ParseTerm(variableValue);
        else if (TryConsume(MinusSign)) result -= ParseTerm(variableValue);
        else return result;
      }
    }

    private double ParseTerm(double variableValue)
    {
      double result = ParseUnary(variableValue);
      while (true)
      {
        SkipWhitespace();
        if (TryConsume(MultiplicationSign)) result *= ParseUnary(variableValue);
        else if (TryConsume(DivisionSign))
        {
          double divisor = ParseUnary(variableValue);
          if (divisor == 0.0) throw new DivideByZeroException("Деление на ноль в формуле.");
          result /= divisor;
        }
        else return result;
      }
    }

    private double ParseUnary(double variableValue)
    {
      SkipWhitespace();
      if (TryConsume(MinusSign)) return -ParseUnary(variableValue);
      if (TryConsume(PlusSign)) return ParseUnary(variableValue);
      return ParseFactor(variableValue);
    }

    private double ParseFactor(double variableValue)
    {
      double baseValue = ParsePrimary(variableValue);
      SkipWhitespace();
      if (TryConsume(PowerSign))
      {
        double exponent = ParseFactor(variableValue);
        return Math.Pow(baseValue, exponent);
      }
      return baseValue;
    }

    private double ParsePrimary(double variableValue)
    {
      SkipWhitespace();
      if (currentPosition >= sourceText.Length) throw new FormatException("Неожиданный конец формулы.");

      char currentChar = sourceText[currentPosition];

      if (currentChar == OpeningParenthesis)
      {
        ++currentPosition;
        double innerValue = ParseExpression(variableValue);
        SkipWhitespace();
        if (!TryConsume(ClosingParenthesis)) throw new FormatException("Отсутствует закрывающая скобка.");
        return innerValue;
      }

      if (char.IsLetter(currentChar))
      {
        string identifier = ReadIdentifier();
        SkipWhitespace();
        if (TryConsume(OpeningParenthesis))
        {
          double argument = ParseExpression(variableValue);
          SkipWhitespace();
          if (!TryConsume(ClosingParenthesis)) throw new FormatException($"Для функции '{identifier}' отсутствует закрывающая скобка.");
          return ApplyFunction(identifier, argument);
        }
        return ResolveIdentifier(identifier, variableValue);
      }

      if (char.IsDigit(currentChar) || currentChar == DecimalSeparator)
      {
        return ReadNumber();
      }

      throw new FormatException($"Непредвиденный символ '{currentChar}' на позиции {currentPosition}.");
    }

    private string ReadIdentifier()
    {
      int startPosition = currentPosition;
      while (currentPosition < sourceText.Length && (char.IsLetter(sourceText[currentPosition]) || char.IsDigit(sourceText[currentPosition])))
      {
        ++currentPosition;
      }
      return sourceText.Substring(startPosition, currentPosition - startPosition).ToLowerInvariant();
    }

    private double ReadNumber()
    {
      int startPosition = currentPosition;
      bool hasDecimalSeparator = false;
      while (currentPosition < sourceText.Length)
      {
        char currentChar = sourceText[currentPosition];
        if (char.IsDigit(currentChar))
        {
          ++currentPosition;
          continue;
        }
        if (currentChar == DecimalSeparator && !hasDecimalSeparator)
        {
          hasDecimalSeparator = true;
          ++currentPosition;
          continue;
        }
        break;
      }
      string numberText = sourceText.Substring(startPosition, currentPosition - startPosition);
      if (!double.TryParse(numberText, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue))
      {
        throw new FormatException($"Некорректное число '{numberText}'.");
      }
      return parsedValue;
    }

    private static double ResolveIdentifier(string identifier, double variableValue)
    {
      return identifier switch
      {
        VariableIdentifier => variableValue,
        PiIdentifier => Math.PI,
        EulerIdentifier => Math.E,
        _ => throw new FormatException($"Неизвестный идентификатор '{identifier}'.")
      };
    }

    private static double ApplyFunction(string identifier, double argument)
    {
      return identifier switch
      {
        "sin" => Math.Sin(argument),
        "cos" => Math.Cos(argument),
        "tan" => Math.Tan(argument),
        "exp" => Math.Exp(argument),
        "ln" => Math.Log(argument),
        "log" => Math.Log10(argument),
        "sqrt" => Math.Sqrt(argument),
        "abs" => Math.Abs(argument),
        _ => throw new FormatException($"Неизвестная функция '{identifier}'.")
      };
    }

    private void SkipWhitespace()
    {
      while (currentPosition < sourceText.Length && char.IsWhiteSpace(sourceText[currentPosition]))
      {
        ++currentPosition;
      }
    }

    private bool TryConsume(char expectedCharacter)
    {
      SkipWhitespace();
      if (currentPosition < sourceText.Length && sourceText[currentPosition] == expectedCharacter)
      {
        ++currentPosition;
        return true;
      }
      return false;
    }
  }
}