using System;

namespace STI.Biometria.Dominio
{
    public sealed class Digital
    {
        public Digital(int id, string matricula, byte[] templateIso)
        {
            Id = id;
            Matricula = matricula;
            TemplateIso = templateIso;
        }

        public int Id { get; }
        public string Matricula { get; }
        public byte[] TemplateIso { get; }

        internal static Digital NovaNaoEncontrada()
            => new Digital(0,"", null);
    }
}
