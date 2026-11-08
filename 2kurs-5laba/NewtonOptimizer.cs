using System;
using System.Collections.Generic;

namespace _2kurs_5laba {
  public sealed class NewtonOptimizationResult {
    public NewtonOptimizationResult(List<double> trajectory, bool success, string message)
    {
      Trajectory = trajectory;
      Success = success;
      Message = message;
    }

    public List<double> Trajectory { get; }
    public bool Success { get; }
    public string Message { get; }
  }

  public static class NewtonOptimizer {
    private const double DerivativeStep = 1e-5;
    private const int MaxIterations = 100;

    public static NewtonOptimizationResult FindMinimum(
        Func<double, double> function,
        double intervalStart,
        double intervalEnd,
        double precision)
    {
      if (function == null) throw new ArgumentNullException(nameof(function));
      if (!(intervalEnd > intervalStart)) throw new ArgumentException("Правый конец интервала должен быть строго больше левого.");
      if (!(precision > 0.0)) throw new ArgumentException("Точность должна быть положительной.");

      List<double> trajectory = new List<double>();

      double currentPoint = (intervalStart + intervalEnd) / 2.0;
      trajectory.Add(currentPoint);

      for (int iterationIndex = 0; iterationIndex < MaxIterations; ++iterationIndex)
      {
        double firstDerivative = CalculateFirstDerivative(function, currentPoint);
        double secondDerivative = CalculateSecondDerivative(function, currentPoint);

        if (Math.Abs(secondDerivative) < 1e-12)
        {
          return new NewtonOptimizationResult(trajectory, false, "Вторая производная равна нулю.");
        }

        double nextPoint = currentPoint - firstDerivative / secondDerivative;

        if (double.IsNaN(nextPoint) || double.IsInfinity(nextPoint))
        {
          return new NewtonOptimizationResult(trajectory, false, "Получено недопустимое значение координаты.");
        }

        if (nextPoint < intervalStart || nextPoint > intervalEnd)
        {
          return new NewtonOptimizationResult(trajectory, false, "Точка вышла за пределы заданного интервала.");
        }

        trajectory.Add(nextPoint);

        if (Math.Abs(nextPoint - currentPoint) < precision)
        {
          return new NewtonOptimizationResult(trajectory, true, "Минимум найден.");
        }

        currentPoint = nextPoint;
      }

      return new NewtonOptimizationResult(trajectory, false, "Достигнуто максимальное количество итераций.");
    }

    private static double CalculateFirstDerivative(Func<double, double> function, double point)
    {
      return (function(point + DerivativeStep) - function(point - DerivativeStep)) / (2.0 * DerivativeStep);
    }

    private static double CalculateSecondDerivative(Func<double, double> function, double point)
    {
      return (function(point + DerivativeStep) - 2.0 * function(point) + function(point - DerivativeStep)) / (DerivativeStep * DerivativeStep);
    }
  }
}