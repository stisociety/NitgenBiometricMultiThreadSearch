using STI.Compartilhado.Core;

namespace STI.Biometria.Dominio
{
    public interface IConfiguracoesBiometriaRepositorio
    {
        Resultado<ConfiguracaoBiometria, Falha> Recuperar(int estacao);
    }
}
