using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManiacEDv2.Actions
{
    public interface IAction
    {
        void Undo();
        IAction Redo();

        string Description { get; }
    }
}
