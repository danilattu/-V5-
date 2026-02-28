using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ПРИЛОЖЕНИЕ_V5_Задание_4_Модуль_3
{
    public class NewClass
    {
        public string Name 
        { get
            {
                if (Name == "")
                    return "Поле не заполнено";
                else
                    return Name;
            };
            private void SetName(string name)
            { 
            Name = name; 
            }
        }


    }
}
