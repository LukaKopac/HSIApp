using HSIApp.Models;
using HSIApp.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace HSIApp.Controls
{
    public partial class SpectrumManager : UserControl
    {
        public SpectrumManager()
        {
            InitializeComponent();
        }

        public void AddSpectrum(SpectrumSelection selection)
        {
            SpectrumList.Items.Add(selection);
            SpectrumList.ScrollIntoView(selection);
        }

        public void RemoveSpectrum(SpectrumSelection selection)
        {
            SpectrumList.Items.Remove(selection);
        }

        public IList<SpectrumSelection> GetSelectedSpectra()
        {
            return SpectrumList.SelectedItems
                .Cast<SpectrumSelection>()
                .ToList();
        }

        private void DeleteSelectedSpectra_Click(
            object sender,
            RoutedEventArgs e)
        {
            var selections = GetSelectedSpectra();

            foreach (var selection in selections)
            {
                SpectrumList.Items.Remove(selection);

                SpectrumRemoved?.Invoke(selection);
            }
        }

        private void SpectrumList_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                if (item is SpectrumSelection selection)
                {
                    SpectrumSelectionChanged?.Invoke(
                        selection,
                        true);
                }
            }

            foreach (var item in e.RemovedItems)
            {
                if (item is SpectrumSelection selection)
                {
                    SpectrumSelectionChanged?.Invoke(
                        selection,
                        false);
                }
            }
        }

        public event Action<SpectrumSelection, bool>?
            SpectrumSelectionChanged;

        public event Action<SpectrumSelection>?
            SpectrumRemoved;

        private void SpectrumName_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;

            if (sender is not TextBlock textBlock)
                return;

            if (textBlock.DataContext is not SpectrumSelection selection)
                return;

            if (textBlock.Parent is not Grid grid)
                return;

            var editor = grid.Children
                .OfType<TextBox>()
                .FirstOrDefault();

            if (editor == null)
                return;

            editor.Text = selection.Name;

            textBlock.Visibility = Visibility.Collapsed;
            editor.Visibility = Visibility.Visible;

            editor.Focus();
            editor.SelectAll();

            e.Handled = true;
        }

        private void SpectrumNameEditor_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (sender is not TextBox editor)
                return;

            if (editor.DataContext is not SpectrumSelection selection)
                return;

            if (e.Key == Key.Enter)
            {
                selection.Name = editor.Text;

                FinishNameEditing(editor);

                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                editor.Text = selection.Name;
                
                FinishNameEditing(editor);

                e.Handled = true;
            }
        }

        private void SpectrumNameEditor_LostFocus(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is not TextBox editor)
                return;

            if (editor.DataContext is not SpectrumSelection selection)
                return;

            selection.Name = editor.Text;

            FinishNameEditing(editor);
        }

        private void FinishNameEditing(TextBox editor)
        {
            if (editor.Parent is not Grid grid)
                return;

            var display = grid.Children
                .OfType<TextBlock>()
                .FirstOrDefault();

            if (display == null)
                return;

            editor.Visibility = Visibility.Collapsed;
            display.Visibility = Visibility.Visible;
        }

        private void SpectrumColor_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (sender is not Ellipse ellipse)
                return;

            if (ellipse.DataContext is not SpectrumSelection selection)
                return;

            var picker = new ColorPickerWindow(selection.Color)
            {
                Owner = Window.GetWindow(this)
            };

            if (picker.ShowDialog() == true)
            {
                selection.Color = picker.SelectedColor;
            }

            e.Handled = true;
        }
    }
}
