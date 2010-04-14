﻿using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Syncless.Core.Exceptions;
using Syncless.Filters;
using SynclessUI.Helper;

namespace SynclessUI
{
    /// <summary>
    /// Interaction logic for TagDetailsWindow.xaml
    /// </summary>
    public partial class TagDetailsWindow : Window
    {
        private MainWindow _main;
        private string _tagname;
        private List<Filter> filters;
        private bool _closingAnimationNotCompleted = true;

        public TagDetailsWindow(MainWindow main, string tagname)
        {
            try
            {
                InitializeComponent();
                _main = main;
                Owner = _main;
                ShowInTaskbar = false;
                _tagname = tagname;
                LblTag_Details.Content = "Tag Details for " + _tagname;
                filters = _main.Gui.GetAllFilters(_tagname);
                PopulateFilterStringList(false);
                TxtBoxPattern.IsEnabled = false;
                CmbBoxMode.IsEnabled = false;
            }
            catch (UnhandledException)
            {
                DialogHelper.DisplayUnhandledExceptionMessage(this);
            }
        }

        private void PopulateFilterStringList(bool selectoriginal)
        {
            int index = -1;

            if (selectoriginal)
            {
                index = ListFilters.SelectedIndex;
            }

            var generatedFilterStringList = new List<string>();

            int i = 1;

            foreach (Filter f in filters)
            {
                if (f is ExtensionFilter)
                {
                    var ef = (ExtensionFilter) f;
                    
					string mode = "";
					
                    if(ef.Mode == FilterMode.INCLUDE) {
						mode = "[Inclusion] ";
					} else if(ef.Mode == FilterMode.EXCLUDE) {
						mode = "[Exclusion] ";
					}
						
                    generatedFilterStringList.Add(i + ". " + mode + " " + ef.Pattern);
                }
                i++;
            }

            ListFilters.ItemsSource = null;
            ListFilters.ItemsSource = generatedFilterStringList;

            if (selectoriginal)
            {
                ListFilters.SelectedIndex = index;
            }

            if (generatedFilterStringList.Count == 0)
            {
                TxtBoxPattern.IsEnabled = false;
                TxtBoxPattern.Text = "";
                CmbBoxMode.IsEnabled = false;
                CmbBoxMode.SelectedIndex = -1;
            }
        }

        private void SelectFilter(Filter f)
        {
            int index = filters.IndexOf(f);
            ListFilters.SelectedIndex = index;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            BtnOk.IsEnabled = false;
            try
            {
                if (!_main.Gui.GetTag(_tagname).IsLocked)
                {
                    if(CheckRedundantFilters())
                    {
                        DialogHelper.ShowError(this, "Duplicate Filters", "Please remove all duplicate filters.");
                        BtnOk.IsEnabled = true;
                    } else
                    {
                        bool result = _main.Gui.UpdateFilterList(_tagname, filters);
                        Close();
                    }
                }
                else
                {
                    BtnOk.IsEnabled = true;
                    DialogHelper.ShowError(this, _tagname + " is Synchronizing",
                                           "You cannot update tag details while the tag is synchronizing.");
                }
            }
            catch (UnhandledException)
            {
                DialogHelper.DisplayUnhandledExceptionMessage(this);
                Close();
            }
        }

        private bool CheckRedundantFilters()
        {
            for (int i = 0; i < filters.Count; i++)
            {
                Filter fi = filters[i];
                filters.RemoveAt(i);
                if (filters.Contains(fi))
                {
                    filters.Insert(i, fi);
                    return true;
                }
                filters.Insert(i, fi);
            }

            return false;
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            BtnCancel.IsEnabled = false;
            Close();
        }

        private void BtnAddFilter_Click(object sender, RoutedEventArgs e)
        {
            var ef = (ExtensionFilter) FilterFactory.CreateExtensionFilter("*.*", FilterMode.INCLUDE);
            filters.Add(ef);

            PopulateFilterStringList(true);
            SelectFilter(ef);
        }

        private void BtnRemoveFilter_Click(object sender, RoutedEventArgs e)
        {
            if (ListFilters.SelectedIndex != -1)
            {
                int index = ListFilters.SelectedIndex;
                filters.RemoveAt(index);

                PopulateFilterStringList(false);
            }
        }

        private void ListFilters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ListFilters.SelectedIndex;

            if (index != -1)
            {
                TxtBoxPattern.IsEnabled = true;
                CmbBoxMode.IsEnabled = true;

                Filter f = filters[index];
                if (f is ExtensionFilter)
                {
                    var ef = (ExtensionFilter) f;
                    TxtBoxPattern.Text = ef.Pattern;
                    if (ef.Mode == FilterMode.INCLUDE)
                        CmbBoxMode.SelectedIndex = 0;
                    else if (ef.Mode == FilterMode.EXCLUDE)
                        CmbBoxMode.SelectedIndex = 1;
                }
            }
        }

        private void CmbBoxMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListFilters.SelectedIndex != -1 && ListFilters.SelectedIndex <= filters.Count)
            {
                Filter f = filters[ListFilters.SelectedIndex];

                if (CmbBoxMode.SelectedIndex == 0)
                    f.Mode = FilterMode.INCLUDE;
                else if (CmbBoxMode.SelectedIndex == 1)
                    f.Mode = FilterMode.EXCLUDE;
				
				PopulateFilterStringList(true);
            }
        }

        private void TabItemFiltering_GotFocus(object sender, RoutedEventArgs e)
        {
            SpecificFilterGrid.Visibility = Visibility.Visible;
        }

        private void TabItemProperties_GotFocus(object sender, RoutedEventArgs e)
        {
            SpecificFilterGrid.Visibility = Visibility.Hidden;
        }

        private void TabItemVersioning_GotFocus(object sender, RoutedEventArgs e)
        {
            SpecificFilterGrid.Visibility = Visibility.Hidden;
        }

        private void TxtBoxPattern_LostFocus(object sender, RoutedEventArgs e)
        {
                if (ListFilters.SelectedIndex != -1)
                {
                    Filter f = filters[ListFilters.SelectedIndex];
                    if (f is ExtensionFilter)
                    {
                        var ef = (ExtensionFilter)f;
                        ef.Pattern = TxtBoxPattern.Text;
                    }

                    PopulateFilterStringList(true);              
                }
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void FormFadeOut_Completed(object sender, EventArgs e)
        {
            _closingAnimationNotCompleted = false;
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_closingAnimationNotCompleted)
            {
                BtnCancel.IsCancel = false;
                e.Cancel = true;
                FormFadeOut.Begin();
            }
        }

        private void TxtBoxPattern_PreviewLostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            if (TxtBoxPattern.Text.Trim() == string.Empty && BtnCancel != e.NewFocus)
			{
				e.Handled = true;
                
				DialogHelper.ShowError(this, "Extension Mask Cannot be Empty", "Please input a valid extension mask.");
			}
        }
    }
}