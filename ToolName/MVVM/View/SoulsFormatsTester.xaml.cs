﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Linq;
using SoulsFormats;
using System.Diagnostics;

namespace ToolName.MVVM.View
{
    /// <summary>
    /// Interaction logic for SoulsFormatsTester.xaml
    /// </summary>
    public partial class SoulsFormatsTester : Window
    {
        public SoulsFormatsTester()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TestListBox.Items.Clear();
                //BND4 anibnd = BND4.Read(ofd.FileName);
                //TAE3 tae = TAE3.Read(anibnd.Files.First(item => item.ID == 5000000 + 4).Bytes);
                foreach (var groups in GPARAM.Read(ofd.FileName).Groups)
                {
                    var CommentsTreeViewItem = new TreeViewItem() { Header = "Comments" };
                    var groupsTreeViewItem = new TreeViewItem() {  Header = groups.Name1 + " | " + groups.Name2 };
                    groupsTreeViewItem.Items.Add(CommentsTreeViewItem);
                    foreach (var comment in groups.Comments)
                    {
                        if (!string.IsNullOrWhiteSpace(comment))
                        {
                            var commentTreeViewItem = new TreeViewItem() { Header = comment };
                            CommentsTreeViewItem.Items.Add(commentTreeViewItem);
                        }
                    }
                    foreach (var param in groups.Params)
                    {
                        var paramTreeViewItem = new TreeViewItem() { Header = param.Name1 + " | " + param.Name2 };
                        groupsTreeViewItem.Items.Add(paramTreeViewItem);

                    }
                    TestListBox.Items.Add(groupsTreeViewItem);
                }
            }
        }

        private TreeViewItem Lol(GPARAM.Param param)
        {
            var paramValueTreeViewItem = new TreeViewItem();
            var valuesList = param.Values;
            if (param.Type == GPARAM.ParamType.BoolA)
            {
                foreach (var value in param.Values)
                {
                    var valueCasted = (bool)value;
                    var valueTreeView = new TreeViewItem() { Header=valueCasted.ToString()};
                    paramValueTreeViewItem.Items.Add(valueTreeView);
                }
            }
            else if (param.Type == GPARAM.ParamType.Byte)
            {
                foreach (var value in param.Values)
                {
                    var valueCasted = (byte)value;
                    var valueTreeView = new TreeViewItem() { Header = valueCasted.ToString() };
                    paramValueTreeViewItem.Items.Add(valueTreeView);
                }
            }
            else if (param.Type == GPARAM.ParamType.Float)
            {
                foreach (var value in param.Values)
                {
                    var valueCasted = (float)value;
                    var valueTreeView = new TreeViewItem() { Header = valueCasted.ToString() };
                    paramValueTreeViewItem.Items.Add(valueTreeView);
                }
            }
            else if (param.Type == GPARAM.ParamType.IntA)
            {
                foreach (var value in param.Values)
                {
                    var valueCasted = (int)value;
                    var valueTreeView = new TreeViewItem() { Header = valueCasted.ToString() };
                    paramValueTreeViewItem.Items.Add(valueTreeView);
                }
            }
            else if (param.Type == GPARAM.ParamType.Short)
            {
                foreach (var value in param.Values)
                {
                    var valueCasted = (short)value;
                    var valueTreeView = new TreeViewItem() { Header = valueCasted.ToString() };
                    paramValueTreeViewItem.Items.Add(valueTreeView);
                }
            }
            return paramValueTreeViewItem;
        }
    }
}
