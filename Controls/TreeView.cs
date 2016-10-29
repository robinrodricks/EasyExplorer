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
    #region TreeView

    /// <summary>
    /// This is the TreeView used in the Browser control
    /// </summary>
    public class ExplorerTreeView : TreeView
    {
        private ExplorerTreeSorter sorter;

        public ExplorerTreeView()
        {
            HandleCreated += new EventHandler(BrowserTreeView_HandleCreated);

            sorter = new ExplorerTreeSorter();
        }

        #region Override

        #endregion

        #region Events

        /// <summary>
        /// Once the handle is created we can assign the image list to the TreeView
        /// </summary>
        void BrowserTreeView_HandleCreated(object sender, EventArgs e)
        {
            ShellImageList.SetSmallImageList(this);
        }

        #endregion

        #region Public

        public bool GetTreeNode(ShellItem shellItem, out TreeNode treeNode)
        {
            ArrayList pathList = new ArrayList();
            
            while (shellItem.ParentItem != null)
            {
                pathList.Add(shellItem);
                shellItem = shellItem.ParentItem;
            }
            pathList.Add(shellItem);

            pathList.Reverse();

            treeNode = Nodes[0];
            for (int i = 1; i < pathList.Count; i++)
            {
                bool found = false;
                foreach (TreeNode node in treeNode.Nodes)
                {
                    if (node.Tag != null && node.Tag.Equals(pathList[i]))
                    {
                        treeNode = node;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    treeNode = null;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This method will check whether a TreeNode is a parent of another TreeNode
        /// </summary>
        /// <param name="parent">The parent TreeNode</param>
        /// <param name="child">The child TreeNode</param>
        /// <returns>true if the parent is a parent of the child, false otherwise</returns>
        public bool IsParentNode(TreeNode parent, TreeNode child)
        {
            TreeNode current = child;
            while (current.Parent != null)
            {
                if (current.Parent.Equals(parent))
                    return true;

                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// This method will check whether a TreeNode is a parent of another TreeNode
        /// </summary>
        /// <param name="parent">The parent TreeNode</param>
        /// <param name="child">The child TreeNode</param>
        /// <param name="path">If the parent is indeed a parent of the child, this will be a path of
        /// TreeNodes from the parent to the child including both parent and child</param>
        /// <returns>true if the parent is a parent of the child, false otherwise</returns>
        public bool IsParentNode(TreeNode parent, TreeNode child, out TreeNode[] path)
        {
            ArrayList pathList = new ArrayList();

            TreeNode current = child;
            while (current.Parent != null)
            {
                pathList.Add(current);
                if (current.Parent.Equals(parent))
                {
                    pathList.Add(parent);
                    pathList.Reverse();
                    path = (TreeNode[])pathList.ToArray(typeof(TreeNode));
                    return true;
                }

                current = current.Parent;
            }

            path = null;
            return false;
        }

        public void SetSorting(bool sorting)
        {
            if (sorting)
                this.TreeViewNodeSorter = sorter;
            else
                this.TreeViewNodeSorter = null;
        }

        #endregion
    }

    /// <summary>
    /// This class is used to sort the TreeNodes in the BrowserTreeView
    /// </summary>
    internal class ExplorerTreeSorter : IComparer
    {
        #region IComparer Members

        /// <summary>
        /// This method will compare the ShellItems of the TreeNodes to determine the return value for
        /// comparing the TreeNodes.
        /// </summary>
        public int Compare(object x, object y)
        {
            TreeNode nodeX = x as TreeNode;
            TreeNode nodeY = y as TreeNode;

            if (nodeX.Tag != null && nodeY.Tag != null)
                return ((ShellItem)nodeX.Tag).CompareTo(nodeY.Tag);
            else if (nodeX.Tag != null)
                return 1;
            else if (nodeY.Tag != null)
                return -1;
            else
                return 0;
        }

        #endregion
    }

    #endregion
}