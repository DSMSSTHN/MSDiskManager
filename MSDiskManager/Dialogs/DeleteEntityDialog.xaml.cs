using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for DeleteEntityDialog.xaml
    /// </summary>
    public partial class DeleteEntityDialog : Window
    {
        public BaseEntityViewModel Entity { get; }
        public Prop<string> Name { get; } = new Prop<string>("");
        public Prop<string> FullPath { get; } = new Prop<string>("");
        public Prop<int> Progress { get; } = new Prop<int>(0);
        public Prop<int> Maximum { get; } = new Prop<int>(0);
        public Prop<Visibility> Started { get; } = new Prop<Visibility>(Visibility.Hidden);
        public List<BaseEntityViewModel> Entities { get; }
        public List<BaseEntityViewModel> Successful { get; } = new List<BaseEntityViewModel>();
        public string Message { get; }
        public DeleteEntityDialog(BaseEntityViewModel entity)
        {
            this.Entity = entity;
            Maximum.Value = 1;
            Name.Value = entity.Name;
            FullPath.Value = entity.FullPath;
            this.Message = entity is DirectoryViewModel ? "do you wish to remove the directory with all items within it" : "do you wish to remove this item ";
            InitializeComponent();
        }
        public DeleteEntityDialog(List<BaseEntityViewModel> entities)
        {
            this.Entities = entities;
            Maximum.Value = entities.Count;
            
            this.Message = $"do you wish to remove the {entities.Count} items with all items within them";
            InitializeComponent();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            if(this.DialogResult == null)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private async void DeleteReferenceClicked(object sender, RoutedEventArgs e)
        {
            Started.Value = Visibility.Visible;
            if (Entity != null)
            {
                await deleteEntityReference(Entity);
                Successful.Add(Entity);
            } else
            {
                foreach (var entity in Entities)
                {
                    Name.Value = entity.Name;
                    FullPath.Value = entity.FullPath;
                    var handeled = false;
                    while (!handeled)
                    {
                    var res = await deleteEntityReference(entity);
                        if (!res.success)
                        {
                            var button = MessageBoxButton.YesNoCancel;
                            var result = MessageBox.Show($"item {entity.Name} in [{entity.FullPath}] could not be deleted \n{res.message}.\nDo you wish to retry?", "Error while deleting", button);
                            if (result == MessageBoxResult.Cancel)
                            {
                                this.DialogResult = true;
                                this.Close();
                                return;
                            }
                            handeled = result == MessageBoxResult.No;
                        }
                        else handeled = true;
                    }
                    Progress.Value += 1;
                    Successful.Add(entity);
                }
            }
           if(this.DialogResult == null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
        private async Task<(Boolean success, string message, BaseEntity directory)> deleteEntityReference(BaseEntityViewModel entity)
        {
            if (entity is DirectoryViewModel)
            {
                return await new DirectoryRepository().DeleteReferenceOnly((long)entity.Id);
                
            }
            else if (entity is FileViewModel)
            {
                return await new FileRepository().DeleteReferenceOnly((long)entity.Id);
               
            }
            return (false, "Entity is not valid", null);
        }

        private async void FullDeleteClicked(object sender, RoutedEventArgs e)
        {
            Started.Value = Visibility.Visible;
            if (Entity != null)
            {
                await fullDeleteEntity(Entity);
                Successful.Add(Entity);
            }
            else
            {
                foreach (var entity in Entities)
                {
                    Name.Value = entity.Name;
                    FullPath.Value = entity.FullPath;
                    var handeled = false;
                    while (!handeled)
                    {
                        var res = await fullDeleteEntity(entity);
                        if (!res.success)
                        {
                            var button = MessageBoxButton.YesNoCancel;
                            var result = MessageBox.Show($"item {entity.Name} in [{entity.FullPath}] could not be deleted \n{res.message}.\nDo you wish to retry?", "Error while deleting", button);
                            if (result == MessageBoxResult.Cancel)
                            {
                                if(this.DialogResult == null)
                                {
                                    this.DialogResult = true;
                                    this.Close();
                                }
                                return;
                            }
                            handeled = result == MessageBoxResult.No;
                        }
                        else handeled = true;
                    }
                    Progress.Value += 1;
                    Successful.Add(entity);
                }
            }
            if (this.DialogResult == null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }
        private async Task<(Boolean success, string message, BaseEntity directory)> fullDeleteEntity(BaseEntityViewModel entity)
        {
            if (entity is DirectoryViewModel)
            {
                return await new DirectoryRepository().DeleteDirectory((long)entity.Id);
               
            }
            else if (entity is FileViewModel)
            {
                return await new FileRepository().DeleteFile((long)entity.Id);
                
            }
            return (false, "Entity is not valid", null);
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.KeyDown += (a, r) => { if (r.Key == Key.Escape) if (this.DialogResult == null) { this.DialogResult = false; this.Close(); } };
        }
    }
}
