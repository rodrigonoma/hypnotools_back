using HypnoTools.API.Models.ERP;

namespace HypnoTools.API.Services.ERP;

public interface IERPIntegrationService
{
    Task<List<ProvedorExternoModel>> GetProvedoresExternosAsync(string empresa, int? provedor = null);
    Task<List<EmpresaAtivaModel>> ObterEmpresasAtivasAsync(string empresa);
    Task<List<ObraAtivaModel>> ObterObrasAtivasAsync(string empresa);
    Task<List<UnidadeDetalhadaModel>> BuscarUnidadesDetalhadasAsync(string empresa, string codigoObra);
    Task<List<CampoPersonalizadoModel>> BuscarCamposPersonalizadosAsync(string empresa, string codigoObra);
}