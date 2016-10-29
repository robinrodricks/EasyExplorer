using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Collections;
using System.Drawing;
using System.Runtime.InteropServices;
using ShellDll;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.ComponentModel;

namespace EasyExplorer
{
    #region ComboBox

    /// <summary>
    /// This is the ComboBox used in the navigationbar. It is extended to show the icons that go with the
    /// ShellItems.
    /// </summary>
    internal class ExplorerComboBox : ToolStripComboBox
    {
        #region Fields

        // The width of the indent of the ComboItems
        private int indentWidth = 10;

        // The item for the browser's current selected directory
        private ExplorerComboItem currentItem;

        // The class to draw the icon in the textbox area of the ComboBox
        private ComboEditWindow editWindow;

        #endregion

        public ExplorerComboBox()
        {
            ComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            ComboBox.DrawItem += new DrawItemEventHandler(ComboBox_DrawItem);
            ComboBox.HandleCreated += new EventHandler(ComboBox_HandleCreated);

            ComboBox.MouseDown += new MouseEventHandler(ComboBox_MouseDown);
            ComboBox.MouseMove += new MouseEventHandler(ComboBox_MouseMove);
            ComboBox.MouseClick += new MouseEventHandler(ComboBox_MouseClick);

            ComboBox.DropDown += new EventHandler(ComboBox_DropDown);
        }

        #region Events

        /// <summary>
        /// This method will change the currentItem field once a new item is selected
        /// </summary>
        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);

