using STI.Biometria.Dominio.Infra.Integracoes.Nitgen;
using STI.Compartilhado.Core;

namespace STI.Biometria.Dominio.Infra.Integracoes
{
    public sealed class FabricaMotorBuscaBiometrica : IFabricaMotorBuscaPorParceiro
    {
        public Resultado<IMotorBuscaBiometrica, Falha> CriarMotorDeBuscaBiometrica(string parceiro)
        {
            if (parceiro.ToLower().Equals("nitgen"))
                return new SdkNitgen();
            return Falha.Nova(400, $"SDK {parceiro} não é reconhecido");
        }
    }
}
