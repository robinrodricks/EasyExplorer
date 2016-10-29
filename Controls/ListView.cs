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
    #region ListView

    /// <summary>
    /// This is the ListView used in the Browser control
    /// </summary>
	public class ExplorerListView : ListView
    {
        #region Fields

        // The arraylist to store the order by which ListViewItems has been selected
        private ArrayList selectedOrder;

        private ContextMenu columnHeaderContextMenu;
        private bool suspendHeaderContextMenu;
        private int columnHeight = 0;

        private ExplorerListSorter sorter;

        #endregion

        public ExplorerListView()
        {
            OwnerDraw = true;

            HandleCreated += new EventHandler(BrowserListView_HandleCreated);
            selectedOrder = new ArrayList();
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);

            DrawItem += new DrawListViewItemEventHandler(BrowserListView_DrawItem);
            DrawSubItem += new DrawListViewSubItemEventHandler(BrowserListView_DrawSubItem);
            DrawColumnHeader += new DrawListViewColumnHeaderEventHandler(BrowserListView_DrawColumnHeader);

            this.Alignment = ListViewAlignment.Left;
            sorter = new ExplorerListSorter();
        }

        #region Owner Draw

        void BrowserListView_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        void BrowserListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        void BrowserListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
            columnHeight = e.Bounds.Height;
        }

        #endregion

        #region Override

        public new View View
        {
            get
            {
                return base.View;
            }
            set
            {
                base.View = value;

                if (value == View.Details)
                {
                    foreach (ColumnHeader col in Columns)
                        if (col.Width == 0)
                            col.Width = 120;
                }
            }
        }

        protected override void OnItemSelectionChanged(ListViewItemSelectionChangedEventArgs e)
        {
            if (e.IsSelected)
                selectedOrder.Insert(0, e.Item);
            else
                selectedOrder.Remove(e.Item);

            base.OnItemSelectionChanged(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (this.View == View.Details && columnHeaderContextMenu != null &&
                (int)m.Msg == (int)ShellAPI.WM.CONTEXTMENU)
            {
                if (suspendHeaderContextMenu)
                    suspendHeaderContextMenu = false;
                else
                {
                    int x = (int)ShellHelper.LoWord(m.LParam);
                    int y = (int)ShellHelper.HiWord(m.LParam);
                    Point clientPoint = PointToClient(new Point(x, y));
                    
                    if (clientPoint.Y <= columnHeight)
                        columnHeaderContextMenu.Show(this, clientPoint);
                }

                return;
            }

            base.WndProc(ref m);
        }

        #endregion

        #region Events

        /// <summary>
        /// Once the handle is created we can assign the image lists to the ListView
        /// </summary>
        void BrowserListView_HandleCreated(object sender, EventArgs e)
        {
            ShellImageList.SetSmallImageList(this);
            ShellImageList.SetLargeImageList(this);
        }

        #endregion

        #region Public

        [Browsable(false)]
        public ArrayList SelectedOrder
        {
            get { return selectedOrder; }
        }

        [Browsable(false)]
        public bool SuspendHeaderContextMenu
        {
            get { return suspendHeaderContextMenu; }
            set { suspendHeaderContextMenu = value; }
        }

        [Browsable(true)]
        public ContextMenu ColumnHeaderContextMenu
        {
            get { return columnHeaderContextMenu; }
            set { columnHeaderContextMenu = value; }
        }

        public void SetSorting(bool sorting)
        {
            if (sorting)
                this.ListViewItemSorter = sorter;
            else
                this.ListViewItemSorter = null;
        }

        public void ClearSelections()
        {
            selectedOrder.Clear();
            selectedOrder.Capacity = 0;
        }

        public bool GetListItem(ShellItem shellItem, out ListViewItem listItem)
        {
            listItem = null;

            foreach (ListViewItem item in Items)
            {
                if (shellItem.Equals(item.Tag))
                {
                    listItem = item;
                    return true;
                }
            }

            return false;
        }

        #endregion
    }

    /// <summary>
    /// This class is used to sort the ListViewItems in the BrowserListView
    /// </summary>
    internal class ExplorerListSorter : IComparer
    {
        #region IComparer Members

        /// <summary>
        /// This method will compare the ShellItems of the ListViewItems to determine the return value for
        /// comparing the ListViewItems.
        /// </summary>
        public int Compare(object x, object y)
        {
            ListViewItem itemX = x as ListViewItem;
            ListViewItem itemY = y as ListViewItem;

            if (itemX.Tag != null && itemY.Tag != null)
                return ((ShellItem)itemX.Tag).CompareTo(itemY.Tag);
            else if (itemX.Tag != null)
                return 1;
            else if (itemY.Tag != null)
                return -1;
            else
                return 0;
        }

        #endregion
    }

    #endregion
}