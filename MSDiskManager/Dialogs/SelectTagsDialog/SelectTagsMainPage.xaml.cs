using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSDiskManager.Dialogs.SelectTagsDialog
{
    /// <summary>
    /// Interaction logic for SelectTagsMainPage.xaml
    /// </summary>
    public partial class SelectTagsMainPage : Page
    {
        private CancellationTokenSource cancels;
        public ObservableCollection<TagViewModel> Items { get; set; } = new ObservableCollection<TagViewModel>();
        private List<long> selectedTagIds { get; set; }
        private Action<Tag> selectTagFunction { get; set; }
        private int page = 0;
        private const int limit = 100;
        public Visibility AddButtonVisibility { get; set; } = Visibility.Collapsed;
        public Prop<string> Filter { get; set; } = new Prop<string>("");
        public Prop<Visibility> LoadMoreVisibility { get; set; } = new Prop<Visibility>(Visibility.Visible);
        private bool stopUpdate = false;
        public SelectTagsMainPage(List<long> selectedTagIds, Action<Tag> selectTagFunction, bool allowAdd)
        {
            Filter.PropertyChanged += (f, a) => { if (stopUpdate) return; _ = filterTags((f as Prop<string>)?.Value); };
            this.AddButtonVisibility = allowAdd ? Visibility.Visible : Visibility.Collapsed;
            this.selectTagFunction = selectTagFunction;
            this.selectedTagIds = selectedTagIds;
            InitializeComponent();
        }
        private async void ItemClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button.CommandParameter as TagViewModel;
            await SelectTag(tag);
        }
        private async Task SelectTag(TagViewModel tag)
        {
            if (tag == null) return;
            selectTagFunction(tag.Tag);
            selectedTagIds.Add((long)tag.Id);
            Items.Remove(tag);
            FilterTextBox.Focus();
            FilterTextBox.SelectAll();
            await new TagRepository().setLastAccessDate(tag.Id);
        }
        private async Task filterTags(string filter = "")
        {
            if (stopUpdate)
            {
                return;
            }
            Decorator border = VisualTreeHelper.GetChild(ItemsListView, 0) as Decorator;
            ScrollViewer scrollViewer = border.Child as ScrollViewer;
            scrollViewer.ScrollToVerticalOffset(0);

            cancels?.Cancel();
            cancels = new CancellationTokenSource();
            var token = cancels.Token;
            await Task.Delay(150);
            if (token.IsCancellationRequested)
            {
                return;
            }
            var rep = new TagRepository();
            Interlocked.Exchange(ref page, 0);
            var tags = (await rep.FilterByName<Tag>(filter, selectedTagIds, page, limit, true)).Select(t => new TagViewModel(t)).ToList();
            this.Items.Clear();
            if(tags.Count < limit)
            {
                LoadMoreVisibility.Value = Visibility.Collapsed;
            } else
            {
                LoadMoreVisibility.Value = Visibility.Visible;
            }
            tags.ForEach(t => this.Items.Add(t));
        }
        private async Task nextPage()
        {
            while (page > 0 && cancels != null && !cancels.IsCancellationRequested)
            {
                await Task.Delay(50);
                if (page == 0)
                {
                    return;
                }
            }
            cancels?.Cancel();
            cancels = new CancellationTokenSource();
            var token = cancels.Token;
            await Task.Delay(150);
            if (token.IsCancellationRequested)
            {
                return;
            }
            var rep = new TagRepository();
            Interlocked.Increment(ref page);
            var tags = (await rep.FilterByName<Tag>(Filter.Value, selectedTagIds, page, limit,true)).Select(t => new TagViewModel(t)).ToList();
            if (tags.Count < limit)
            {
                LoadMoreVisibility.Value = Visibility.Collapsed;
            }
            else
            {
                LoadMoreVisibility.Value = Visibility.Visible;
            }
            tags.ForEach(t => this.Items.Add(t));
            cancels.Cancel();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = filterTags();
        }

        private void LoadMore(object sender, RoutedEventArgs e)
        {
            _ = nextPage();
        }

        private void AddNewItem(object sender, RoutedEventArgs e)
        {
            createTag();
        }
        private void createTag()
        {
            FilterTextBox.SelectAll();
            var page = new AddTagPage(Filter.Value, async (tag) =>
            {
                selectTagFunction(tag);
                Name = "";
                selectedTagIds.Add((long)tag.Id);
            });
            this.NavigationService.Navigate(page);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            _ = filterTags();
            FilterTextBox.Focus();
            Window.GetWindow(this).KeyDown += listenToKeys;
        }
        private void listenToKeys(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Window.GetWindow(this).Close();
        }
        private async void FilterTextKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    e.Handled = true;
                    stopUpdate = true;
                    TagViewModel item = null;
                    if (Items.Count > 0 && (item = Items.FirstOrDefault(i => i.Name.ToLower().Trim() == Filter.Value.ToLower().Trim())) != null)
                    {
                        var index = Items.IndexOf(item);
                        await SelectTag(item);
                        if (index < Items.Count && index >= 0)
                        {
                            Filter.Value = Items[index].Name;
                            Items[index].IsSelected = true;

                            FilterTextBox.SelectAll();
                        }
                        else
                        {
                            Filter.Value = "";
                        }
                    }
                    else
                    {
                        createTag();
                    }
                    stopUpdate = false;
                    break;
                case Key.Tab:
                    if (Items.Count > 0)
                    {
                        e.Handled = true;
                        stopUpdate = true;
                        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                        {
                            var index = Items.Select(i => i.Name.Trim().ToLower()).ToList().IndexOf(Filter.Value.ToLower().Trim()) - 1;
                            if (index < 0)
                            {
                                index = Items.Count - 1;
                            }

                            Filter.Value = Items[index].Name;
                            Items[index].IsSelected = true;
                            ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
                        }
                        else
                        {
                            var index = Items.Select(i => i.Name.Trim().ToLower()).ToList().IndexOf(Filter.Value.ToLower().Trim()) + 1;
                            if (index < 0 || index >= Items.Count)
                            {
                                index = 0;
                            }

                            Filter.Value = Items[index].Name;
                            Items[index].IsSelected = true;
                            ItemsListView.ScrollIntoView(ItemsListView.SelectedItem);
                            //FilterTextBox.Text = Filter.Value;
                        }
                        await Task.Delay(100);
                        FilterTextBox.Focus();
                        FilterTextBox.SelectAll();
                        //FilterTextBox.Select(Filter.Value.Length, 0);
                        stopUpdate = false;
                    }
                    break;
            }

        }

        private async void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (Items.Count >= limit && (sender as ScrollViewer).VerticalOffset >= (sender as ScrollViewer).ScrollableHeight - 20)
            {
                await nextPage();
            }
        }
    }
}
