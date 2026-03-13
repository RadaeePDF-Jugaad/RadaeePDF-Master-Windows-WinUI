using Microsoft.UI.Xaml.Controls;
using RDUILib;
using System.Collections.Generic;

namespace RadaeeWinUI.Controls
{
    public class OutlineItem
    {
        public string Title { get; set; } = string.Empty;
        public int PageIndex { get; set; }
    }

    public sealed partial class OutlineDialog : ContentDialog
    {
        public int SelectedPageIndex { get; private set; } = -1;

        public OutlineDialog()
        {
            this.InitializeComponent();
            this.XamlRoot = App.MainWindow.Content.XamlRoot;
        }

        public void LoadOutline(PDFOutline rootOutline)
        {
            OutlineTreeView.RootNodes.Clear();

            if (rootOutline != null)
            {
                var childOutline = rootOutline.Child;
                while (childOutline != null)
                {
                    var node = BuildOutlineTree(childOutline);
                    if (node != null)
                    {
                        OutlineTreeView.RootNodes.Add(node);
                    }
                    childOutline = childOutline.Next;
                }
            }

            ExpandAllNodes(OutlineTreeView.RootNodes);
        }

        private TreeViewNode? BuildOutlineTree(PDFOutline outline)
        {
            if (outline == null)
                return null;

            var item = new OutlineItem
            {
                Title = outline.label ?? "Untitled",
                PageIndex = outline.dest
            };

            var node = new TreeViewNode
            {
                Content = item
            };

            var childOutline = outline.Child;
            while (childOutline != null)
            {
                var childNode = BuildOutlineTree(childOutline);
                if (childNode != null)
                {
                    node.Children.Add(childNode);
                }
                childOutline = childOutline.Next;
            }

            return node;
        }

        private void ExpandAllNodes(IList<TreeViewNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.IsExpanded = true;
                if (node.Children.Count > 0)
                {
                    ExpandAllNodes(node.Children);
                }
            }
        }

        private void OutlineTreeView_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
        {
            if (args.InvokedItem is TreeViewNode node && node.Content is OutlineItem item)
            {
                SelectedPageIndex = item.PageIndex;
                this.Hide();
            }
        }
    }
}
