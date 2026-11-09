using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace _2kurs_5laba {
  public partial class Form1 : Form {
    private const double DefaultIntervalStartValue = 0.0;
    private const double DefaultIntervalEndValue = 5.0;
    private const double DefaultPrecisionValue = 1e-4;
    private const double MinimumAllowedPrecisionValue = 1e-12;
    private const double ChartPaddingFactorValue = 0.15;
    private const double DerivativeStepValue = 1e-5;

    private const string DefaultFormulaText = "x^2 - 4*x + 3";
    private const string ResultNumberFormat = "G10";
    private const string TickNumberFormat = "0.###";

    private const int InitialWindowWidthValue = 1200;
    private const int InitialWindowHeightValue = 720;
    private const int InputPanelWidthValue = 360;
    private const int MarginSizeValue = 8;
    private const int RowHeightValue = 30;
    private const int LabelColumnWidthValue = 120;
    private const int ResultLabelHeightValue = 120;
    private const int ChartSampleCountValue = 400;
    private const int ModeDefaultIndex = 0;
    private const int TargetTickCountValue = 10;

    private const int MarkerRadiusValue = 6;
    private const int TrajectoryRadiusValue = 3;
    private const int AxisLineWidthValue = 1;
    private const int IntervalLineWidthValue = 2;
    private const int TangentLineWidthValue = 2;
    private const int LabelFontSizeValue = 9;
    private const int TickFontSizeValue = 8;
    private const int AxisLabelOffsetValue = 5;
    private const int IntervalLabelOffsetValue = 10;
    private const int TickMarkLengthValue = 4;
    private const float IntervalDashPatternValue = 4f;
    private const float TangentDashPatternValue = 5f;
    private const float ResultLabelFontSizeValue = 10f;
    private const float FullPercentSizeValue = 100f;

    private TextBox intervalStartField = null!;
    private TextBox intervalEndField = null!;
    private TextBox precisionField = null!;
    private TextBox formulaField = null!;
    private ComboBox modeSelector = null!;
    private PictureBox functionPictureBox = null!;
    private Label resultLabel = null!;

    private ToolStripMenuItem stepForwardItem = null!;
    private ToolStripMenuItem stepBackwardItem = null!;
    private ToolStripMenuItem toStartItem = null!;
    private ToolStripMenuItem toEndItem = null!;

    private List<double> currentTrajectory = new List<double>();
    private int currentStepIndex = -1;
    private Func<double, double> currentTargetFunction = null!;
    private double currentIntervalStart;
    private double currentIntervalEnd;
    private bool currentSuccess;
    private string currentMessage = string.Empty;

    public Form1()
    {
      Text = "Лабораторная работа №5 — Метод Ньютона";
      ClientSize = new Size(InitialWindowWidthValue, InitialWindowHeightValue);
      MinimumSize = new Size(InitialWindowWidthValue, InitialWindowHeightValue);
      StartPosition = FormStartPosition.CenterScreen;

      BuildInterface();
      UpdateStepMenuState();
    }

    private void BuildInterface()
    {
      TableLayoutPanel rootLayout = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 1,
        RowCount = 2
      };
      rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
      rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, FullPercentSizeValue));
      rootLayout.Controls.Add(BuildMenuStrip(), 0, 0);

      TableLayoutPanel contentLayout = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        RowCount = 1,
        Padding = new Padding(MarginSizeValue)
      };
      contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, InputPanelWidthValue));
      contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, FullPercentSizeValue));
      contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, FullPercentSizeValue));
      contentLayout.Controls.Add(BuildInputPanel(), 0, 0);
      contentLayout.Controls.Add(BuildChartPanel(), 1, 0);
      rootLayout.Controls.Add(contentLayout, 0, 1);

      Controls.Add(rootLayout);
    }

    private MenuStrip BuildMenuStrip()
    {
      MenuStrip menuStrip = new MenuStrip();
      ToolStripMenuItem actionsMenu = new ToolStripMenuItem("Действия");
      actionsMenu.DropDownItems.Add(new ToolStripMenuItem("Рассчитать", null, (_, _) => HandleCalculate()));
      actionsMenu.DropDownItems.Add(new ToolStripMenuItem("Очистить", null, (_, _) => HandleClear()));
      actionsMenu.DropDownItems.Add(new ToolStripSeparator());

      stepBackwardItem = new ToolStripMenuItem("Шаг назад", null, (_, _) => ChangeStep(-1));
      stepForwardItem = new ToolStripMenuItem("Шаг вперед", null, (_, _) => ChangeStep(1));
      toStartItem = new ToolStripMenuItem("В начало", null, (_, _) => SetStep(0));
      toEndItem = new ToolStripMenuItem("В конец", null, (_, _) => SetStep(currentTrajectory.Count - 1));

      actionsMenu.DropDownItems.Add(stepBackwardItem);
      actionsMenu.DropDownItems.Add(stepForwardItem);
      actionsMenu.DropDownItems.Add(toStartItem);
      actionsMenu.DropDownItems.Add(toEndItem);

      actionsMenu.DropDownItems.Add(new ToolStripSeparator());
      actionsMenu.DropDownItems.Add(new ToolStripMenuItem("Выход", null, (_, _) => Close()));
      menuStrip.Items.Add(actionsMenu);
      MainMenuStrip = menuStrip;
      return menuStrip;
    }

    private void UpdateStepMenuState()
    {
      bool hasTrajectory = currentTrajectory.Count > 0;
      stepBackwardItem.Enabled = hasTrajectory && currentStepIndex > 0;
      stepForwardItem.Enabled = hasTrajectory && currentStepIndex < currentTrajectory.Count - 1;
      toStartItem.Enabled = hasTrajectory && currentStepIndex > 0;
      toEndItem.Enabled = hasTrajectory && currentStepIndex < currentTrajectory.Count - 1;
    }

    private void ChangeStep(int delta)
    {
      SetStep(currentStepIndex + delta);
    }

    private void SetStep(int newIndex)
    {
      if (newIndex >= 0 && newIndex < currentTrajectory.Count)
      {
        currentStepIndex = newIndex;
        UpdateChart(currentTargetFunction, currentIntervalStart, currentIntervalEnd);
        DisplayResult();
        UpdateStepMenuState();
      }
    }

    private Control BuildInputPanel()
    {
      TableLayoutPanel panel = new TableLayoutPanel
      {
        Dock = DockStyle.Fill,
        ColumnCount = 2,
        AutoScroll = true
      };
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, LabelColumnWidthValue));
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, FullPercentSizeValue));

      intervalStartField = CreateTextField(DefaultIntervalStartValue.ToString(CultureInfo.InvariantCulture));
      intervalEndField = CreateTextField(DefaultIntervalEndValue.ToString(CultureInfo.InvariantCulture));
      precisionField = CreateTextField(DefaultPrecisionValue.ToString("G", CultureInfo.InvariantCulture));
      formulaField = CreateTextField(DefaultFormulaText);

      modeSelector = new ComboBox
      {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList
      };
      modeSelector.Items.AddRange(new object[] { "Минимум функции" });
      modeSelector.SelectedIndex = ModeDefaultIndex;
      modeSelector.Enabled = false;

      AddLabeledRow(panel, "Начало (a):", intervalStartField);
      AddLabeledRow(panel, "Конец (b):", intervalEndField);
      AddLabeledRow(panel, "Точность (e):", precisionField);
      AddLabeledRow(panel, "f(x):", formulaField);
      AddLabeledRow(panel, "Что ищем:", modeSelector);

      resultLabel = new Label
      {
        Text = "Результат не рассчитан.",
        AutoSize = false,
        Dock = DockStyle.Fill,
        BorderStyle = BorderStyle.FixedSingle,
        Padding = new Padding(MarginSizeValue),
        Font = new Font(FontFamily.GenericSansSerif, ResultLabelFontSizeValue, FontStyle.Regular)
      };
      AddSpanRow(panel, resultLabel, ResultLabelHeightValue);

      return panel;
    }

    private static TextBox CreateTextField(string text)
    {
      return new TextBox { Text = text, Dock = DockStyle.Fill };
    }

    private void AddLabeledRow(TableLayoutPanel panel, string caption, Control input)
    {
      int rowIndex = panel.RowCount;
      panel.RowCount = rowIndex + 1;
      panel.RowStyles.Add(new RowStyle(SizeType.Absolute, RowHeightValue));
      Label captionLabel = new Label
      {
        Text = caption,
        TextAlign = ContentAlignment.MiddleLeft,
        Dock = DockStyle.Fill
      };
      panel.Controls.Add(captionLabel, 0, rowIndex);
      input.Dock = DockStyle.Fill;
      panel.Controls.Add(input, 1, rowIndex);
    }

    private void AddSpanRow(TableLayoutPanel panel, Control control, int height)
    {
      int rowIndex = panel.RowCount;
      panel.RowCount = rowIndex + 1;
      panel.RowStyles.Add(new RowStyle(SizeType.Absolute, height));
      control.Dock = DockStyle.Fill;
      panel.Controls.Add(control, 0, rowIndex);
      panel.SetColumnSpan(control, 2);
    }

    private Control BuildChartPanel()
    {
      functionPictureBox = new PictureBox
      {
        Dock = DockStyle.Fill,
        BackColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle
      };
      return functionPictureBox;
    }

    private void HandleCalculate()
    {
      try
      {
        currentIntervalStart = ParseDouble(intervalStartField.Text, "Начало интервала (a)");
        currentIntervalEnd = ParseDouble(intervalEndField.Text, "Конец интервала (b)");
        double precision = ParseDouble(precisionField.Text, "Точность (e)");

        if (!(currentIntervalEnd > currentIntervalStart))
          throw new ArgumentException("Конец интервала должен быть строго больше начала.");
        if (!(precision > 0.0))
          throw new ArgumentException("Точность должна быть положительной.");
        if (precision < MinimumAllowedPrecisionValue)
          throw new ArgumentException($"Точность должна быть не меньше {MinimumAllowedPrecisionValue:G}.");

        FormulaEvaluator evaluator = new FormulaEvaluator(formulaField.Text);
        evaluator.Evaluate(currentIntervalStart);
        evaluator.Evaluate(currentIntervalEnd);

        currentTargetFunction = argumentValue => evaluator.Evaluate(argumentValue);

        NewtonOptimizationResult result = NewtonOptimizer.FindMinimum(currentTargetFunction, currentIntervalStart, currentIntervalEnd, precision);

        currentTrajectory = result.Trajectory;
        currentSuccess = result.Success;
        currentMessage = result.Message;

        currentStepIndex = 0;

        UpdateChart(currentTargetFunction, currentIntervalStart, currentIntervalEnd);
        DisplayResult();
        UpdateStepMenuState();
      }
      catch (Exception exception)
      {
        MessageBox.Show(this, exception.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void UpdateChart(Func<double, double> targetFunction, double intervalStart, double intervalEnd)
    {
      int width = functionPictureBox.Width;
      int height = functionPictureBox.Height;
      if (width <= 0 || height <= 0 || currentTrajectory.Count == 0) return;

      Bitmap bitmap = new Bitmap(width, height);
      using (Graphics graphics = Graphics.FromImage(bitmap))
      {
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.White);

        double intervalSpan = intervalEnd - intervalStart;
        double padding = intervalSpan * ChartPaddingFactorValue;
        double minX = intervalStart - padding;
        double maxX = intervalEnd + padding;

        double minY = double.MaxValue;
        double maxY = double.MinValue;

        for (int sampleIndex = 0; sampleIndex <= ChartSampleCountValue; ++sampleIndex)
        {
          double sampleX = minX + (maxX - minX) * sampleIndex / ChartSampleCountValue;
          try
          {
            double sampleY = targetFunction(sampleX);
            if (double.IsNaN(sampleY) || double.IsInfinity(sampleY)) continue;
            if (sampleY < minY) minY = sampleY;
            if (sampleY > maxY) maxY = sampleY;
          }
          catch { }
        }

        if (minY == double.MaxValue) return;

        double yPadding = (maxY - minY) * ChartPaddingFactorValue;
        if (yPadding == 0) yPadding = 1;
        minY -= yPadding;
        maxY += yPadding;

        Func<double, double, PointF> toPixel = (x, y) =>
        {
          float px = (float)((x - minX) / (maxX - minX) * width);
          float py = (float)(height - (y - minY) / (maxY - minY) * height);
          return new PointF(px, py);
        };

        using (Font labelFont = new Font("Arial", LabelFontSizeValue))
        using (Font tickFont = new Font("Arial", TickFontSizeValue))
        using (SolidBrush textBrush = new SolidBrush(Color.Black))
        using (SolidBrush gridTextBrush = new SolidBrush(Color.DimGray))
        using (Pen axisPen = new Pen(Color.Black, AxisLineWidthValue))
        using (Pen gridPen = new Pen(Color.FromArgb(230, 230, 230), 1))
        using (Pen intervalPen = new Pen(Color.SeaGreen, IntervalLineWidthValue)
        { DashPattern = new float[] { IntervalDashPatternValue, IntervalDashPatternValue } })
        using (Pen tangentPen = new Pen(Color.Red, TangentLineWidthValue)
        { DashPattern = new float[] { TangentDashPatternValue, TangentDashPatternValue } })
        using (Pen functionPen = new Pen(Color.SteelBlue, 2))
        using (Brush pointBrush = new SolidBrush(Color.Crimson))
        using (Brush trajectoryBrush = new SolidBrush(Color.Orange))
        {
          double stepX = CalculateNiceStep(maxX - minX, TargetTickCountValue);
          double stepY = CalculateNiceStep(maxY - minY, TargetTickCountValue);

          float yAxisX = (float)((0 - minX) / (maxX - minX) * width);
          float xAxisY = (float)(height - (0 - minY) / (maxY - minY) * height);

          if (yAxisX < 0) yAxisX = 0; if (yAxisX > width) yAxisX = width;
          if (xAxisY < 0) xAxisY = 0; if (xAxisY > height) xAxisY = height;

          for (double tickX = Math.Ceiling(minX / stepX) * stepX; tickX <= maxX; tickX += stepX)
          {
            float px = toPixel(tickX, 0).X;
            graphics.DrawLine(gridPen, px, 0, px, height);
            graphics.DrawLine(axisPen, px, xAxisY - TickMarkLengthValue, px, xAxisY + TickMarkLengthValue);

            string label = tickX.ToString(TickNumberFormat, CultureInfo.InvariantCulture);
            SizeF textSize = graphics.MeasureString(label, tickFont);

            float textY = xAxisY + TickMarkLengthValue + 2;
            if (textY + textSize.Height > height)
              textY = xAxisY - TickMarkLengthValue - textSize.Height - 2;

            graphics.DrawString(label, tickFont, gridTextBrush, px - textSize.Width / 2, textY);
          }

          for (double tickY = Math.Ceiling(minY / stepY) * stepY; tickY <= maxY; tickY += stepY)
          {
            float py = toPixel(0, tickY).Y;
            graphics.DrawLine(gridPen, 0, py, width, py);
            graphics.DrawLine(axisPen, yAxisX - TickMarkLengthValue, py, yAxisX + TickMarkLengthValue, py);

            string label = tickY.ToString(TickNumberFormat, CultureInfo.InvariantCulture);
            SizeF textSize = graphics.MeasureString(label, tickFont);

            float textX = yAxisX - textSize.Width - TickMarkLengthValue - 2;
            if (textX < 0)
              textX = yAxisX + TickMarkLengthValue + 2;

            graphics.DrawString(label, tickFont, gridTextBrush, textX, py - textSize.Height / 2);
          }

          graphics.DrawLine(axisPen, 0, xAxisY, width, xAxisY);
          graphics.DrawLine(axisPen, yAxisX, 0, yAxisX, height);

          float xTitleY = xAxisY + AxisLabelOffsetValue + 5;
          if (xTitleY + 20 > height) xTitleY = xAxisY - AxisLabelOffsetValue - 20;
          graphics.DrawString("X", labelFont, textBrush, width - AxisLabelOffsetValue - 20, xTitleY);

          float yTitleX = yAxisX + AxisLabelOffsetValue;
          if (yTitleX > width - 20) yTitleX = yAxisX - AxisLabelOffsetValue - 15;
          graphics.DrawString("Y", labelFont, textBrush, yTitleX, AxisLabelOffsetValue);

          float startPixelX = toPixel(intervalStart, 0).X;
          float endPixelX = toPixel(intervalEnd, 0).X;

          graphics.DrawLine(intervalPen, startPixelX, 0, startPixelX, height);
          graphics.DrawLine(intervalPen, endPixelX, 0, endPixelX, height);

          float intervalLabelY = IntervalLabelOffsetValue + 15;

          string aLabel = $"a = {intervalStart:G4}";
          graphics.DrawString(aLabel, labelFont, textBrush, startPixelX + AxisLabelOffsetValue, intervalLabelY);

          string bLabel = $"b = {intervalEnd:G4}";
          SizeF bSize = graphics.MeasureString(bLabel, labelFont);
          float bX = endPixelX - bSize.Width - AxisLabelOffsetValue;
          if (bX < 0) bX = 0;
          graphics.DrawString(bLabel, labelFont, textBrush, bX, intervalLabelY);

          PointF? previousPoint = null;
          for (int sampleIndex = 0; sampleIndex <= ChartSampleCountValue; ++sampleIndex)
          {
            double sampleX = minX + (maxX - minX) * sampleIndex / ChartSampleCountValue;
            try
            {
              double sampleY = targetFunction(sampleX);
              if (double.IsNaN(sampleY) || double.IsInfinity(sampleY))
              {
                previousPoint = null;
                continue;
              }
              PointF currentPoint = toPixel(sampleX, sampleY);
              if (previousPoint.HasValue)
              {
                graphics.DrawLine(functionPen, previousPoint.Value, currentPoint);
              }
              previousPoint = currentPoint;
            }
            catch { previousPoint = null; }
          }

          double currentXPoint = currentTrajectory[currentStepIndex];
          double currentYPoint = targetFunction(currentXPoint);
          double derivative = CalculateDerivative(targetFunction, currentXPoint);

          double tangentYAtMinX = currentYPoint + derivative * (minX - currentXPoint);
          double tangentYAtMaxX = currentYPoint + derivative * (maxX - currentXPoint);

          PointF tangentStart = toPixel(minX, tangentYAtMinX);
          PointF tangentEnd = toPixel(maxX, tangentYAtMaxX);
          graphics.DrawLine(tangentPen, tangentStart, tangentEnd);

          for (int stepIndex = 0; stepIndex < currentTrajectory.Count; ++stepIndex)
          {
            double stepXValue = currentTrajectory[stepIndex];
            double stepYValue = targetFunction(stepXValue);
            PointF trajectoryPoint = toPixel(stepXValue, stepYValue);

            if (stepIndex == currentStepIndex)
            {
              graphics.FillEllipse(pointBrush,
                  trajectoryPoint.X - MarkerRadiusValue,
                  trajectoryPoint.Y - MarkerRadiusValue,
                  MarkerRadiusValue * 2,
                  MarkerRadiusValue * 2);
            }
            else if (stepIndex < currentStepIndex)
            {
              graphics.FillEllipse(trajectoryBrush,
                  trajectoryPoint.X - TrajectoryRadiusValue,
                  trajectoryPoint.Y - TrajectoryRadiusValue,
                  TrajectoryRadiusValue * 2,
                  TrajectoryRadiusValue * 2);
            }
          }
        }
      }
      functionPictureBox.Image?.Dispose();
      functionPictureBox.Image = bitmap;
    }

    private static double CalculateDerivative(Func<double, double> function, double point)
    {
      return (function(point + DerivativeStepValue) - function(point - DerivativeStepValue)) / (2.0 * DerivativeStepValue);
    }

    private static double CalculateNiceStep(double range, double targetStepCount)
    {
      if (range <= 0) return 1.0;

      double rawStep = range / targetStepCount;
      double magnitude = Math.Pow(10, Math.Floor(Math.Log10(rawStep)));
      double normalizedStep = rawStep / magnitude;

      double niceStep;
      if (normalizedStep <= 1.5) niceStep = 1;
      else if (normalizedStep <= 3) niceStep = 2;
      else if (normalizedStep <= 7) niceStep = 5;
      else niceStep = 10;

      return niceStep * magnitude;
    }

    private void DisplayResult()
    {
      if (currentTrajectory.Count == 0) return;

      double currentPoint = currentTrajectory[currentStepIndex];
      double currentValue = currentTargetFunction(currentPoint);
      int totalSteps = currentTrajectory.Count - 1;

      string finalMessage = currentStepIndex == totalSteps ? "\n" + currentMessage : "";

      resultLabel.Text =
          $"Метод Ньютона:\n" +
          $"Пройдено шагов: {currentStepIndex} из {totalSteps}\n" +
          $"x* = {currentPoint.ToString(ResultNumberFormat, CultureInfo.InvariantCulture)}\n" +
          $"f(x*) = {currentValue.ToString(ResultNumberFormat, CultureInfo.InvariantCulture)}" +
          finalMessage;
    }

    private void HandleClear()
    {
      intervalStartField.Text = DefaultIntervalStartValue.ToString(CultureInfo.InvariantCulture);
      intervalEndField.Text = DefaultIntervalEndValue.ToString(CultureInfo.InvariantCulture);
      precisionField.Text = DefaultPrecisionValue.ToString("G", CultureInfo.InvariantCulture);
      formulaField.Text = string.Empty;

      currentTrajectory.Clear();
      currentStepIndex = -1;

      functionPictureBox.Image?.Dispose();
      functionPictureBox.Image = null;
      resultLabel.Text = "Результат не рассчитан.";
      UpdateStepMenuState();
    }

    private static double ParseDouble(string text, string fieldName)
    {
      if (string.IsNullOrWhiteSpace(text))
        throw new ArgumentException($"Поле «{fieldName}» не заполнено.");

      string normalized = text.Replace(',', '.');
      if (!double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedValue))
        throw new ArgumentException($"Поле «{fieldName}» содержит некорректное число: '{text}'.");

      if (double.IsNaN(parsedValue) || double.IsInfinity(parsedValue))
        throw new ArgumentException($"Поле «{fieldName}» не может быть NaN или бесконечностью.");

      return parsedValue;
    }
  }
}