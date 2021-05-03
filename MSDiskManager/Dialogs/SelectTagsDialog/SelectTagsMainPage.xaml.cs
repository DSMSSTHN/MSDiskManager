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
        public ObservableCollection<Tag> Tags { get; set; } = new ObservableCollection<Tag>();
        private List<long> selectedTagIds { get; set; }
        private Action<Tag> selectTagFunction { get; set; }
        private string _filter = "";
        private int page = 0;
        private const int limit = 30;
        public Visibility AddButtonVisibility { get; set; } = Visibility.Collapsed;
        public string Filter
        {
            get => _filter; set
            {
                _filter = value;
                _ = this.filterTags();
            }
        }
        public SelectTagsMainPage(List<long> selectedTagIds, Action<Tag> selectTagFunction, bool allowAdd)
        {
            this.AddButtonVisibility = allowAdd ? Visibility.Visible : Visibility.Collapsed;
            this.selectTagFunction = selectTagFunction;
            this.selectedTagIds = selectedTagIds;
            InitializeComponent();
            this.DataContext = this;
        }
        private void TagClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button.CommandParameter as Tag;
            this.selectTagFunction(tag);
            this.selectedTagIds.Add((long)tag.Id);
            this.Tags.Remove(tag);
        }

        private async Task filterTags()
        {
            var rep = new TagRepository();
            Interlocked.Exchange(ref page, 0);
            var tags = await rep.GetTags(Filter, selectedTagIds, page, limit);
            this.Tags.Clear();
            tags.ForEach(t => this.Tags.Add(t));
        }
        private async Task nextPage()
        {
            var rep = new TagRepository();
            Interlocked.Increment(ref page);
            var tags = await rep.GetTags(Filter, selectedTagIds, page, limit);
            tags.ForEach(t => this.Tags.Add(t));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _ = filterTags();
        }

        private void LoadMore(object sender, RoutedEventArgs e)
        {
            _ = nextPage();
        }

        private void AddNewTag(object sender, RoutedEventArgs e)
        {
            var page = new AddTagPage(_filter, (tag) =>
            {
                this.selectTagFunction(tag);
                this.selectedTagIds.Add((long)tag.Id);
                this.Tags.Remove(tag);
            });
            this.NavigationService.Navigate(page);
        }
    }
}
