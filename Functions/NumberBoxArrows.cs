using System.Drawing;
using System.Windows.Forms;
namespace Transport_Delivery_Management_System_for_M.J._Billones_Transport_Service.Functions
{
    /// <summary>
    /// Nov 16: a NumericUpDown whose up/down arrows are hidden keeps their strip at the right edge, and the box never paints
    /// that strip itself (it is an opaque control), so leftover pixels of whatever was on screen showed through (a stray "e"
    /// beside the truck form's locked crate capacity). <see cref="HideArrows"/> hides the arrows and paints the strip in the
    /// box's own background colour.
    /// </summary>
    public static class NumberBoxArrows
    {
        /// <summary>Hides the up/down arrows of a (read-only) number box and keeps the empty strip they leave painted.</summary>
        public static void HideArrows(this NumericUpDown box)
        {
            if (box.Controls.Count == 0) return;
            Control arrows = box.Controls[0];
            arrows.Visible = false;
            box.Paint += (s, e) =>
            {
                if (arrows.Visible) return;
                // The typing area (Controls[1]) has the colour the person sees; a read-only box may differ from box.BackColor.
                Color back = box.Controls.Count > 1 ? box.Controls[1].BackColor : box.BackColor;
                using var b = new SolidBrush(back);
                e.Graphics.FillRectangle(b, arrows.Bounds);
            };
            box.Invalidate();
        }
    }
}
