using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class OrderModel : INotifyPropertyChanged
    {
        private bool isChecked = false;

        public FileOrder Order { get; }
        public bool IsChecked { get => isChecked; set { isChecked = value; NotifyPropertChanged("IsChecked"); } }
        public OrderModel(FileOrder order, bool isChecked = false)
        {
            this.Order = order;
            this.isChecked = isChecked;
        }
        public string OrderName
        {
            get
            {
                switch (Order)
                {
                    case FileOrder.Name:
                        return "Name";
                    case FileOrder.NameDesc:
                        return "NameDesc";
                    case FileOrder.AddTime:
                        return "AddTime";
                    case FileOrder.AddTimeDesc:
                        return "AddTimeDesc";
                    case FileOrder.MoveTime:
                        return "MoveTime";
                    case FileOrder.MoveTimeDesc:
                        return "MoveTimeDesc";
                    case FileOrder.Type:
                        return "Type";
                    case FileOrder.TypeDesc:
                        return "TypeDesc";
                }
                return "Name";
            }
        }
        public DirectoryOrder DirectoryOrder
        {
            get
            {
                switch (Order)
                {
                    case FileOrder.Name:
                        return DirectoryOrder.Name;
                    case FileOrder.NameDesc:
                        return DirectoryOrder.NameDesc;
                    case FileOrder.AddTime:
                        return DirectoryOrder.AddTime;
                    case FileOrder.AddTimeDesc:
                        return DirectoryOrder.AddTimeDesc;
                    case FileOrder.MoveTime:
                        return DirectoryOrder.MoveTime;
                    case FileOrder.MoveTimeDesc:
                        return DirectoryOrder.MoveTimeDesc;
                    case FileOrder.Type:
                        return DirectoryOrder.Name;
                    case FileOrder.TypeDesc:
                        return DirectoryOrder.NameDesc;
                }
                return DirectoryOrder.Name;
            }
        }
        public static List<FileOrder> AllOrders
        {
            get
            {
                var result = new List<FileOrder>();
                result.Add(FileOrder.Name);
                result.Add(FileOrder.NameDesc);
                result.Add(FileOrder.Type);
                result.Add(FileOrder.TypeDesc);
                result.Add(FileOrder.AddTime);
                result.Add(FileOrder.AddTimeDesc);
                result.Add(FileOrder.MoveTime);
                result.Add(FileOrder.MoveTimeDesc);
                return result;

            }
        }
      
        public static List<OrderModel> AllOrdersModels { get => AllOrders.Select(order => new OrderModel (order)).ToList(); }
        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
