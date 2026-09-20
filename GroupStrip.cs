using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WinCopy {
    // Paint a single scrollable row rather than clipping an overflowing FlowLayoutPanel.
    public class GroupStrip : Control {
        public event Action<string> Selected;
        public event Action<string[]> Reordered;
        readonly List<string> items = new List<string>();
        string active = "", pressed;
        int offset, insertion = -1;
        Point down;
        bool dragging;
        public bool ReorderEnabled;
        public GroupStrip() { DoubleBuffered = true; TabStop = true; Cursor = Cursors.Hand; AccessibleName = "分类与分组，可左右拖动排序"; }
        public void SetItems(IEnumerable<string> values, string selected) {
            var incoming = values.ToList();
            if (!items.SequenceEqual(incoming)) { items.Clear(); items.AddRange(incoming); }
            active = selected; ClampOffset(); Invalidate();
        }
        int ItemWidth(string text) { return Math.Min(180, Math.Max(46, TextRenderer.MeasureText(text, Font).Width + 20)); }
        int TotalWidth { get { return items.Sum(x => ItemWidth(x) + 4); } }
        int ViewWidth { get { return Math.Max(1, Width - (TotalWidth > Width ? 42 : 0)); } }
        void ClampOffset() { offset = Math.Max(0, Math.Min(offset, Math.Max(0, TotalWidth - ViewWidth))); }
        int Hit(int x) { int left = -offset; for (int i = 0; i < items.Count; i++) { int w = ItemWidth(items[i]); if (x >= left && x < left + w) return i; left += w + 4; } return -1; }
        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e); var state = e.Graphics.Save(); e.Graphics.SetClip(new Rectangle(0, 0, ViewWidth, Height)); int left = -offset;
            for (int i = 0; i < items.Count; i++) {
                int width = ItemWidth(items[i]); var rect = new Rectangle(left, 1, width, Math.Max(1, Height-3)); bool selected = items[i] == active;
                Design.Surface(e.Graphics, new Rectangle(rect.X+1,rect.Y+1,rect.Width-2,rect.Height-2), selected ? Color.FromArgb(224,235,251) : BackColor, 10, Color.Empty);
                TextRenderer.DrawText(e.Graphics, items[i], Font, rect, selected ? Design.Accent : ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);
                if (dragging && insertion == i) using (var pen = new Pen(Design.Accent, 2)) e.Graphics.DrawLine(pen, left, 3, left, Height-3);
                left += width + 4;
            }
            if (dragging && insertion == items.Count) using (var pen = new Pen(Design.Accent, 2)) e.Graphics.DrawLine(pen, left-2, 3, left-2, Height-3);
            e.Graphics.Restore(state);
            if (TotalWidth > Width) { using(var cover = new SolidBrush(BackColor)) e.Graphics.FillRectangle(cover, new Rectangle(Width-42,0,42,Height)); TextRenderer.DrawText(e.Graphics, "‹", Font, new Rectangle(Width-40,0,20,Height), ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); TextRenderer.DrawText(e.Graphics, "›", Font, new Rectangle(Width-20,0,20,Height), ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter); }
        }
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e); if (e.Button != MouseButtons.Left) return; Focus();
            if (e.X >= ViewWidth && TotalWidth > Width) { offset += e.X < Width-20 ? -90 : 90; ClampOffset(); Invalidate(); return; }
            int i = Hit(e.X); if (i < 0) return; pressed = items[i]; down = e.Location; Capture = true;
        }
        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e); if (pressed == null || !ReorderEnabled || pressed == items.FirstOrDefault()) return;
            if (!dragging && Math.Abs(e.X-down.X) < SystemInformation.DragSize.Width && Math.Abs(e.Y-down.Y) < SystemInformation.DragSize.Height) return;
            dragging = true; if (e.X < 18) offset -= 12; else if (e.X > ViewWidth-18) offset += 12; ClampOffset();
            int left = -offset; insertion = items.Count;
            for (int i = 0; i < items.Count; i++) { int width = ItemWidth(items[i]); if (e.X < left + width/2) { insertion = i; break; } left += width+4; } Invalidate();
        }
        protected override void OnMouseUp(MouseEventArgs e) {
            base.OnMouseUp(e); if (e.Button != MouseButtons.Left || pressed == null) return;
            string item = pressed; bool moved = dragging; int destination = Math.Max(1, insertion);
            pressed = null; dragging = false; insertion = -1; Capture = false;
            if (moved && destination >= 0) { int old = items.IndexOf(item); items.RemoveAt(old); if (destination > old) destination--; items.Insert(Math.Max(0, Math.Min(destination, items.Count)), item); if (Reordered != null) Reordered(items.ToArray()); }
            else if (Selected != null) Selected(item);
            Invalidate();
        }
        protected override void OnMouseCaptureChanged(EventArgs e) { base.OnMouseCaptureChanged(e); if (!Capture) { pressed = null; dragging = false; insertion = -1; Invalidate(); } }
        protected override void OnMouseWheel(MouseEventArgs e) { offset -= Math.Sign(e.Delta)*85; ClampOffset(); Invalidate(); base.OnMouseWheel(e); }
        protected override bool IsInputKey(Keys keyData) { return keyData == Keys.Left || keyData == Keys.Right || base.IsInputKey(keyData); }
        protected override void OnKeyDown(KeyEventArgs e) { base.OnKeyDown(e); int i = items.IndexOf(active); if ((e.KeyCode == Keys.Left || e.KeyCode == Keys.Right) && items.Count > 0) { i = Math.Max(0, Math.Min(items.Count-1, i + (e.KeyCode == Keys.Left ? -1 : 1))); if (Selected != null) Selected(items[i]); offset = Math.Max(0, items.Take(i).Sum(x => ItemWidth(x)+4) - ViewWidth/2); ClampOffset(); Invalidate(); e.Handled = true; } }
    }
}
