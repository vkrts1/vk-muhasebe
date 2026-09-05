using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using SkiaSharp;

namespace ErmayMuhasebe.Services
{
    public static class ChartHelper
    {
        // 3x ölçek faktörü: PDF'de kristal netlikte görüntü
        private const float Scale = 3f;

        // ═══════════════════════════════════════════════════════════════
        // 1. ÇİFT SERİLİ BAR CHART (Gerçekleşen vs Hedef)
        // ═══════════════════════════════════════════════════════════════
        public static byte[] GetBarChartImage(int width, int height, List<ChartDataItem> data, string labelHeader = "", string actualLabel = "Gerçekleşen", string targetLabel = "Hedef")
        {
            if (data == null || !data.Any()) return Array.Empty<byte>();

            actualLabel = actualLabel ?? "Gerçekleşen";
            targetLabel = targetLabel ?? "Hedef";
            labelHeader = labelHeader ?? "";

            int effectiveWidth = Math.Max(width, data.Count * 55 + 120);
            int pxW = (int)(effectiveWidth * Scale);
            int pxH = (int)(height * Scale);

            var info = new SKImageInfo(pxW, pxH);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(Scale);

            float mL = 65, mR = 15, mT = 35, mB = 35;
            float chartW = effectiveWidth - mL - mR;
            float chartH = height - mT - mB;
            float x0 = mL, y0 = height - mB;

            decimal maxVal = data.Max(x => Math.Max(x.Target, x.Actual));
            if (maxVal <= 0) maxVal = 1;
            float maxF = (float)maxVal * 1.15f;

#pragma warning disable CS0618
            using var pYTxt = MkPaint("#64748b", 9);
            using var pXTxt = MkPaint("#334155", 9, bold: true);
            using var pLeg = MkPaint("#334155", 9);
            using var pValTxt = MkPaint("#1e40af", 7, bold: true);
#pragma warning restore CS0618

            using var pGrid = new SKPaint { Color = SKColor.Parse("#e2e8f0"), StrokeWidth = 0.5f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 4f, 3f }, 0) };
            using var pAct = new SKPaint { Color = SKColor.Parse("#3b82f6"), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var pTgt = new SKPaint { Color = SKColor.Parse("#f97316"), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var pBase = new SKPaint { Color = SKColor.Parse("#94a3b8"), StrokeWidth = 1f, IsAntialias = true };

            // Grid
            for (int i = 0; i <= 5; i++)
            {
                float val = maxF * i / 5;
                float y = y0 - (chartH * i / 5);
                if (i > 0) canvas.DrawLine(x0, y, x0 + chartW, y, pGrid);
                string lbl = FmtAxis(val, maxF);
                float tw = pYTxt.MeasureText(lbl);
                canvas.DrawText(lbl, x0 - tw - 6, y + 4, pYTxt);
            }
            canvas.DrawLine(x0, y0, x0 + chartW, y0, pBase);

            // Bars
            float slot = chartW / data.Count;
            float bw = Math.Min(slot * 0.28f, 22);

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                float cx = x0 + i * slot + slot / 2;

                float hA = item.Actual > 0 ? (float)item.Actual / maxF * chartH : 0;
                if (hA > 0 && hA < 3) hA = 3;
                canvas.DrawRoundRect(new SKRect(cx - bw - 1, y0 - hA, cx - 1, y0), 2, 2, pAct);

                float hT = item.Target > 0 ? (float)item.Target / maxF * chartH : 0;
                if (hT > 0 && hT < 3) hT = 3;
                canvas.DrawRoundRect(new SKRect(cx + 1, y0 - hT, cx + bw + 1, y0), 2, 2, pTgt);

                string dl = MonthName(item.Label) ?? "";
                float lw = pXTxt.MeasureText(dl);
                canvas.DrawText(dl, cx - lw / 2, y0 + 14, pXTxt);

                // Değer etiketlerini barların üzerine ekle
                if (item.Actual > 0)
                {
                    string actTxt = FmtAxis((float)item.Actual, maxF);
                    float aw = pValTxt.MeasureText(actTxt);
                    canvas.DrawText(actTxt, cx - bw/2 - aw/2 - 0.5f, y0 - hA - 5, pValTxt);
                }
                if (item.Target > 0)
                {
                    string tgtTxt = FmtAxis((float)item.Target, maxF);
                    float tw_val = pValTxt.MeasureText(tgtTxt);
                    canvas.DrawText(tgtTxt, cx + bw/2 - tw_val/2 + 0.5f, y0 - hT - 5, pValTxt);
                }
            }

            // Legend
            float ly = 16;
            canvas.DrawRoundRect(new SKRect(x0, ly - 8, x0 + 12, ly), 2, 2, pAct);
            canvas.DrawText(actualLabel, x0 + 15, ly, pLeg);
            canvas.DrawRoundRect(new SKRect(x0 + 95, ly - 8, x0 + 107, ly), 2, 2, pTgt);
            canvas.DrawText(targetLabel, x0 + 110, ly, pLeg);

            return Encode(surface);
        }

