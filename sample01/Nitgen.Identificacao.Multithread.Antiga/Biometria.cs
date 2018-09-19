using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nitgen.Identificacao.Multithread.Antiga
{
    public sealed class Biometria
    {
        public int Id { get; set; }
        public byte[] TemplateFIR { get; set; }
    }
}
