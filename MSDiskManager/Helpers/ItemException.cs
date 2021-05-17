using MSDiskManager.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.Helpers
{
    public class ENTITYEXCEPTION
    {
        public BaseEntityViewModel Item { get; set; }
        public string Message { get; set; }

        public ENTITYEXCEPTION(BaseEntityViewModel item, Exception exception)
        {
            this.Item = item;
            this.Message = exception.Message;
        }
    }
}