        // ═══════════════════════════════════════════════════════════════
        // 2. TEK SERİLİ BAR CHART
        // ═══════════════════════════════════════════════════════════════
        public static byte[] GetSingleBarChartImage(int width, int height, List<(string Label, decimal Value)> data, string title = "", string barColor = "#3b82f6")
        {
            if (data == null || !data.Any()) return Array.Empty<byte>();

            title = title ?? "";
            barColor = barColor ?? "#3b82f6";

            int effectiveWidth = Math.Max(width, data.Count * 80 + 120);
            int pxW = (int)(effectiveWidth * Scale);
            int pxH = (int)(height * Scale);

            var info = new SKImageInfo(pxW, pxH);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(Scale);

            float mL = 65, mR = 15, mT = 30, mB = 40;
            float chartW = effectiveWidth - mL - mR;
            float chartH = height - mT - mB;
            float x0 = mL, y0 = height - mB;

            decimal maxVal = data.Max(x => Math.Abs(x.Value));
            if (maxVal <= 0) maxVal = 1;
            float maxF = (float)maxVal * 1.15f;

#pragma warning disable CS0618
            using var pYTxt = MkPaint("#64748b", 9);
            using var pXTxt = MkPaint("#334155", 8);
            using var pTitle = MkPaint("#1e3a8a", 10, bold: true);
            using var pValTxt = MkPaint("#1e40af", 7, bold: true);
#pragma warning restore CS0618

            using var pGrid = new SKPaint { Color = SKColor.Parse("#e2e8f0"), StrokeWidth = 0.5f, IsAntialias = true, PathEffect = SKPathEffect.CreateDash(new[] { 4f, 3f }, 0) };
            using var pBar = new SKPaint { Color = SKColor.Parse(barColor), IsAntialias = true, Style = SKPaintStyle.Fill };
            using var pBase = new SKPaint { Color = SKColor.Parse("#94a3b8"), StrokeWidth = 1f, IsAntialias = true };

            if (!string.IsNullOrEmpty(title))
                canvas.DrawText(title, x0, 16, pTitle);

            // Grid
            for (int i = 0; i <= 5; i++)
            {
                float val = maxF * i / 5;
                float y = y0 - (chartH * i / 5);
                if (i > 0) canvas.DrawLine(x0, y, x0 + chartW, y, pGrid);
                string lbl = FmtAxis(val, maxF);
                float tw = pYTxt.MeasureText(lbl);
                canvas.DrawText(lbl, x0 - tw - 6, y + 4, pYTxt);
            }
            canvas.DrawLine(x0, y0, x0 + chartW, y0, pBase);

            // Bars
            float slot = chartW / data.Count;
            float bw = Math.Min(slot * 0.55f, 40);

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                float cx = x0 + i * slot + slot / 2;

                float h = item.Value > 0 ? (float)item.Value / maxF * chartH : 0;
                if (h > 0 && h < 3) h = 3;
                canvas.DrawRoundRect(new SKRect(cx - bw / 2, y0 - h, cx + bw / 2, y0), 3, 3, pBar);

                // Değer yazısı bar üstünde
                if (item.Value > 0)
                {
                    string vTxt = FmtAxis((float)item.Value, maxF);
                    float vw = pValTxt.MeasureText(vTxt);
                    canvas.DrawText(vTxt, cx - vw / 2, y0 - h - 5, pValTxt);
                }

                // Etiket
                string dl = item.Label ?? "";
                float lw = pXTxt.MeasureText(dl);
                canvas.DrawText(dl, cx - lw / 2, y0 + 14, pXTxt);
            }

