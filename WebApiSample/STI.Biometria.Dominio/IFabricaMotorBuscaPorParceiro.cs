using STI.Compartilhado.Core;

namespace STI.Biometria.Dominio
{
    public interface IFabricaMotorBuscaPorParceiro
    {
        Resultado<IMotorBuscaBiometrica, Falha> CriarMotorDeBuscaBiometrica(string parceiro);
    }
}
