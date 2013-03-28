using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Prism.Events;

namespace Infrastructure
{
    public class What2Do
    {
        public string ModuleName;
        private List<string> _modulecommand = new List<string>();
        public List<string> ModuleCommand
        {
            get
            {
                return _modulecommand;
            }
            set
            {
                if (value != null)
                    _modulecommand = value;
            }
        }
    }
    public class ActivateModuleCopy : CompositePresentationEvent<What2Do>
    {
    }
    public class CreateBookmark : CompositePresentationEvent<What2Do>
    {
    }
    public class Command2Module : CompositePresentationEvent<What2Do>
    {
    }
    //Команда из поля tbAddress
    public class Command1Executed : CompositePresentationEvent<What2Do>
    {
    }
    //Команда из второго поля "поиска"
    public class Command2Executed : CompositePresentationEvent<What2Do>
    {
    }
}
