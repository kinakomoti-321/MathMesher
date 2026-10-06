using System;
using System.Collections.Generic;

namespace MathMesher.Editor.Engine
{
    internal sealed partial class Typesetter
    {
        private sealed class CellLayout
        {
            public ArrayCell Cell;
            public int Column;
            public Box Box;
        }

        private char CellAlign(ArrayNode a, int col, int row, int rows, ArrayCell cell)
        {
            if (cell?.Align != null) return cell.Align.Value;
            if (a.Columns != null && col < a.Columns.Count) return a.Columns[col].Align;
            switch (a.ColumnMode)
            {
                case "align": return col % 2 == 0 ? 'r' : 'l';
                case "eqnarray": return col == 0 ? 'r' : col == 1 ? 'c' : 'l';
                case "cases": return 'l';
                case "multline": return rows <= 1 ? 'c' : row == 0 ? 'l' : row == rows - 1 ? 'r' : 'c';
                default: return a.DefaultAlign;
            }
        }

        private Box MakeArray(ArrayNode a, Options o)
        {
            Options cellO;
            if (a.CellStyle.HasValue) cellO = o.WithStyle(new MathStyle(a.CellStyle.Value * 2));
            else if (a.ColumnMode == "array" || a.ColumnMode == "matrix" || a.ColumnMode == "cases") cellO = o.WithStyle(MathStyle.Text);
            else cellO = o;
            float cs = cellO.Scale;
            int rows = a.Rows.Count;
            if (rows == 0) return new Box();

            // ---- build cells
            var layout = new List<List<CellLayout>>();
            int cols = a.Columns?.Count ?? 0;
            foreach (var row in a.Rows)
            {
                var line = new List<CellLayout>();
                int c = 0;
                foreach (var cell in row)
                {
                    var body = cell.Body;
                    if ((a.ColumnMode == "align" || a.ColumnMode == "eqnarray") && c % 2 == 1 && cell.Span == 1)
                    {
                        body = new List<Node>(body);
                        body.Insert(0, new GroupNode());
                    }
                    Box cellBox;
                    if (body.Count == 1 && body[0] is CDArrowNode cd) cellBox = MakeCDArrow(cd, cellO, 0, 0);
                    else if (body.Count == 1 && body[0] is HDotsForNode) cellBox = new Box();
                    else cellBox = BuildList(body, cellO);
                    line.Add(new CellLayout { Cell = cell, Column = c, Box = cellBox });
                    c += Math.Min(cell.Span, 1000);
                }
                layout.Add(line);
                cols = Math.Max(cols, Math.Min(c, 1000));
            }
            if (cols == 0) cols = 1;

            // ---- horizontal padding / gaps
            float colSep = a.ColSep * cs;
            var leftPad = new float[cols];
            var rightPad = new float[cols];
            var befores = new Box[cols];
            for (int c = 0; c < cols; c++)
            {
                var spec = a.Columns != null && c < a.Columns.Count ? a.Columns[c] : null;
                leftPad[c] = c == 0 ? (a.OuterColSep ? colSep : 0) : colSep;
                rightPad[c] = c == cols - 1 ? (a.OuterColSep ? colSep : 0) : colSep;
                if (spec?.Before != null)
                {
                    befores[c] = BuildList(spec.Before, cellO);
                    leftPad[c] = 0;
                    if (c > 0) rightPad[c - 1] = 0;
                }
                if (a.ColumnMode == "align" && c > 0 && c % 2 == 0) leftPad[c] += a.AlignGap * cs;
                if (a.ColumnMode == "cases" && c > 0) leftPad[c] += 1f * cs;
            }

            // ---- column widths
            var colW = new float[cols];
            for (int c = 0; c < cols; c++)
            {
                var spec = a.Columns != null && c < a.Columns.Count ? a.Columns[c] : null;
                if (spec?.Width != null) colW[c] = spec.Width.Value * cellO.SizeScale;
            }
            foreach (var line in layout)
                foreach (var cl in line)
                    if (cl.Cell.Span == 1 && cl.Column < cols) colW[cl.Column] = Math.Max(colW[cl.Column], cl.Box.Width);
            foreach (var line in layout)
                foreach (var cl in line)
                {
                    if (cl.Cell.Span <= 1 || cl.Cell.Span >= 1000) continue;
                    int last = Math.Min(cols - 1, cl.Column + cl.Cell.Span - 1);
                    float avail = SpanWidth(cl.Column, last, colW, leftPad, rightPad, befores);
                    if (cl.Box.Width > avail) colW[last] += cl.Box.Width - avail;
                }

            // amscd: horizontal arrows stretch to their column width
            if (a.ColumnMode == "CD")
                foreach (var line in layout)
                    foreach (var cl in line)
                        if (cl.Cell.Body.Count == 1 && cl.Cell.Body[0] is CDArrowNode cd && (cd.Kind == '>' || cd.Kind == '<' || cd.Kind == '='))
                        {
                            cl.Box = MakeCDArrow(cd, cellO, colW[cl.Column] - 0.5f * cs, 0);
                            colW[cl.Column] = Math.Max(colW[cl.Column], cl.Box.Width);
                        }

            // column x positions (content start)
            var colX = new float[cols];
            float x = 0;
            var ruleXs = new List<(float x, HLineKind kind, int boundary)>();
            for (int c = 0; c < cols; c++)
            {
                var spec = a.Columns != null && c < a.Columns.Count ? a.Columns[c] : null;
                if (spec != null)
                    foreach (var r in spec.LeftRules) { ruleXs.Add((x, r, c)); x += 0.04f * cs + (spec.LeftRules.Count > 1 ? 0.2f * cs : 0); }
                if (befores[c] != null) x += befores[c].Width;
                x += leftPad[c];
                colX[c] = x;
                x += colW[c] + rightPad[c];
            }
            foreach (var r in a.TrailingRules) { ruleXs.Add((x, r, cols)); x += 0.04f * cs + (a.TrailingRules.Count > 1 ? 0.2f * cs : 0); }
            float totalWidth = x;

            // ---- rows
            float arrayskip = a.ArrayStretch * 1.2f * cs;
            float strutH = 0.7f * arrayskip, strutD = 0.3f * arrayskip;
            if (a.ColumnMode == "CD") { strutH = 0; strutD = 0; }
            float jot = a.AddJot ? 0.3f * cs : 0;
            float rule = 0.04f * cs, doubleSep = 0.2f * cs;
            var rowY = new float[rows];
            var segBottom = new float[rows];
            var lineYs = new List<(float y, HLineKind kind, float x0, float x1)>();
            float cursor = 0;
            for (int r = 0; r <= rows; r++)
            {
                var hl = r < a.HLines.Count ? a.HLines[r] : null;
                var cl = r < a.CLines.Count ? a.CLines[r] : null;
                if (hl != null && hl.Count > 0)
                {
                    for (int i = 0; i < hl.Count; i++)
                    {
                        if (i > 0) cursor -= doubleSep;
                        lineYs.Add((cursor - rule / 2, hl[i], 0, totalWidth));
                        cursor -= rule;
                    }
                    if (r < rows) cursor -= 0.05f * cs;
                }
                if (cl != null && cl.Count > 0)
                {
                    foreach (var (from, to, kind) in cl)
                    {
                        int f = Math.Min(from, cols - 1), t = Math.Min(to, cols - 1);
                        lineYs.Add((cursor - rule / 2, kind, colX[f] - leftPad[f], colX[t] + colW[t] + rightPad[t]));
                    }
                    if (hl == null || hl.Count == 0) cursor -= rule;
                }
                if (r == rows) break;
                float h = strutH, d = strutD;
                foreach (var c in layout[r]) { h = Math.Max(h, c.Box.Height); d = Math.Max(d, c.Box.Depth); }
                if (a.Tags.Count > r && a.Tags[r] != null) { }
                cursor -= h;
                rowY[r] = cursor;
                cursor -= d;
                if (r < rows - 1) cursor -= jot + (r < a.RowGaps.Count ? a.RowGaps[r] * cellO.SizeScale : 0);
                segBottom[r] = cursor;
            }
            float bottom = cursor;

            // ---- place
            var box = new Box(totalWidth, 0, 0);
            float shift = a.VAlign switch
            {
                't' => -rowY[0],
                'b' => -rowY[rows - 1],
                _ => C.AxisHeight * o.Scale - bottom / 2,
            };
            for (int r = 0; r < rows; r++)
            {
                foreach (var cl in layout[r])
                {
                    int span = Math.Min(cl.Cell.Span, cols - cl.Column);
                    int last = cl.Column + span - 1;
                    float start = colX[cl.Column];
                    float width = cl.Cell.Span >= 1000 ? totalWidth - start : SpanWidth(cl.Column, last, colW, leftPad, rightPad, befores) ;
                    if (cl.Cell.Body.Count == 1 && cl.Cell.Body[0] is HDotsForNode hd)
                    {
                        AddDots(box, start, width, rowY[r] + shift, hd, cellO);
                        continue;
                    }
                    char align = CellAlign(a, cl.Column, r, rows, cl.Cell);
                    float cx = align == 'l' ? start : align == 'r' ? start + width - cl.Box.Width : start + (width - cl.Box.Width) / 2;
                    if (cl.Cell.Span >= 1000) cx = 0;
                    box.Add(cl.Box, cx, rowY[r] + shift);
                }
            }
            for (int c = 0; c < cols; c++)
                if (befores[c] != null)
                    for (int r = 0; r < rows; r++) box.Add(befores[c], colX[c] - leftPad[c] - befores[c].Width, rowY[r] + shift);
            foreach (var (ly, kind, x0, x1) in lineYs) AddHRule(box, x0, x1, ly + shift, rule, kind, o.Color, cs);
            foreach (var (rx, kind, boundary) in ruleXs)
            {
                // vertical rules are interrupted where a \multicolumn cell spans across them
                float top = 0;
                for (int r = 0; r < rows; r++)
                {
                    float segTop = r == 0 ? 0 : segBottom[r - 1];
                    float segBot = r == rows - 1 ? bottom : segBottom[r];
                    bool crossed = false;
                    foreach (var cl in layout[r])
                        if (cl.Column < boundary && cl.Column + Math.Min(cl.Cell.Span, 1000) > boundary) crossed = true;
                    if (crossed)
                    {
                        if (segTop < top) AddVRule(box, rx, top + shift, segTop + shift, rule, kind, o.Color, cs);
                        top = segBot;
                    }
                }
                if (bottom < top) AddVRule(box, rx, top + shift, bottom + shift, rule, kind, o.Color, cs);
            }

            // ---- tags
            float tagX = totalWidth + 2f * cs;
            var tagBoxes = new Box[rows];
            float tagW = 0;
            for (int r = 0; r < rows && r < a.Tags.Count; r++)
            {
                if (a.Tags[r] == null) continue;
                tagBoxes[r] = BuildText(a.Tags[r], o.WithText(true));
                tagW = Math.Max(tagW, tagBoxes[r].Width);
            }
            for (int r = 0; r < rows; r++)
                if (tagBoxes[r] != null) box.Add(tagBoxes[r], tagX + tagW - tagBoxes[r].Width, rowY[r] + shift);
            if (tagW > 0) box.Width = tagX + tagW;
            box.Fit(false);
            box.Height = Math.Max(box.Height, shift);
            box.Depth = Math.Max(box.Depth, -(bottom + shift));
            return box;
        }

