using System;
using System.Drawing;
using System.Windows.Forms;

public class CustomScrubBar : Control
{
    private const int PaddingOffset = 10;
    private const double PrecisionFactor = 0.1;

    public int Min { get; set; } = 0;
    public int Max { get; set; } = 1000; // Full range for precision
    public int StartTrim { get; set; } = 0;
    public int EndTrim { get; set; } = 1000;
    
    public long TotalDurationMs { get; set; } = 180000; // Default: 3 minutes

    public Action<string>? OnStartTrimChanged;
    public Action<string>? OnEndTrimChanged;

    private bool draggingStart = false;
    private bool draggingEnd = false;

    public CustomScrubBar()
    {
        this.DoubleBuffered = true;
        this.Height = 30;
        this.MouseDown += ScrubBar_MouseDown;
        this.MouseMove += ScrubBar_MouseMove;
        this.MouseUp += ScrubBar_MouseUp;
        this.Resize += (s, e) => Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        int barHeight = 10;
        int handleSize = 8;

        int availableWidth = Width - (PaddingOffset * 2);

        int startX = PaddingOffset + (int)((StartTrim - Min) * availableWidth / (double)(Max - Min));
        int endX = PaddingOffset + (int)((EndTrim - Min) * availableWidth / (double)(Max - Min));

        g.FillRectangle(Brushes.Gray, PaddingOffset, (Height - barHeight) / 2, availableWidth, barHeight);
        g.FillRectangle(Brushes.LightBlue, startX, (Height - barHeight) / 2, endX - startX, barHeight);
        g.FillRectangle(Brushes.Red, startX - handleSize / 2, (Height - handleSize) / 2, handleSize, handleSize);
        g.FillRectangle(Brushes.Red, endX - handleSize / 2, (Height - handleSize) / 2, handleSize, handleSize);
    }

    private void ScrubBar_MouseDown(object? sender, MouseEventArgs e)
    {
        int availableWidth = Width - (PaddingOffset * 2);
        int startX = PaddingOffset + (int)((StartTrim - Min) * availableWidth / (double)(Max - Min));
        int endX = PaddingOffset + (int)((EndTrim - Min) * availableWidth / (double)(Max - Min));
        int handleSize = 8;

        if (Math.Abs(e.X - startX) < handleSize)
            draggingStart = true;
        else if (Math.Abs(e.X - endX) < handleSize)
            draggingEnd = true;
    }

    private void ScrubBar_MouseMove(object? sender, MouseEventArgs e)
    {
        int availableWidth = Width - (PaddingOffset * 2);
        
        if (draggingStart)
        {
            double newStart = Min + ((e.X - PaddingOffset) * (Max - Min) / (double)availableWidth);
            StartTrim = (int)(Math.Round(newStart / PrecisionFactor) * PrecisionFactor);
            StartTrim = Math.Max(Min, Math.Min(EndTrim - 1, StartTrim));
            Invalidate();
            OnStartTrimChanged?.Invoke(FormatTime((StartTrim * TotalDurationMs) / Max));
        }
        else if (draggingEnd)
        {
            double newEnd = Min + ((e.X - PaddingOffset) * (Max - Min) / (double)availableWidth);
            EndTrim = (int)(Math.Round(newEnd / PrecisionFactor) * PrecisionFactor);
            EndTrim = Math.Max(StartTrim + 1, Math.Min(Max, EndTrim));
            Invalidate();
            OnEndTrimChanged?.Invoke(FormatTime((EndTrim * TotalDurationMs) / Max));
        }
    }

    private void ScrubBar_MouseUp(object? sender, MouseEventArgs e)
    {
        draggingStart = draggingEnd = false;
    }

    private string FormatTime(long milliseconds){
        TimeSpan time = TimeSpan.FromMilliseconds(milliseconds);
        return $"{time.Hours:D2}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
    }

    public void ResetTrim()
    {
        StartTrim = 0;
        EndTrim = 1000;
        Invalidate();
        OnStartTrimChanged?.Invoke(FormatTime(0));
        OnEndTrimChanged?.Invoke(FormatTime((TotalDurationMs * EndTrim) / Max));
    }
}
