using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenFloater
{
    internal static class Program
    {
        private const int PROCESS_PER_MONITOR_DPI_AWARE = 2;
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int awareness);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        private static void Main()
        {
            EnableDpiAwareness();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PreviewForm());
        }

        private static void EnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2))
                {
                    return;
                }
            }
            catch { }

            try
            {
                if (SetProcessDpiAwareness(PROCESS_PER_MONITOR_DPI_AWARE) == 0)
                {
                    return;
                }
            }
            catch { }

            try { SetProcessDPIAware(); } catch { }
        }
    }

    internal sealed class PreviewForm : Form
    {
        private readonly ComboBox screenCombo = new ComboBox();
        private readonly ComboBox modeCombo = new ComboBox();
        private readonly CheckBox topMostCheck = new CheckBox();
        private readonly CheckBox pauseCheck = new CheckBox();
        private readonly CheckBox magnifyMouseCheck = new CheckBox();
        private readonly Button refreshButton = new Button();
        private readonly PictureBox previewBox = new PictureBox();
        private readonly Label statusLabel = new Label();
        private readonly Timer captureTimer = new Timer();

        private Screen[] screens = new Screen[0];
        private int selectedScreenIndex = 0;
        private DateTime lastStatusUpdate = DateTime.MinValue;

        private const int CURSOR_SHOWING = 0x00000001;
        private const int DI_NORMAL = 0x0003;
        private const int SM_CXCURSOR = 13;
        private const int SM_CYCURSOR = 14;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIconEx(
            IntPtr hdc,
            int xLeft,
            int yTop,
            IntPtr hIcon,
            int cxWidth,
            int cyWidth,
            int istepIfAniCur,
            IntPtr hbrFlickerFreeDraw,
            int diFlags);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        public PreviewForm()
        {
            Text = "ScreenFloater - \u6269\u5c55\u5c4f\u9884\u89c8";
            StartPosition = FormStartPosition.Manual;
            MinimumSize = new Size(380, 240);
            Size = new Size(760, 480);
            TopMost = true;
            KeyPreview = true;

            ConfigureToolbar();
            ConfigurePreview();

            captureTimer.Interval = 120;
            captureTimer.Tick += delegate { CaptureFrame(); };

            Load += delegate
            {
                RefreshScreens();
                MoveToPrimaryScreen();
                captureTimer.Start();
            };

            FormClosed += delegate
            {
                captureTimer.Stop();
                if (previewBox.Image != null)
                {
                    previewBox.Image.Dispose();
                    previewBox.Image = null;
                }
            };

            Resize += delegate { UpdateStatus(false); };
            KeyDown += OnKeyDown;
        }

        private void ConfigureToolbar()
        {
            var toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 38;
            toolbar.BackColor = SystemColors.Control;

            screenCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            screenCombo.Left = 8;
            screenCombo.Top = 7;
            screenCombo.Width = 292;
            screenCombo.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            screenCombo.SelectedIndexChanged += delegate
            {
                selectedScreenIndex = Math.Max(0, screenCombo.SelectedIndex);
                UpdateStatus(true);
            };

            modeCombo.DropDownStyle = ComboBoxStyle.DropDownList;
            modeCombo.Left = screenCombo.Right + 8;
            modeCombo.Top = 7;
            modeCombo.Width = 64;
            modeCombo.Items.Add("\u586b\u6ee1");
            modeCombo.Items.Add("\u5b8c\u6574");
            modeCombo.SelectedIndex = 0;
            modeCombo.SelectedIndexChanged += delegate { ApplyDisplayMode(); };

            topMostCheck.Text = "\u7f6e\u9876";
            topMostCheck.Checked = true;
            topMostCheck.AutoSize = true;
            topMostCheck.Top = 9;
            topMostCheck.CheckedChanged += delegate { TopMost = topMostCheck.Checked; };

            pauseCheck.Text = "\u6682\u505c";
            pauseCheck.AutoSize = true;
            pauseCheck.Top = 9;

            magnifyMouseCheck.Text = "\u9f20\u6807+";
            magnifyMouseCheck.Checked = true;
            magnifyMouseCheck.AutoSize = true;
            magnifyMouseCheck.Top = 9;

            refreshButton.Text = "\u5237\u65b0";
            refreshButton.Width = 54;
            refreshButton.Height = 24;
            refreshButton.Top = 7;
            refreshButton.Click += delegate { RefreshScreens(); };

            statusLabel.AutoSize = false;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.Top = 7;
            statusLabel.Height = 24;
            statusLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;

            toolbar.Controls.Add(screenCombo);
            toolbar.Controls.Add(modeCombo);
            toolbar.Controls.Add(topMostCheck);
            toolbar.Controls.Add(pauseCheck);
            toolbar.Controls.Add(magnifyMouseCheck);
            toolbar.Controls.Add(refreshButton);
            toolbar.Controls.Add(statusLabel);
            toolbar.Resize += delegate { LayoutToolbar(toolbar); };

            Controls.Add(toolbar);
            LayoutToolbar(toolbar);
        }

        private void LayoutToolbar(Control toolbar)
        {
            modeCombo.Left = screenCombo.Right + 8;
            topMostCheck.Left = modeCombo.Right + 10;
            pauseCheck.Left = topMostCheck.Right + 12;
            magnifyMouseCheck.Left = pauseCheck.Right + 12;
            refreshButton.Left = magnifyMouseCheck.Right + 12;
            statusLabel.Left = refreshButton.Right + 10;
            statusLabel.Width = Math.Max(40, toolbar.ClientSize.Width - statusLabel.Left - 8);
        }

        private void ConfigurePreview()
        {
            previewBox.Dock = DockStyle.Fill;
            previewBox.BackColor = Color.FromArgb(32, 32, 32);
            previewBox.BorderStyle = BorderStyle.None;
            ApplyDisplayMode();
            Controls.Add(previewBox);
            previewBox.SendToBack();
        }

        private void ApplyDisplayMode()
        {
            if (modeCombo.SelectedIndex == 1)
            {
                previewBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            else
            {
                previewBox.SizeMode = PictureBoxSizeMode.StretchImage;
            }
        }

        private void RefreshScreens()
        {
            screens = Screen.AllScreens;
            screenCombo.Items.Clear();

            for (int i = 0; i < screens.Length; i++)
            {
                Screen s = screens[i];
                Rectangle b = s.Bounds;
                string primary = s.Primary ? "\u4e3b\u5c4f" : "\u6269\u5c55\u5c4f";
                screenCombo.Items.Add(string.Format(
                    "{0}: {1}  {2}x{3}  X={4}, Y={5}",
                    i + 1, primary, b.Width, b.Height, b.X, b.Y));
            }

            int defaultIndex = 0;
            for (int i = 0; i < screens.Length; i++)
            {
                if (!screens[i].Primary)
                {
                    defaultIndex = i;
                    break;
                }
            }

            if (screens.Length == 0)
            {
                statusLabel.Text = "\u672a\u68c0\u6d4b\u5230\u5c4f\u5e55";
                return;
            }

            selectedScreenIndex = Math.Min(defaultIndex, screens.Length - 1);
            screenCombo.SelectedIndex = selectedScreenIndex;
            FitWindowToSourceAspect();
            UpdateStatus(true);
        }

        private void MoveToPrimaryScreen()
        {
            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Left = area.Left + 80;
            Top = area.Top + 80;
        }

        private void FitWindowToSourceAspect()
        {
            if (screens.Length == 0 || selectedScreenIndex < 0 || selectedScreenIndex >= screens.Length)
            {
                return;
            }

            Rectangle b = screens[selectedScreenIndex].Bounds;
            if (b.Width <= 0 || b.Height <= 0)
            {
                return;
            }

            int maxWidth = Math.Min(900, Math.Max(420, Screen.PrimaryScreen.WorkingArea.Width - 160));
            int contentWidth = maxWidth;
            int contentHeight = Math.Max(180, (int)Math.Round(contentWidth * (double)b.Height / b.Width));
            Size = new Size(contentWidth, contentHeight + 38);
        }

        private void CaptureFrame()
        {
            if (pauseCheck.Checked || screens.Length == 0)
            {
                return;
            }

            if (selectedScreenIndex < 0 || selectedScreenIndex >= screens.Length)
            {
                selectedScreenIndex = 0;
            }

            Rectangle bounds = screens[selectedScreenIndex].Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            try
            {
                Bitmap frame = new Bitmap(bounds.Width, bounds.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(frame))
                {
                    g.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                    if (magnifyMouseCheck.Checked)
                    {
                        DrawMousePreview(g, frame, bounds);
                    }
                }

                Image old = previewBox.Image;
                previewBox.Image = frame;
                if (old != null)
                {
                    old.Dispose();
                }

                UpdateStatus(false);
            }
            catch (Exception ex)
            {
                if ((DateTime.Now - lastStatusUpdate).TotalSeconds > 1)
                {
                    statusLabel.Text = "\u622a\u56fe\u5931\u8d25: " + ex.Message;
                    lastStatusUpdate = DateTime.Now;
                }
            }
        }

        private void DrawMousePreview(Graphics g, Bitmap frame, Rectangle screenBounds)
        {
            CURSORINFO cursorInfo = new CURSORINFO();
            cursorInfo.cbSize = Marshal.SizeOf(typeof(CURSORINFO));

            if (!GetCursorInfo(out cursorInfo) ||
                (cursorInfo.flags & CURSOR_SHOWING) == 0 ||
                cursorInfo.hCursor == IntPtr.Zero)
            {
                return;
            }

            int relX = cursorInfo.ptScreenPos.X - screenBounds.Left;
            int relY = cursorInfo.ptScreenPos.Y - screenBounds.Top;
            if (relX < 0 || relY < 0 || relX >= screenBounds.Width || relY >= screenBounds.Height)
            {
                return;
            }

            g.SmoothingMode = SmoothingMode.AntiAlias;
            DrawMouseMarker(g, relX, relY, screenBounds);
            DrawLargeCursor(g, cursorInfo.hCursor, relX, relY, 2);
            DrawMagnifier(g, frame, cursorInfo.hCursor, relX, relY, screenBounds);
        }

        private void DrawMouseMarker(Graphics g, int x, int y, Rectangle screenBounds)
        {
            int radius = Math.Max(34, Math.Min(screenBounds.Width, screenBounds.Height) / 34);
            using (Pen shadow = new Pen(Color.FromArgb(210, 0, 0, 0), Math.Max(6, radius / 7)))
            using (Pen ring = new Pen(Color.FromArgb(245, 255, 214, 0), Math.Max(4, radius / 8)))
            using (Pen line = new Pen(Color.FromArgb(245, 255, 214, 0), Math.Max(3, radius / 12)))
            {
                g.DrawEllipse(shadow, x - radius, y - radius, radius * 2, radius * 2);
                g.DrawEllipse(ring, x - radius, y - radius, radius * 2, radius * 2);
                g.DrawLine(line, x - radius - 16, y, x - 8, y);
                g.DrawLine(line, x + 8, y, x + radius + 16, y);
                g.DrawLine(line, x, y - radius - 16, x, y - 8);
                g.DrawLine(line, x, y + 8, x, y + radius + 16);
            }
        }

        private void DrawMagnifier(Graphics g, Bitmap frame, IntPtr cursorHandle, int cursorX, int cursorY, Rectangle screenBounds)
        {
            int lensSize = Math.Max(340, Math.Min(screenBounds.Height / 3, screenBounds.Width / 4));
            lensSize = Math.Min(lensSize, Math.Min(screenBounds.Width, screenBounds.Height) - 24);
            if (lensSize < 180)
            {
                return;
            }

            int margin = Math.Max(16, lensSize / 18);
            Rectangle dest = new Rectangle(
                screenBounds.Width - lensSize - margin,
                screenBounds.Height - lensSize - margin,
                lensSize,
                lensSize);

            if (dest.Contains(cursorX, cursorY))
            {
                dest.X = margin;
                dest.Y = screenBounds.Height - lensSize - margin;
            }

            int zoom = 3;
            int srcSize = Math.Max(80, lensSize / zoom);
            Rectangle src = new Rectangle(cursorX - srcSize / 2, cursorY - srcSize / 2, srcSize, srcSize);
            src = ClampRectangle(src, frame.Width, frame.Height);

            using (Bitmap crop = frame.Clone(src, frame.PixelFormat))
            using (Brush bg = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
            using (Pen borderShadow = new Pen(Color.FromArgb(230, 0, 0, 0), Math.Max(8, lensSize / 45)))
            using (Pen border = new Pen(Color.FromArgb(255, 255, 214, 0), Math.Max(5, lensSize / 64)))
            using (Pen cross = new Pen(Color.FromArgb(230, 255, 214, 0), Math.Max(3, lensSize / 100)))
            {
                g.FillRectangle(bg, dest);

                InterpolationMode oldInterpolation = g.InterpolationMode;
                PixelOffsetMode oldPixelOffset = g.PixelOffsetMode;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.DrawImage(crop, dest);
                g.InterpolationMode = oldInterpolation;
                g.PixelOffsetMode = oldPixelOffset;

                g.DrawRectangle(borderShadow, dest);
                g.DrawRectangle(border, dest);

                int centerX = dest.Left + lensSize / 2;
                int centerY = dest.Top + lensSize / 2;
                int gap = Math.Max(10, lensSize / 26);
                int arm = Math.Max(34, lensSize / 6);
                g.DrawLine(cross, centerX - arm, centerY, centerX - gap, centerY);
                g.DrawLine(cross, centerX + gap, centerY, centerX + arm, centerY);
                g.DrawLine(cross, centerX, centerY - arm, centerX, centerY - gap);
                g.DrawLine(cross, centerX, centerY + gap, centerX, centerY + arm);

                DrawLargeCursor(g, cursorHandle, centerX, centerY, 4);
            }
        }

        private Rectangle ClampRectangle(Rectangle rect, int maxWidth, int maxHeight)
        {
            if (rect.Width > maxWidth)
            {
                rect.Width = maxWidth;
            }
            if (rect.Height > maxHeight)
            {
                rect.Height = maxHeight;
            }
            if (rect.X < 0)
            {
                rect.X = 0;
            }
            if (rect.Y < 0)
            {
                rect.Y = 0;
            }
            if (rect.Right > maxWidth)
            {
                rect.X = maxWidth - rect.Width;
            }
            if (rect.Bottom > maxHeight)
            {
                rect.Y = maxHeight - rect.Height;
            }
            return rect;
        }

        private void DrawLargeCursor(Graphics g, IntPtr cursorHandle, int x, int y, int scale)
        {
            int cursorWidth = Math.Max(32, GetSystemMetrics(SM_CXCURSOR));
            int cursorHeight = Math.Max(32, GetSystemMetrics(SM_CYCURSOR));
            IntPtr hdc = g.GetHdc();
            try
            {
                DrawIconEx(hdc, x, y, cursorHandle, cursorWidth * scale, cursorHeight * scale, 0, IntPtr.Zero, DI_NORMAL);
            }
            finally
            {
                g.ReleaseHdc(hdc);
            }
        }

        private void UpdateStatus(bool force)
        {
            if (!force && (DateTime.Now - lastStatusUpdate).TotalSeconds < 0.8)
            {
                return;
            }

            if (screens.Length == 0 || selectedScreenIndex < 0 || selectedScreenIndex >= screens.Length)
            {
                statusLabel.Text = "\u65e0\u5c4f\u5e55";
                return;
            }

            Rectangle b = screens[selectedScreenIndex].Bounds;
            statusLabel.Text = string.Format(
                "\u6e90: {0}x{1}    \u7a97\u53e3: {2}x{3}    T=\u7f6e\u9876, \u7a7a\u683c=\u6682\u505c, M=\u9f20\u6807+",
                b.Width,
                b.Height,
                ClientSize.Width,
                Math.Max(0, ClientSize.Height - 38));
            lastStatusUpdate = DateTime.Now;
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.T)
            {
                topMostCheck.Checked = !topMostCheck.Checked;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space)
            {
                pauseCheck.Checked = !pauseCheck.Checked;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.R)
            {
                RefreshScreens();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.M)
            {
                magnifyMouseCheck.Checked = !magnifyMouseCheck.Checked;
                e.Handled = true;
            }
        }
    }
}