        private static float SpanWidth(int first, int last, float[] colW, float[] leftPad, float[] rightPad, Box[] befores)
        {
            float w = colW[first];
            for (int c = first + 1; c <= last; c++) w += rightPad[c - 1] + (befores[c]?.Width ?? 0) + leftPad[c] + colW[c];
            return w;
        }

        private void AddDots(Box box, float x0, float width, float y, HDotsForNode hd, Options o)
        {
            float step = Math.Max(0.2f, hd.Spacing * 0.5f) * o.Scale;
            var dot = GlyphOf('.', o);
            int n = Math.Max(1, (int)Math.Floor(width / step));
            float used = (n - 1) * step;
            float start = x0 + (width - used - dot.Width) / 2;
            for (int i = 0; i < n; i++) box.Add(new GlyphBox(dot.Glyph, dot.Scale, o.Color), start + i * step, y);
        }

        private static void AddHRule(Box box, float x0, float x1, float y, float t, HLineKind kind, Rgba color, float s)
        {
            if (kind == HLineKind.Dashed)
            {
                float dash = 0.4f * s, gap = 0.4f * s;
                for (float x = x0; x < x1; x += dash + gap)
                    box.Add(new RuleBox(Math.Min(dash, x1 - x), t / 2, t / 2, color), x, y);
                return;
            }
            box.Add(new RuleBox(x1 - x0, t / 2, t / 2, color), x0, y);
        }

        private static void AddVRule(Box box, float x, float top, float bottom, float t, HLineKind kind, Rgba color, float s)
        {
            if (kind == HLineKind.Dashed)
            {
                float dash = 0.4f * s, gap = 0.4f * s;
                for (float y = top; y > bottom; y -= dash + gap)
                {
                    float len = Math.Min(dash, y - bottom);
                    box.Add(new RuleBox(t, 0, len, color), x, y);
                }
                return;
            }
            box.Add(new RuleBox(t, 0, top - bottom, color), x, top);
        }
    }
}
