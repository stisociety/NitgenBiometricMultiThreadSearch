using System;
using System.Diagnostics;
using NITGEN.SDK.NBioBSP;
using STI.Compartilhado.Core;

namespace STI.Biometria.Dominio.Infra.Integracoes.Nitgen
{
    public class SdkNitgen : IMotorBuscaBiometrica
    {
        private NBioAPI _api;
        private NBioAPI.Export _apiConversor;
        private NBioAPI.IndexSearch _apiBusca;


        public SdkNitgen()
        {
            _api = new NBioAPI();
            _apiConversor = new NBioAPI.Export(_api);
            _apiBusca = new NBioAPI.IndexSearch(_api);
            _apiBusca.InitEngine();
        }

        public Resultado<Digital, Falha> IdentificarDigital(byte[] digitalLida, Digital digitalParaComparar)
        {
            var encontrou = false;
            var payload = new NBioAPI.Type.FIR_PAYLOAD();

            NBioAPI.Type.HFIR digitalLidaConvertida;
            _apiConversor.FDxToNBioBSPEx(digitalLida, (uint)digitalLida.Length, NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_ISO, NBioAPI.Type.FIR_PURPOSE.ENROLL_FOR_IDENTIFICATION_ONLY, out digitalLidaConvertida);

            NBioAPI.Type.HFIR digitalParaCompararConvertida;
            _apiConversor.FDxToNBioBSPEx(digitalParaComparar.TemplateIso, (uint)digitalParaComparar.TemplateIso.Length, NBioAPI.Type.MINCONV_DATA_TYPE.MINCONV_TYPE_ISO, NBioAPI.Type.FIR_PURPOSE.ENROLL_FOR_IDENTIFICATION_ONLY, out digitalParaCompararConvertida);

            var retorno = _api.VerifyMatch(digitalLidaConvertida, digitalParaCompararConvertida, out encontrou, payload);
            if (encontrou)
            {
                var cbInfo = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
                NBioAPI.IndexSearch.FP_INFO[] informacaoBiometria;
                NBioAPI.IndexSearch.FP_INFO nitgenBiometria;

                var resultadoAddFir = _apiBusca.AddFIR(digitalLidaConvertida, (uint)digitalParaComparar.Id, out informacaoBiometria);
                retorno = _apiBusca.IdentifyData(digitalLidaConvertida, NBioAPI.Type.FIR_SECURITY_LEVEL.HIGHEST, out nitgenBiometria, cbInfo);

                var idEncontrado = nitgenBiometria.ID;
                _apiBusca.RemoveUser((uint)digitalParaComparar.Id);
                if (idEncontrado > 0)
                    return digitalParaComparar;
            }
            Debug.WriteLine($"digital {digitalParaComparar.Id}");
            return Falha.Nova(404, "Digital não encontrada");
        }
    }
}