            if (ComboBox.SelectedIndex > -1)
                currentItem = ComboBox.SelectedItem as ExplorerComboItem;         
        }

        /// <summary>
        /// Once the handle has been created a new instance of ComboEditWindow is made to make sure
        /// the icons are drawn in the TextBox part of the ComboBox
        /// </summary>
        void ComboBox_HandleCreated(object sender, EventArgs e)
        {
            editWindow = new ComboEditWindow(this);
        }

        /// <summary>
        /// This method will draw the items of the DropDownList. It will draw the Icon and the text and
        /// with the indent that goes with the item
        /// </summary>
        void ComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1)
                return;
            else
            {
                ExplorerComboItem item = (ExplorerComboItem)Items[e.Index];

                e.DrawBackground();
                e.DrawFocusRectangle();

                int indentOffset = indentWidth * item.Indent;

                int imageYOffset = (e.Bounds.Height - item.Image.Height) / 2;
                Point imagePoint = new Point(
                    e.Bounds.Left + indentOffset + 2, 
                    e.Bounds.Top + imageYOffset);

                Size textSize = e.Graphics.MeasureString(item.Text, Font).ToSize();
                int textYOffset = (e.Bounds.Height - textSize.Height) / 2;
                Point textPoint = new Point(
                    e.Bounds.Left + item.Image.Width + indentOffset + 2, 
                    e.Bounds.Top + textYOffset);

                e.Graphics.DrawIcon(item.Image, imagePoint.X, imagePoint.Y);
                e.Graphics.DrawString(item.Text, e.Font, new SolidBrush(e.ForeColor), textPoint);
            }
        }

        /// <summary>
        /// If the mouse is moved over the icon of the ComboBox this method will change the cursor
        /// into an hand to indicate that the icon can be clicked
        /// </summary>
        void ComboBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (editWindow.ImageRect.Contains(e.Location))
            {
                Cursor.Current = Cursors.Hand;
            }
            else if (Cursor.Current == Cursors.Hand)
            {
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// If the mouse is moved over the icon of the ComboBox this method will change the cursor
        /// into an hand to indicate that the icon can be clicked
        /// </summary>
        void ComboBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (editWindow.ImageRect.Contains(e.Location))
                Cursor.Current = Cursors.Hand;
        }  

        /// <summary>
        /// If the icon of the ComboBox is clicked this method will select all text in the ComboBox
        /// </summary>
        void ComboBox_MouseClick(object sender, MouseEventArgs e)
        {
            if (editWindow.ImageRect.Contains(e.Location))
                SelectAll();
        }

        /// <summary>
        /// This method will make sure that when the ComboBox is dropped down, the width of the DropDownList
        /// will be sufficient to fit all items
        /// </summary>
        void ComboBox_DropDown(object sender, EventArgs e)
        {
            int width = 0;
            Graphics gfx = ComboBox.CreateGraphics();
            foreach (ExplorerComboItem item in Items)
            {
                int itemWidth =
                    gfx.MeasureString(item.Text, Font).ToSize().Width +
                    item.Image.Width +
                    indentWidth * item.Indent +
                    (Items.Count > MaxDropDownItems ? SystemInformation.VerticalScrollBarWidth : 0);

                if (itemWidth > width)
                    width = itemWidth;
            }

            if (width > this.Width)
                ComboBox.DropDownWidth = width;
            else
                ComboBox.DropDownWidth = this.Width;
        } 

        #endregion

        #region Public

        [Browsable(false)]
        public ExplorerComboItem CurrentItem 
        { 
            get { return currentItem; }
            set { currentItem = value; }
        }

        #endregion

        #region ComboEditWindow

        class ComboEditWindow : NativeWindow
        {
            private const int LEFTMARGIN = 0x1;
            private ExplorerComboBox owner;
            private Rectangle imageRect;
            private int margin = 0;

            /// <summary>
            /// The native window's original handle is released 
            /// and the handle of the TextBox is assigned to it.
            /// </summary>
            public ComboEditWindow(ExplorerComboBox owner)
            {
                this.owner = owner;

                ShellAPI.COMBOBOXINFO info = new ShellAPI.COMBOBOXINFO();
                info.cbSize = Marshal.SizeOf(typeof(ShellAPI.COMBOBOXINFO));
                ShellAPI.GetComboBoxInfo(owner.ComboBox.Handle, ref info);

                if (!this.Handle.Equals(IntPtr.Zero))
                {
                    this.ReleaseHandle();
                }
                this.AssignHandle(info.hwndEdit);
            }

            public Rectangle ImageRect { get { return imageRect; } }

            /// <summary>
            /// Set the margin of the TextBox to make room for the icon
            /// </summary>
            /// <param name="margin">The margin to set</param>
            private void SetMargin(int margin)
            {
                if (this.margin != margin)
                {
                    this.margin = margin;

                    if (owner == null)
                        return;

                    ShellAPI.SendMessage(
                        this.Handle, ShellAPI.WM.SETMARGINS, LEFTMARGIN,
                        new IntPtr(margin));
                }
            }

            /// <summary>
            /// Whenever the textbox is repainted, this method will draw the icon.
            /// </summary>
            private void DrawImage()
            {
                if (owner.CurrentItem != null)
                {
                    SetMargin(owner.CurrentItem.Image.Width + 3);

                    Icon icon = owner.CurrentItem.Image;
                    Bitmap image = new Bitmap(icon.Width, icon.Height);
                    
                    Graphics imageGfx = Graphics.FromImage(image);
                    imageRect = new Rectangle(new Point(0, 0), image.Size);
                    imageGfx.FillRectangle(Brushes.White, imageRect);
                    imageGfx.DrawIcon(icon, 0, 0);
                    imageGfx.Flush();

                    // Gets a GDI drawing surface from the textbox.
                    Graphics gfx = Graphics.FromHwnd(this.Handle);

                    if (owner.RightToLeft == RightToLeft.Yes)
                    {
                        gfx.DrawImage(image, (int)gfx.VisibleClipBounds.Width - icon.Width, 0);
                    }
                    else if (owner.RightToLeft == RightToLeft.No)
                    {
                        gfx.DrawImage(image, 0, 0);
                    }

                    gfx.Flush();
                    gfx.Dispose();
                }
            }

            // Override the WndProc method so that we can redraw the TextBox when the textbox is repainted.
            protected override void WndProc(ref Message m)
            {
                switch (m.Msg)
                {
                    case (int)ShellAPI.WM.PAINT:
                        base.WndProc(ref m);
                        DrawImage();
                        break;
                    case (int)ShellAPI.WM.LBUTTONDOWN:
                        base.WndProc(ref m);
                        DrawImage();
                        break;
                    case (int)ShellAPI.WM.KEYDOWN:
                        base.WndProc(ref m);
                        DrawImage();
                        break;
                    case (int)ShellAPI.WM.KEYUP:
                        base.WndProc(ref m);
                        DrawImage();
                        break;
                    case (int)ShellAPI.WM.CHAR:
                        base.WndProc(ref m);
                        DrawImage();
                        break;
                    case (int)ShellAPI.WM.GETTEXTLENGTH:
                        base.WndProc(ref m);
                        DrawImage();
                        break;
                    case (int)ShellAPI.WM.GETTEXT:
                        base.WndProc(ref m);
                        DrawImage();
                        break;
                    default:
                        base.WndProc(ref m);
                        break;
                }
            }
        }

        #endregion
    }
    
    /// <summary>
    /// This class represents the items that can be used for the ExplorerComboBox
    /// </summary>
    internal class ExplorerComboItem
    {
        // The ShellItem that goes with the ComboItem
        private ShellItem shellItem;

        // The indent that goes with the ComboItem
        private int indent;

        // The Icon that has to be drawn for this ComboItem
        private Icon image;

        public ExplorerComboItem(ShellItem shellItem, int indent)
        {
            this.shellItem = shellItem;
            this.indent = indent;
            this.image = ShellImageList.GetIcon(shellItem.ImageIndex, true);
        }

        #region Properties

        public ShellItem ShellItem { get { return shellItem; } }
        public int Indent { get { return indent; } }
        public Icon Image { get { return image; } }
        public string Text { get { return ShellItem.Text; } }

        #endregion

        public override string ToString()
        {
            return ShellItem.Path;
        }
    }

    #endregion
}