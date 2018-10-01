namespace STI.Biometria.Dominio
{
    public sealed class ConfiguracaoBiometria
    {
        public ConfiguracaoBiometria(int quantidadeThreadsIdentificacaoDigital, string parceiroSdkDigital)
        {
            QuantidadeThreadsIdentificacaoDigital = quantidadeThreadsIdentificacaoDigital;
            ParceiroSdkDigital = parceiroSdkDigital;
        }

        public int QuantidadeThreadsIdentificacaoDigital { get; }
        public string ParceiroSdkDigital { get; }
    }
}
