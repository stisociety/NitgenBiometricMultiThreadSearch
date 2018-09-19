using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using NITGEN.SDK.NBioBSP;

namespace Nitgen.Identificacao.Simples.Demo
{
    internal class IdentificarBiometriaHandler
    {
        private NBioAPI.IndexSearch _nitgenSearchApi;
        private NBioAPI _nitgenMainApi;

        public IdentificarBiometriaHandler()
        {
            _nitgenMainApi = new NBioAPI();
            _nitgenSearchApi = new NBioAPI.IndexSearch(_nitgenMainApi);
            _nitgenSearchApi.InitEngine();
        }

        public void CarregarBiometrias(IEnumerable<Biometria> biometrias)
        {
            foreach (var biometria in biometrias)
            {
                var biometriaNitgen = new NBioAPI.Type.FIR_TEXTENCODE { TextFIR = biometria.TemplateISOText };
                NBioAPI.IndexSearch.FP_INFO[] informacaoBiometria;
                _nitgenSearchApi.AddFIR(biometriaNitgen, (uint)biometria.Id, out informacaoBiometria);
            }
        }

        public int IdentificarBiometria(NBioAPI.Type.HFIR template)
        {
            Console.WriteLine($"Localizado digital ...");
            var cbInfo = new NBioAPI.IndexSearch.CALLBACK_INFO_0();
            NBioAPI.IndexSearch.FP_INFO nitgenBiometria;
            var relogio = new Stopwatch();
            relogio.Start();
            var retorno = _nitgenSearchApi.IdentifyData(template, NBioAPI.Type.FIR_SECURITY_LEVEL.HIGH, out nitgenBiometria, cbInfo);
            relogio.Stop();
            Console.WriteLine($"Localizado {nitgenBiometria.ID} em {relogio.Elapsed.TotalSeconds}");
            return (int)nitgenBiometria.ID;
        }
    }
}