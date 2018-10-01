namespace STI.Biometria.Dominio.Comandos
{
    public sealed class IdentificarSocioPorDigitalComando
    {
        public IdentificarSocioPorDigitalComando(byte[] template, int estacao)
        {
            Template = template;
            Estacao = estacao;
        }

        public byte[] Template { get; }
        public int Estacao { get; }
    }
}