            return Encode(surface);
        }

        // ═══════════════════════════════════════════════════════════════
        // 3. YATAY BAR CHART
        // ═══════════════════════════════════════════════════════════════
        public static byte[] GetHorizontalBarChartImage(int width, int height, List<(string Label, decimal Value)> data, string title = "", string barColor = "#8b5cf6")
        {
            if (data == null || !data.Any()) return Array.Empty<byte>();

            title = title ?? "";
            barColor = barColor ?? "#8b5cf6";

            int rowH = 24;
            int effectiveHeight = Math.Max(height, data.Count * rowH + 40);
            int pxW = (int)(width * Scale);
            int pxH = (int)(effectiveHeight * Scale);

            var info = new SKImageInfo(pxW, pxH);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.White);
            canvas.Scale(Scale);

            float labelAreaW = 130, mR = 55, mT = 12;
            float barAreaW = width - labelAreaW - mR;

#pragma warning disable CS0618
            using var pLabel = MkPaint("#334155", 8);
            using var pTitle = MkPaint("#1e3a8a", 10, bold: true);
            using var pVal = MkPaint("#475569", 8);
#pragma warning restore CS0618

            using var pBar = new SKPaint { Color = SKColor.Parse(barColor), IsAntialias = true, Style = SKPaintStyle.Fill };

            if (!string.IsNullOrEmpty(title))
            {
                canvas.DrawText(title, 10, 14, pTitle);
                mT += 14;
            }

            decimal maxVal = data.Max(x => Math.Abs(x.Value));
            if (maxVal <= 0) maxVal = 1;

            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                float y = mT + i * rowH;

                canvas.DrawText(item.Label ?? "", 5, y + 15, pLabel);

                float bLen = (float)(Math.Abs(item.Value) / maxVal) * barAreaW;
                if (bLen < 3 && item.Value != 0) bLen = 3;
                canvas.DrawRoundRect(new SKRect(labelAreaW, y + 4, labelAreaW + bLen, y + rowH - 4), 3, 3, pBar);

                string vTxt = FmtAxis((float)Math.Abs(item.Value), (float)maxVal);
                canvas.DrawText(vTxt, labelAreaW + bLen + 5, y + 15, pVal);
            }

            return Encode(surface);
        }

        // ═══════════════════════════════════════════════════════════════
        // Yardımcı Fonksiyonlar
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>Y ekseni ve değer formatı. Tekrar etmeyen, anlamlı etiketler üretir.</summary>
        private static string FmtAxis(float val, float maxVal)
        {
            if (maxVal >= 1_000_000)
                return $"{val / 1_000_000:N1}M";
            if (maxVal >= 10_000)
                return $"{val / 1_000:N1}K";
            if (maxVal >= 1_000)
                return $"{val / 1_000:N2}K";
            if (maxVal >= 100)
                return $"{val:N0}";
            if (maxVal >= 10)
                return $"{val:N1}";
            return $"{val:N2}";
        }

        private static string MonthName(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            if (int.TryParse(input, out int m) && m >= 1 && m <= 12)
                return new[] { "", "Oca", "Şub", "Mar", "Nis", "May", "Haz", "Tem", "Ağu", "Eyl", "Eki", "Kas", "Ara" }[m];
            if (input.All(char.IsDigit)) return $"H{input}";
            return input;
        }

#pragma warning disable CS0618
        private static SKPaint MkPaint(string color, float size, bool bold = false)
        {
            return new SKPaint
            {
                Color = SKColor.Parse(color),
                IsAntialias = true,
                TextSize = size,
                Typeface = SKTypeface.FromFamilyName("Arial", bold ? SKFontStyle.Bold : SKFontStyle.Normal)
            };
        }
#pragma warning restore CS0618

        private static byte[] Encode(SKSurface surface)
        {
            using var image = surface.Snapshot();
            using var d = image.Encode(SKEncodedImageFormat.Png, 100);
            return d.ToArray();
        }
    }
}
