namespace UniCore.Api.Modules.Seguridad.Dto;

/// <summary>Resultado paginado de una consulta administrativa.</summary>
public sealed class ResultadoPaginadoDto<T>
{
    public List<T> items { get; set; } = [];
    public int pagina { get; set; }
    public int tamanoPagina { get; set; }
    public long total { get; set; }
}
