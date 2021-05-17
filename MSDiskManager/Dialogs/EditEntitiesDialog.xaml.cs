using MSDiskManager.Helpers;
using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
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
    /// Interaction logic for EditEntitiesDialog.xaml
    /// </summary>
    public partial class EditEntitiesDialog : Window
    {
        public BaseEntityViewModel Entity;
        private BaseEntityViewModel baseEntity;
        private HashSet<BaseEntityViewModel> entities;
        private List<Tag> commonTags;
        private bool local { get; }
        public EditEntitiesDialog(BaseEntityViewModel baseEntity, List<BaseEntityViewModel> entities, bool local = false)
        {
            this.local = local;
            if (baseEntity == null) this.Close();
            this.entities = entities.ToHashSet();
            this.entities.Add(baseEntity);
            this.baseEntity = baseEntity;
            var tags = Globals.IsNullOrEmpty(entities) ? baseEntity.Tags : baseEntity.Tags.Where(t => entities.All(e => e.Tags.Any(t2 => t2.Id == t.Id))).ToObservableCollection();
            this.commonTags = tags.ToList();
            var isHidden = Globals.IsNullOrEmpty(entities) ? baseEntity.IsHidden : baseEntity.IsHidden && entities.All(e => e.IsHidden);
            var name = baseEntity.Name;
            var description = baseEntity.Description;
            this.Entity = new DirectoryViewModel { Name = name, Description = description, Tags = tags, IsHidden = isHidden };
            this.DataContext = Entity;
            InitializeComponent();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            if (this.DialogResult == null)
            {
                this.DialogResult = false;
                this.Close();
            }
        }

        private async void SaveClicked(object sender, RoutedEventArgs e)
        {
            if (local)
            {
                handleLocal();
                return;
            }
            var modified = false;
            var dRep = new DirectoryRepository();
            var fRep = new FileRepository();
            if (Globals.IsNotNullNorEmpty(Entity.Name) && Entity.Name != baseEntity.Name)
            {
                modified = true;
                baseEntity.Name = Entity.Name;
                foreach (var en in entities)
                {
                    if (en is FileViewModel) await fRep.ChangeName((long)en.Id, Entity.Name);
                    else await dRep.ChangeName((long)en.Id, Entity.Name);
                    
                }
            }
            if (Globals.IsNotNullNorEmpty(Entity.Description) && Entity.Description != baseEntity.Description)
            {
                modified = true;
                baseEntity.Description = Entity.Description;
                foreach (var en in entities)
                {
                    if (en is FileViewModel) await fRep.ChangeDescription((long)en.Id, Entity.Description);
                    else await dRep.ChangeDescription((long)en.Id, Entity.Description);
                }
            }
            if (Entity.IsHidden != baseEntity.IsHidden)
            {
                modified = true;
                baseEntity.IsHidden = Entity.IsHidden;
                foreach (var en in entities)
                {
                    if (en is FileViewModel) await fRep.ChangeIsHidden((long)en.Id, Entity.IsHidden);
                    else await dRep.ChangeIsHidden((long)en.Id, Entity.IsHidden);
                }
            }
            if (Entity.Tags.Count != commonTags.Count || !Entity.Tags.All(t => commonTags.Contains(t)))
            {
                modified = true;
                var toRemove = commonTags.Where(t => !Entity.Tags.Contains(t)).Select(t => t.Id).Cast<long>().ToList();
                var toAdd = Entity.Tags.Where(t => !commonTags.Contains(t)).Select(t => t.Id).Cast<long>().ToList();

                foreach (var en in entities)
                {
                    if (en is FileViewModel) { await fRep.RemoveTags((long)en.Id, toRemove); await fRep.AddTags((long)en.Id, toAdd.Except(en.Tags.Select(t => t.Id).Cast<long>()).ToList()); }
                    else { await dRep.RemoveTags((long)en.Id, toRemove); await dRep.AddTags((long)en.Id, toAdd.Except(en.Tags.Select(t=>t.Id).Cast<long>()).ToList()); }
                }
            }

            if (this.DialogResult == null)
            {
                this.DialogResult = modified;
                this.Close();
            }
        }
        private void handleLocal()
        {
            var modified = false;
            if (Globals.IsNotNullNorEmpty(Entity.Name) && Entity.Name != baseEntity.Name)
            {
                modified = true;
                foreach (var en in entities)
                {
                    en.Name = Entity.Name;
                }
            }
            if (Globals.IsNotNullNorEmpty(Entity.Description) && Entity.Description != baseEntity.Description)
            {
                modified = true;
                foreach (var en in entities)
                {
                    en.Description = Entity.Description;
                }
            }
            if (Entity.IsHidden != baseEntity.IsHidden)
            {
                modified = true;
                foreach (var en in entities)
                {
                    en.IsHidden = Entity.IsHidden;
                }
            }
            if (Entity.Tags.Count != commonTags.Count || !Entity.Tags.All(t => commonTags.Contains(t)))
            {
                modified = true;
                var toRemove = commonTags.Where(t => !Entity.Tags.Contains(t)).ToList();
                var toAdd = Entity.Tags.Where(t => !commonTags.Contains(t)).ToList();
                foreach (var en in entities)
                {
                    en.Tags.AddMany(toAdd);
                    en.Tags.RemoveWhere(t => toRemove.Contains(t));
                }
            }

            if (this.DialogResult == null)
            {
                this.DialogResult = modified;
                this.Close();
            }
        }
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.KeyDown += (a,r)=> { if (r.Key == Key.Escape) if (this.DialogResult == null) { this.DialogResult = false;this.Close(); } };
            NameTBX.Focus();
            NameTBX.SelectAll();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var tag = button.DataContext as Tag;
            Entity.Tags.Remove(tag);
        }
        private void AddTags(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var diag = new SelectTagsWindow(Entity.Tags.Select(t => (long)t.Id).ToList(), (tag) => Entity.Tags.Add(tag), true);
            diag.ShowDialog();
        }
    }
}
