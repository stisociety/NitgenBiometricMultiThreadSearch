using STI.Compartilhado.Core;

namespace STI.Biometria.Dominio
{
    public interface IMotorBuscaBiometrica
    {
        Resultado<Digital, Falha> IdentificarDigital(byte[] digitalLida, Digital digitalParaComparar);
    }
}
